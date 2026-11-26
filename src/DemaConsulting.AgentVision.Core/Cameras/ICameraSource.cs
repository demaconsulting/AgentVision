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

namespace DemaConsulting.AgentVision.Core.Cameras;

/// <summary>
///     A source of camera descriptions: the seam between camera enumeration and the capture library
///     that discovers the devices.
/// </summary>
/// <remarks>
///     The abstraction exists so that enumeration and capture policy can be exercised without camera
///     hardware and so that an application can supply its own cameras. No type of the capture library appears
///     in it, which keeps the public API independent of that library.
/// </remarks>
public interface ICameraSource
{
    /// <summary>
    ///     Gets the cameras that are currently available.
    /// </summary>
    /// <returns>
    ///     The available cameras in any order. An empty list, never <see langword="null"/>, when no camera
    ///     is available.
    /// </returns>
    /// <remarks>
    ///     Implementations report what is present now and do not cache between calls, because cameras can
    ///     be attached and removed while an application runs. Failures of the underlying capture library
    ///     are not translated: they propagate to the caller. Whether concurrent calls are allowed depends
    ///     on the implementation.
    /// </remarks>
    IReadOnlyList<CameraInfo> GetCameras();

    /// <summary>
    ///     Starts a camera streaming in the given format and delivers its frames to a callback.
    /// </summary>
    /// <param name="camera">The camera to open, as <see cref="GetCameras"/> described it.</param>
    /// <param name="format">The format to stream, one of the formats of <paramref name="camera"/>.</param>
    /// <param name="frameArrived">
    ///     Called for every frame, on a thread of the source. It must return quickly because a slow callback
    ///     makes the source drop frames. The frame belongs to the callee.
    /// </param>
    /// <param name="faulted">
    ///     Called when the device fails after it was opened, for example when it is unplugged. Frames may stop
    ///     without further notice if the capture library gives no failure signal.
    /// </param>
    /// <returns>
    ///     A handle that stops the camera when it is disposed. Disposing is idempotent, never throws, and returns
    ///     only when the device has stopped and no callback can run any more.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="Capture.CameraOpenException">
    ///     Thrown when the camera cannot be found any more, offers no such format, or cannot be opened or started.
    /// </exception>
    /// <remarks>
    ///     Opening is the slow, device-dependent step (it can take a second or more); applications that need low
    ///     latency open once and keep the handle. The source starts no thread of its own that outlives the handle.
    /// </remarks>
    IDisposable Open(
        CameraInfo camera,
        CameraFormat format,
        Action<EncodedFrame> frameArrived,
        Action<Exception> faulted);
}
