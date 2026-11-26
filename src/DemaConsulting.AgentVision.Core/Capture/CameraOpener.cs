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

using DemaConsulting.AgentVision.Core.Cameras;

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     Opens cameras: finds the camera and the format that a request means, starts the camera, and returns a
///     <see cref="CameraSession"/> that keeps it streaming.
/// </summary>
/// <remarks>
///     <para>
///         One opener can open any number of cameras, one session each, and the sessions are independent: closing
///         one never affects another. That is how an application keeps several cameras hot at the same time.
///     </para>
///     <para>
///         Opening is the slow step (the device has to start streaming), and it is done once per camera; the
///         returned session is then used for as many snapshots as needed. The opener keeps no state besides its
///         source, so it is as thread-safe as the source: concurrent <c>Open</c> calls for different cameras are
///         safe with the default source.
///     </para>
/// </remarks>
public sealed class CameraOpener
{
    /// <summary>
    ///     The source that lists and opens the cameras.
    /// </summary>
    private readonly ICameraSource _source;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraOpener"/> class that opens the cameras of the host
    ///     with the default capture library.
    /// </summary>
    public CameraOpener()
        : this(new FlashCapCameraSource())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraOpener"/> class that opens the cameras of the given
    ///     source.
    /// </summary>
    /// <param name="source">The source that lists and opens the cameras. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    /// <remarks>
    ///     The source is the seam for tests and for applications that bring their own cameras, such as a
    ///     simulated camera.
    /// </remarks>
    public CameraOpener(ICameraSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>
    ///     Opens the camera that a selector names.
    /// </summary>
    /// <param name="camera">
    ///     The index of the camera as <c>agentvision cameras</c> lists it (a number), the identifier of the camera,
    ///     or its name (letter case does not matter). A purely numeric value is always an index.
    /// </param>
    /// <param name="options">The format request, warm-up, and timeouts; <see langword="null"/> for the defaults.</param>
    /// <returns>
    ///     A session that is streaming. The caller owns it and must dispose it to release the camera. The first
    ///     snapshot may wait for the warm-up to end.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="camera"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="camera"/> is empty, or names several cameras of one backend (use the index).
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option is invalid; the message names the option.</exception>
    /// <exception cref="CameraNotFoundException">Thrown when no camera matches; the message lists the available cameras.</exception>
    /// <exception cref="CameraOpenException">
    ///     Thrown when the camera offers no usable format, or cannot be opened or started (for example when another
    ///     application holds it).
    /// </exception>
    /// <remarks>
    ///     The camera list is read again on every call, so a camera that was attached after the application
    ///     started can be opened.
    /// </remarks>
    public CameraSession Open(string camera, CameraOpenOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(camera);

        // Check the options first: a bad request must fail before the machine is asked anything
        var effective = options ?? new CameraOpenOptions();
        effective.Validate(nameof(options));

        // Use the same list and order that the listing shows, so an index means the same camera
        var cameras = new CameraEnumerator(_source).Enumerate();
        return Open(CameraSelector.SelectCamera(cameras, camera), effective);
    }

    /// <summary>
    ///     Opens a camera that was described by the enumeration.
    /// </summary>
    /// <param name="camera">The camera to open, as <see cref="CameraEnumerator"/> described it.</param>
    /// <param name="options">The format request, warm-up, and timeouts; <see langword="null"/> for the defaults.</param>
    /// <returns>
    ///     A session that is streaming. The caller owns it and must dispose it to release the camera.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="camera"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an option is invalid; the message names the option.</exception>
    /// <exception cref="CameraOpenException">
    ///     Thrown when the camera offers no usable format, or cannot be opened or started. The message names the
    ///     camera; when the capture library failed, its exception is the inner exception.
    /// </exception>
    /// <remarks>
    ///     When this method throws, no camera is left open: a device that could not be started is not leaked.
    /// </remarks>
    public CameraSession Open(CameraInfo camera, CameraOpenOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(camera);
        var effective = options ?? new CameraOpenOptions();
        effective.Validate(nameof(options));

        // Decide the format first: a camera with nothing usable fails here, before it is touched
        var format = CameraSelector.SelectFormat(camera, effective);

        // The session exists before the device starts so that it can receive the very first frame
        var session = new CameraSession(camera, format, effective);
        try
        {
            var device = _source.Open(camera, format, session.OnFrameArrived, session.OnFaulted)
                         ?? throw new InvalidOperationException("The camera source returned no device handle.");
            session.Start(device);
            return session;
        }
        catch (CameraException)
        {
            // The source already described the failure in the library's own words
            session.Dispose();
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Anything else from a source is a failure to open; say which camera and keep the cause
            session.Dispose();
            throw new CameraOpenException(
                $"Camera '{camera.Name}' could not be opened: {ex.Message}", ex);
        }
    }
}
