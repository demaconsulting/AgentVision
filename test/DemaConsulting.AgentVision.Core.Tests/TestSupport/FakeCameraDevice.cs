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

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A camera device that a test drives by hand: the test decides when frames arrive and when the device fails.
/// </summary>
/// <remarks>
///     It records how often it was disposed, so tests can prove that a camera is stopped exactly once and that
///     frames after disposal are ignored by the code under test. The device does not stop delivering frames by
///     itself after disposal; that lets a test check that the consumer guards against late frames.
/// </remarks>
internal sealed class FakeCameraDevice : IDisposable
{
    /// <summary>
    ///     The receiver of the frames, as given to the source.
    /// </summary>
    private readonly Action<EncodedFrame> _frameArrived;

    /// <summary>
    ///     The receiver of the failures, as given to the source.
    /// </summary>
    private readonly Action<Exception> _faulted;

    /// <summary>
    ///     The number of times <see cref="Dispose"/> was called.
    /// </summary>
    private int _disposeCount;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FakeCameraDevice"/> class.
    /// </summary>
    /// <param name="camera">The camera that was opened.</param>
    /// <param name="format">The format that was opened.</param>
    /// <param name="frameArrived">The receiver of the frames.</param>
    /// <param name="faulted">The receiver of the failures.</param>
    public FakeCameraDevice(
        CameraInfo camera,
        CameraFormat format,
        Action<EncodedFrame> frameArrived,
        Action<Exception> faulted)
    {
        Camera = camera;
        Format = format;
        _frameArrived = frameArrived;
        _faulted = faulted;
    }

    /// <summary>
    ///     Gets the camera that was opened.
    /// </summary>
    public CameraInfo Camera { get; }

    /// <summary>
    ///     Gets the format that was opened.
    /// </summary>
    public CameraFormat Format { get; }

    /// <summary>
    ///     Gets a value that says whether the device has been disposed at least once.
    /// </summary>
    public bool IsDisposed => DisposeCount > 0;

    /// <summary>
    ///     Gets the number of times the device was disposed.
    /// </summary>
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    /// <summary>
    ///     Delivers a frame, as the camera thread would.
    /// </summary>
    /// <param name="frame">The frame.</param>
    public void Emit(EncodedFrame frame) => _frameArrived(frame);

    /// <summary>
    ///     Reports a device failure, as the camera thread would.
    /// </summary>
    /// <param name="failure">The failure.</param>
    public void Fault(Exception failure) => _faulted(failure);

    /// <summary>
    ///     Records the disposal.
    /// </summary>
    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}
