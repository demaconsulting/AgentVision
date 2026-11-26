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

using DemaConsulting.AgentVision.Core.Capture;
using FlashCap;

namespace DemaConsulting.AgentVision.Core.Cameras;

/// <summary>
///     One FlashCap capture device that is streaming: it turns FlashCap's pixel buffers into
///     <see cref="EncodedFrame"/> values and stops the device deterministically.
/// </summary>
/// <remarks>
///     <para>
///         The unit contains the part of capturing that cannot be written without FlashCap: opening the device
///         with the "latest frame only" strategy, copying each image out of FlashCap's reusable buffer, and
///         stopping and disposing the device in a bounded time. Everything that is policy (which camera, which
///         format, warm-up, what a stale camera means) is in the <c>Capture</c> subsystem.
///     </para>
///     <para>
///         FlashCap is asked to queue at most one frame and to drop frames while the callback is busy, which is the
///         right behavior for a library that only ever wants the newest frame: there is never a backlog, and a slow
///         consumer sees the latest picture instead of an old one. FlashCap returns JPEG and PNG frames as they are
///         and everything else as a bitmap file (raw YUV frames are converted to 24-bit RGB), so every
///         <see cref="EncodedFrame"/> is a complete image file.
///     </para>
///     <para>
///         The type is thread-safe. The callbacks run on a FlashCap thread; they are serialized with
///         <see cref="Dispose"/> so that none runs after disposal has returned.
///     </para>
/// </remarks>
internal sealed class FlashCapCameraDevice : IDisposable
{
    /// <summary>
    ///     How long opening and starting the device may take before it counts as failed.
    /// </summary>
    internal static readonly TimeSpan OpenTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     How long stopping and disposing the device may take before the wait is given up.
    /// </summary>
    internal static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     The number of frames FlashCap may queue for the callback: one, so that only the newest waits.
    /// </summary>
    private const int MaxQueuedFrames = 1;

    /// <summary>
    ///     Serializes the frame callback with disposal.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    ///     The receiver of the frames.
    /// </summary>
    private readonly Action<EncodedFrame> _frameArrived;

    /// <summary>
    ///     The receiver of the failures.
    /// </summary>
    private readonly Action<Exception> _faulted;

    /// <summary>
    ///     The FlashCap device; set once by <see cref="Open"/> before the object is handed out.
    /// </summary>
    private CaptureDevice? _device;

    /// <summary>
    ///     Whether the device has been disposed. Read and written only under <see cref="_gate"/>.
    /// </summary>
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FlashCapCameraDevice"/> class that is not open yet.
    /// </summary>
    /// <param name="frameArrived">Receives each frame.</param>
    /// <param name="faulted">Receives a failure of the capture path.</param>
    private FlashCapCameraDevice(Action<EncodedFrame> frameArrived, Action<Exception> faulted)
    {
        _frameArrived = frameArrived;
        _faulted = faulted;
    }

    /// <summary>
    ///     Opens a FlashCap device and starts it streaming.
    /// </summary>
    /// <param name="descriptor">The FlashCap descriptor of the device.</param>
    /// <param name="characteristics">The video characteristics to stream.</param>
    /// <param name="cameraName">The name of the camera, used in failure messages.</param>
    /// <param name="frameArrived">Called on a FlashCap thread for every frame; must return quickly.</param>
    /// <param name="faulted">Called when copying or delivering a frame fails.</param>
    /// <returns>A streaming device. The caller owns it and must dispose it.</returns>
    /// <exception cref="CameraOpenException">
    ///     Thrown when FlashCap fails to open or start the device, or does not finish within
    ///     <see cref="OpenTimeout"/> (a canceled or timed-out open is reported as a backend that did not open the
    ///     device, with a hint for the legacy VideoForWindows backend). The message names the camera and the
    ///     FlashCap exception is the inner exception. A device that was opened but could not be started is disposed
    ///     before the exception is thrown.
    /// </exception>
    public static FlashCapCameraDevice Open(
        CaptureDeviceDescriptor descriptor,
        VideoCharacteristics characteristics,
        string cameraName,
        Action<EncodedFrame> frameArrived,
        Action<Exception> faulted)
    {
        var result = new FlashCapCameraDevice(frameArrived, faulted);

        // Open: latest-frame-only strategy; failures and timeouts become the library's own exception
        try
        {
            using var cancellation = new CancellationTokenSource(OpenTimeout);
            var opening = descriptor.OpenAsync(
                characteristics,
                TranscodeFormats.Auto,
                isScattering: false,
                MaxQueuedFrames,
                result.OnPixelBufferArrived,
                cancellation.Token);
            result._device = WaitForOpen(opening, cancellation);
        }
        catch (Exception ex) when (ex is not (OutOfMemoryException or CameraOpenException))
        {
            throw new CameraOpenException(
                $"Camera '{cameraName}' could not be opened: {Reason(ex, descriptor.DeviceType, "open")}", Unwrap(ex));
        }

        // Start: a device that opened but cannot start must not stay open
        try
        {
            using var cancellation = new CancellationTokenSource(OpenTimeout);
            WaitFor(result._device.StartAsync(cancellation.Token), cancellation);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            result.Dispose();
            throw new CameraOpenException(
                $"Camera '{cameraName}' could not be started: {Reason(ex, descriptor.DeviceType, "start")}", Unwrap(ex));
        }

        return result;
    }

    /// <summary>
    ///     Stops the device, releases it, and guarantees that no callback runs any more.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Idempotent and never throws: this runs in cleanup paths, where a second failure would hide the first.
    ///         The wait for FlashCap is bounded by <see cref="StopTimeout"/> for each of the two steps (stop, then
    ///         dispose), because a device that is unplugged can make FlashCap's stop hang; after the timeout the
    ///         method returns and the stuck device is left to FlashCap.
    ///     </para>
    ///     <para>
    ///         The callbacks are shut off first, under the lock that the callbacks also take, so a callback that is
    ///         running finishes, and none starts afterwards, before the device is stopped. Do not call this method
    ///         from inside a callback.
    ///     </para>
    /// </remarks>
    public void Dispose()
    {
        // Shut the callbacks off first; this waits for a callback that is running
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        var device = _device;
        if (device == null)
        {
            return;
        }

        // Stop and release the device, each step bounded and each failure ignored: this is cleanup
        Quietly(() => device.StopAsync().Wait(StopTimeout, CancellationToken.None));
        Quietly(() => device.DisposeAsync().Wait(StopTimeout, CancellationToken.None));
    }

    /// <summary>
    ///     Receives a pixel buffer from FlashCap and passes its image on as a frame.
    /// </summary>
    /// <param name="scope">The scope that holds FlashCap's reusable buffer; valid only inside this call.</param>
    /// <remarks>
    ///     The image is copied out of the buffer because FlashCap reuses the buffer as soon as the call returns.
    ///     A failure is reported through the <c>faulted</c> callback and never thrown into FlashCap.
    /// </remarks>
    private void OnPixelBufferArrived(PixelBufferScope scope)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                var buffer = scope.Buffer;
                _frameArrived(new EncodedFrame(buffer.CopyImage(), buffer.Timestamp, buffer.FrameIndex));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _faulted(ex);
            }
        }
    }

    /// <summary>
    ///     Waits for the open task and disposes a device that opens too late.
    /// </summary>
    /// <param name="opening">The task of the open call.</param>
    /// <param name="cancellation">The cancellation source of the open call; canceled on timeout.</param>
    /// <returns>The opened device.</returns>
    /// <exception cref="TimeoutException">Thrown when the open does not finish in time.</exception>
    private static CaptureDevice WaitForOpen(Task<CaptureDevice> opening, CancellationTokenSource cancellation)
    {
        if (!opening.Wait(OpenTimeout, CancellationToken.None))
        {
            // FlashCap may still finish later; a device that appears then must not stay open
            cancellation.Cancel();
            opening.ContinueWith(
                late => Quietly(() => late.Result.DisposeAsync().Wait(StopTimeout, CancellationToken.None)),
                TaskContinuationOptions.OnlyOnRanToCompletion);
            throw new TimeoutException($"FlashCap did not open the device within {OpenTimeout.TotalSeconds:0} seconds.");
        }

        return opening.Result;
    }

    /// <summary>
    ///     Waits for a FlashCap task to finish within the open timeout.
    /// </summary>
    /// <param name="task">The task.</param>
    /// <param name="cancellation">Its cancellation source; canceled on timeout.</param>
    /// <exception cref="TimeoutException">Thrown when the task does not finish in time.</exception>
    private static void WaitFor(Task task, CancellationTokenSource cancellation)
    {
        if (!task.Wait(OpenTimeout, CancellationToken.None))
        {
            cancellation.Cancel();
            throw new TimeoutException($"FlashCap did not finish within {OpenTimeout.TotalSeconds:0} seconds.");
        }
    }

    /// <summary>
    ///     Runs a cleanup step and ignores its failure.
    /// </summary>
    /// <param name="action">The cleanup step.</param>
    private static void Quietly(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Deliberately ignored: cleanup must not throw, and the device is being abandoned anyway
        }
    }

    /// <summary>
    ///     Gets the exception that explains a failure: the inner exception of an aggregate, otherwise the exception.
    /// </summary>
    /// <param name="ex">The caught exception.</param>
    /// <returns>The exception to report.</returns>
    private static Exception Unwrap(Exception ex) =>
        ex is AggregateException { InnerException: { } inner } ? inner : ex;

    /// <summary>
    ///     Describes a failure in one line for a message.
    /// </summary>
    /// <param name="ex">The caught exception.</param>
    /// <returns>The message of the exception that explains the failure.</returns>
    private static string Describe(Exception ex) => Unwrap(ex).Message;

    /// <summary>
    ///     Explains why opening or starting the device failed, in words a user can act on.
    /// </summary>
    /// <param name="ex">The caught exception.</param>
    /// <param name="backend">The FlashCap backend of the device.</param>
    /// <param name="step">The step that failed: <c>open</c> or <c>start</c>.</param>
    /// <returns>The reason to put into the failure message.</returns>
    /// <remarks>
    ///     A backend that never answers ends in a timeout or a canceled task, whose own messages ("A task was
    ///     canceled.") say nothing useful, so they are replaced by a statement that the backend did not respond. The
    ///     legacy Video for Windows backend needs a message pump that a console application does not have and is
    ///     therefore often unusable, which is worth a hint.
    /// </remarks>
    private static string Reason(Exception ex, DeviceTypes backend, string step)
    {
        if (Unwrap(ex) is not (TimeoutException or OperationCanceledException))
        {
            return Describe(ex);
        }

        var reason = $"the {backend} backend did not {step} the device within {OpenTimeout.TotalSeconds:0} seconds.";
        return backend == DeviceTypes.VideoForWindows
            ? reason + " The VideoForWindows backend is a legacy backend that is often unsupported; " +
              "use the DirectShow or MediaFoundation entry of the same camera instead."
            : reason;
    }
}
