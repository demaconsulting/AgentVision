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
using DemaConsulting.AgentVision.Core.Capture;

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     Ready-made cameras, sources, and sessions for the capture tests.
/// </summary>
internal static class TestCameras
{
    /// <summary>
    ///     The width of the frames of the test cameras, in pixels.
    /// </summary>
    public const int FrameWidth = 64;

    /// <summary>
    ///     The height of the frames of the test cameras, in pixels.
    /// </summary>
    public const int FrameHeight = 48;

    /// <summary>
    ///     Creates a camera whose only format is a 64x48 JPEG at 30 frames per second.
    /// </summary>
    /// <param name="id">The identifier of the camera.</param>
    /// <param name="name">The name of the camera.</param>
    /// <returns>The camera.</returns>
    public static CameraInfo Camera(string id = "Fake:0", string name = "Fake Camera") =>
        new(id, name, name + " (Fake)", "Fake", [new CameraFormat(FrameWidth, FrameHeight, "JPEG", 30)]);

    /// <summary>
    ///     Creates a fake source with one camera that does not stream by itself; the test emits frames.
    /// </summary>
    /// <returns>The source.</returns>
    public static FakeCameraSource Source() => new([Camera()]);

    /// <summary>
    ///     Creates a fake source with one camera that streams bright 64x48 JPEG frames by itself.
    /// </summary>
    /// <param name="level">The gray level of the frames.</param>
    /// <returns>The source.</returns>
    public static FakeCameraSource StreamingSource(byte level = 200)
    {
        // The frame is made once; every frame of the stream is a copy, because a source never reuses an array
        var jpeg = SyntheticFrames.SolidJpeg(FrameWidth, FrameHeight, level);
        return new FakeCameraSource([Camera()])
        {
            FrameFactory = index => new EncodedFrame((byte[])jpeg.Clone(), TimeSpan.FromMilliseconds(index * 33), index)
        };
    }

    /// <summary>
    ///     Options without warm-up and with short waits, so that tests are fast.
    /// </summary>
    /// <returns>The options.</returns>
    public static CameraOpenOptions QuickOptions() =>
        new() { WarmUp = TimeSpan.Zero, FirstFrameTimeout = TimeSpan.FromSeconds(10) };

    /// <summary>
    ///     Opens the first camera of a source with quick options.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="options">The options; quick options when null.</param>
    /// <returns>The session; the caller disposes it.</returns>
    public static CameraSession Open(FakeCameraSource source, CameraOpenOptions? options = null) =>
        new CameraOpener(source).Open("0", options ?? QuickOptions());
}
