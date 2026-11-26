// Copyright (c) DEMA Consulting
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Diagnostics;
using System.Globalization;
using DemaConsulting.AgentVision.Core.Cameras;

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     An open ("hot") camera: it streams in the background, holds the latest frame, and hands it out with
///     almost no delay.
/// </summary>
/// <remarks>
///     <para>
///         The session exists so that the slow part of capturing (opening the camera, letting auto exposure
///         settle) is paid once, and every later <see cref="Snapshot"/> is a pointer read. A long-running
///         application, such as a background service, opens one session per camera and keeps it for as long as
///         frames may be wanted.
///     </para>
///     <para>
///         The device thread publishes each frame that arrives after the warm-up as the new latest frame; older
///         frames are not queued. A frame counts only when it arrives at or after the warm-up, measured from the
///         moment streaming started, so the dark and unfocused first frames of a camera are never served.
///     </para>
///     <para>
///         The session is thread-safe: any number of threads may call <see cref="Snapshot"/>, and
///         <see cref="Dispose"/> may be called from any thread, concurrently with snapshots. Disposing is what
///         stops the camera: it is deterministic (the device has stopped when it returns), idempotent, and the
///         only way to release the camera, so always dispose a session when it is no longer needed.
///     </para>
/// </remarks>
public sealed class CameraSession : IDisposable
{
    /// <summary>
    ///     The options the session was opened with.
    /// </summary>
    private readonly CameraOpenOptions _options;

    /// <summary>
    ///     Signaled when a frame arrives, the device faults, or the session is disposed: everything that can
    ///     end the wait of a snapshot. Never reset after it is set.
    /// </summary>
    /// <remarks>
    ///     The event is deliberately not disposed. It never allocates an operating system handle unless a wait
    ///     handle is requested, which this type never does, and disposing it while a snapshot waits would race.
    /// </remarks>
    private readonly ManualResetEventSlim _wakeUp = new(false);

    /// <summary>
    ///     The latest frame that arrived after the warm-up, or <see langword="null"/> before the first one and
    ///     after disposal. Read and written only with volatile or interlocked operations.
    /// </summary>
    private CameraFrame? _latest;

    /// <summary>
    ///     The failure that the device reported, or <see langword="null"/>. Written once, with an interlocked
    ///     operation.
    /// </summary>
    private Exception? _fault;

    /// <summary>
    ///     The handle that stops the device, set by <see cref="Start"/>.
    /// </summary>
    private IDisposable? _device;

    /// <summary>
    ///     The monotonic timestamp at which streaming started; valid once <see cref="_streaming"/> is set.
    /// </summary>
    private long _streamStartTimestamp;

    /// <summary>
    ///     Whether streaming has started. Frames that arrive before it is set count as warm-up.
    /// </summary>
    private volatile bool _streaming;

    /// <summary>
    ///     Zero while the session is open, one after disposal. Changed only with an interlocked operation.
    /// </summary>
    private int _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraSession"/> class that is not streaming yet.
    /// </summary>
    /// <param name="camera">The camera the session belongs to.</param>
    /// <param name="format">The format that was chosen for it.</param>
    /// <param name="options">The validated options the camera was opened with.</param>
    /// <remarks>
    ///     Only <see cref="CameraOpener"/> creates sessions: it first creates the session so that the device
    ///     can be given the session's callbacks, then starts the device and calls <see cref="Start"/>.
    /// </remarks>
    internal CameraSession(CameraInfo camera, CameraFormat format, CameraOpenOptions options)
    {
        Camera = camera;
        Format = format;
        _options = options;
    }

    /// <summary>
    ///     Gets the camera that the session streams.
    /// </summary>
    public CameraInfo Camera { get; }

    /// <summary>
    ///     Gets the format that the camera was opened with, which can differ from the request when the camera
    ///     does not offer it exactly.
    /// </summary>
    public CameraFormat Format { get; }

    /// <summary>
    ///     Gets a value that says whether a frame is ready, so that <see cref="Snapshot"/> returns without waiting.
    /// </summary>
    /// <remarks>
    ///     It is <see langword="false"/> during the warm-up and until the first frame after it arrives, and after
    ///     the session is disposed.
    /// </remarks>
    public bool IsReady => Volatile.Read(ref _latest) != null;

    /// <summary>
    ///     Gets a value that says whether the session has been disposed.
    /// </summary>
    public bool IsClosed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    ///     Gets the latest frame of the camera.
    /// </summary>
    /// <returns>
    ///     The most recent frame that arrived after the warm-up. Every call after the first one returns without
    ///     waiting, whatever the number of threads; two calls may return the same frame when no new one arrived.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session has been disposed, also while a call waits.</exception>
    /// <exception cref="CameraTimeoutException">
    ///     Thrown when no frame arrived within the first-frame timeout after the warm-up. Only the first
    ///     snapshots can wait and time out.
    /// </exception>
    /// <exception cref="CameraDisconnectedException">
    ///     Thrown when the device reported a failure, or the latest frame is older than the stale-frame timeout,
    ///     which means that the camera stopped delivering. The session cannot recover; dispose it and open the
    ///     camera again.
    /// </exception>
    /// <remarks>
    ///     Only the first call can block: it waits (without polling, on an event) for the first frame that
    ///     arrives after the warm-up, which is why a freshly opened session may take up to the warm-up time. A
    ///     daemon that wants no wait at all opens the session ahead of time and checks <see cref="IsReady"/>.
    /// </remarks>
    public CameraFrame Snapshot()
    {
        // A closed or failed session never serves a frame, however recent it is
        ThrowIfClosed();
        ThrowIfFaulted();

        // The common case is one volatile read; only the first snapshot can find no frame yet
        var frame = Volatile.Read(ref _latest) ?? WaitForFirstFrame();

        // A camera that stopped delivering (unplugged, taken over) leaves the last frame in place, so an old
        // frame must not be served as if it were current
        var age = frame.Age;
        if (age > _options.StaleFrameTimeout)
        {
            throw new CameraDisconnectedException(
                $"Camera '{Camera.Name}' has delivered no new frame for {Seconds(age)} seconds (the limit is " +
                $"{Seconds(_options.StaleFrameTimeout)}). It was probably unplugged or taken by another " +
                "application; close the session and open the camera again.");
        }

        return frame;
    }

    /// <summary>
    ///     Stops the camera and releases the session.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Disposing stops the device and returns only when it has stopped and no frame can arrive any more,
    ///         so the camera is free for another application when the call returns. This can take a moment
    ///         (the device is stopped and closed). It is idempotent: the second and later calls do nothing, and it
    ///         is safe to call while other threads wait in <see cref="Snapshot"/>, which then fail with an
    ///         <see cref="ObjectDisposedException"/>.
    ///     </para>
    ///     <para>
    ///         Frames that were handed out by <see cref="Snapshot"/> stay valid.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        // Only the first call does the work, whichever thread it is
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Wake the waiting snapshots first so that they do not wait for the device to finish stopping, then
        // stop the device and drop the held frame
        _wakeUp.Set();
        _device?.Dispose();
        Interlocked.Exchange(ref _latest, null);
    }

    /// <summary>
    ///     Records the device handle and starts the warm-up clock.
    /// </summary>
    /// <param name="device">The handle that stops the device that is now streaming.</param>
    /// <remarks>
    ///     Frames that arrived while the device was being opened are warm-up by definition: the clock starts
    ///     now, so the warm-up always counts from the moment the camera is known to be streaming.
    /// </remarks>
    internal void Start(IDisposable device)
    {
        _device = device;
        _streamStartTimestamp = Stopwatch.GetTimestamp();
        _streaming = true;
    }

    /// <summary>
    ///     Receives a frame from the device, on the device's thread, and publishes it if the warm-up is over.
    /// </summary>
    /// <param name="frame">The frame that arrived.</param>
    /// <remarks>
    ///     The method is the whole of the work done on the device thread: a clock check, one allocation, and a
    ///     volatile write, so the device never waits and frames are never queued. Frames are ignored after the
    ///     session is disposed.
    /// </remarks>
    internal void OnFrameArrived(EncodedFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        // Frames of a closed session, and frames before streaming is known to have started, are not served
        if (IsClosed || !_streaming)
        {
            return;
        }

        // Frames inside the warm-up are discarded: they are the dark and unfocused ones
        if (Stopwatch.GetElapsedTime(_streamStartTimestamp) < _options.WarmUp)
        {
            return;
        }

        // Publish the frame as the latest one and wake a snapshot that waits for the first frame
        Interlocked.Exchange(
            ref _latest,
            new CameraFrame(frame.FrameIndex, frame.Data, Stopwatch.GetTimestamp(), DateTimeOffset.UtcNow));
        _wakeUp.Set();
    }

    /// <summary>
    ///     Receives a device failure and makes every later snapshot fail.
    /// </summary>
    /// <param name="failure">The failure that the device reported.</param>
    internal void OnFaulted(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        // Only the first failure is kept; it is the cause, later ones are consequences
        Interlocked.CompareExchange(ref _fault, failure, null);
        _wakeUp.Set();
    }

    /// <summary>
    ///     Formats a duration as seconds with one decimal for a message.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The number of seconds in the invariant culture.</returns>
    private static string Seconds(TimeSpan duration) =>
        duration.TotalSeconds.ToString("0.#", CultureInfo.InvariantCulture);

    /// <summary>
    ///     Waits for the first frame that arrives after the warm-up.
    /// </summary>
    /// <returns>The first frame.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the session is disposed during the wait.</exception>
    /// <exception cref="CameraDisconnectedException">Thrown when the device fails during the wait.</exception>
    /// <exception cref="CameraTimeoutException">Thrown when the first-frame deadline passes without a frame.</exception>
    private CameraFrame WaitForFirstFrame()
    {
        // The deadline is the end of the warm-up plus the first-frame allowance, counted from the start of streaming
        var allowed = _options.WarmUp + _options.FirstFrameTimeout;
        while (true)
        {
            var remaining = allowed - Stopwatch.GetElapsedTime(_streamStartTimestamp);
            if (remaining > TimeSpan.Zero)
            {
                // Wait on the event: it is set by a frame, a failure, or disposal, so no polling is needed
                _wakeUp.Wait((int)Math.Min(Math.Ceiling(remaining.TotalMilliseconds), int.MaxValue));
            }

            // Whatever ended the wait, report a closed or failed session first, then a frame that arrived
            ThrowIfClosed();
            ThrowIfFaulted();
            var frame = Volatile.Read(ref _latest);
            if (frame != null)
            {
                return frame;
            }

            // No frame and no time left: the camera never delivered one after the warm-up
            if (allowed - Stopwatch.GetElapsedTime(_streamStartTimestamp) <= TimeSpan.Zero)
            {
                throw new CameraTimeoutException(
                    $"Camera '{Camera.Name}' delivered no frame within {Seconds(_options.FirstFrameTimeout)} seconds " +
                    $"after the {_options.WarmUp.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture)} ms " +
                    "warm-up. The camera may be covered, in use by another application, or too slow at this format; " +
                    "try a smaller format or a longer first-frame timeout.");
            }
        }
    }

    /// <summary>
    ///     Fails when the session has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the session is closed.</exception>
    private void ThrowIfClosed() => ObjectDisposedException.ThrowIf(IsClosed, this);

    /// <summary>
    ///     Fails when the device reported a failure.
    /// </summary>
    /// <exception cref="CameraDisconnectedException">Thrown when the device reported a failure.</exception>
    private void ThrowIfFaulted()
    {
        var fault = Volatile.Read(ref _fault);
        if (fault != null)
        {
            throw new CameraDisconnectedException(
                $"Camera '{Camera.Name}' reported a failure and no longer delivers frames: {fault.Message} " +
                "Close the session and open the camera again.",
                fault);
        }
    }
}
