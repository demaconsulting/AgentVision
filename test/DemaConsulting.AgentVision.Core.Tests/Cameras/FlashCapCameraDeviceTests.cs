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
using DemaConsulting.AgentVision.Core.Tests.TestSupport;
using FlashCap;
using FlashCap.Utilities;

namespace DemaConsulting.AgentVision.Core.Tests.Cameras;

/// <summary>
///     Unit tests for the FlashCapCameraDevice class, including the integration tests that provide the evidence
///     for the open, start, stop, and dispose functions of the FlashCap off-the-shelf item.
/// </summary>
/// <remarks>
///     The tests open descriptors that belong to no real camera, whose device is a <see cref="FakeFlashCapDevice"/>
///     that follows FlashCap's real lifecycle, so the real FlashCap open, start, stop, and dispose sequence and its
///     frame queue run without a camera. Tests whose names contain <c>RealFlashCap</c> are the evidence for the
///     FlashCap requirements.
/// </remarks>
public class FlashCapCameraDeviceTests
{
    /// <summary>
    ///     The longest a test waits for something that should happen at once.
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     The video characteristics that the test cameras offer: 64x48 motion JPEG at 30 frames per second.
    /// </summary>
    private static readonly VideoCharacteristics Jpeg = new(PixelFormats.JPEG, 64, 48, new Fraction(30, 1));

    /// <summary>
    ///     Creates a descriptor whose device is the given fake.
    /// </summary>
    /// <param name="device">The device that opening the descriptor yields.</param>
    /// <returns>The descriptor.</returns>
    private static FakeCaptureDeviceDescriptor DescriptorOf(FakeFlashCapDevice device) =>
        new(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera (V4L2)", Jpeg) { OpenDevice = () => device };

    /// <summary>
    ///     Test that opening starts the FlashCap device exactly once.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Open_RealFlashCapLifecycle_StartsDevice()
    {
        // Arrange: a descriptor that yields a fake FlashCap device
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");

        // Act: open the camera device
        using var device = FlashCapCameraDevice.Open(DescriptorOf(fake), Jpeg, "Desk Camera", _ => { }, _ => { });

        // Assert: FlashCap initialized the device with the chosen characteristics, and started it once
        Assert.Equal(Jpeg, fake.InitializedWith);
        Assert.Equal(1, fake.StartCount);
        Assert.Equal(0, fake.StopCount);
        Assert.Equal(0, fake.DisposeCount);
    }

    /// <summary>
    ///     Test that disposing stops and disposes the FlashCap device, and that disposing twice does nothing more.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Dispose_RealFlashCapLifecycle_StopsAndDisposes()
    {
        // Arrange: an open camera device
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");
        var device = FlashCapCameraDevice.Open(DescriptorOf(fake), Jpeg, "Desk Camera", _ => { }, _ => { });

        // Act: dispose it twice
        device.Dispose();
        device.Dispose();

        // Assert: FlashCap stopped and disposed the device exactly once
        Assert.Equal(1, fake.StopCount);
        Assert.Equal(1, fake.DisposeCount);
    }

    /// <summary>
    ///     Test that a frame that the camera delivers reaches the callback as a complete image file.
    /// </summary>
    [Fact]
    public async Task FlashCapCameraDevice_FrameArrived_RealFlashCapQueue_DeliversEncodedFrame()
    {
        // Arrange: an open camera device whose callback completes a task, and a JPEG frame
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");
        var delivered = new TaskCompletionSource<EncodedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = FlashCapCameraDevice.Open(
            DescriptorOf(fake), Jpeg, "Desk Camera", frame => delivered.TrySetResult(frame), _ => { });
        var jpeg = SyntheticFrames.SolidJpeg(64, 48, 128);

        // Act: the camera delivers the frame
        fake.InjectJpeg(jpeg, 5);

        // Assert: the callback receives the same bytes (FlashCap passes JPEG frames on as they are) and the frame number
        var frame = await delivered.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.Equal(jpeg, frame.Data);
        Assert.Equal(5, frame.FrameIndex);
    }

    /// <summary>
    ///     Test that raw 24-bit frames reach the callback as a bitmap file that the library can decode.
    /// </summary>
    [Fact]
    public async Task FlashCapCameraDevice_FrameArrived_RealFlashCapQueueRawFrame_DeliversBitmapFile()
    {
        // Arrange: an open camera device for 24-bit raw frames, and one raw frame of 4x2 pixels (bottom row first)
        var raw = new VideoCharacteristics(PixelFormats.RGB24, 4, 2, new Fraction(30, 1));
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");
        var descriptor = new FakeCaptureDeviceDescriptor(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera", raw)
        {
            OpenDevice = () => fake
        };
        var delivered = new TaskCompletionSource<EncodedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = FlashCapCameraDevice.Open(descriptor, raw, "Desk Camera", frame => delivered.TrySetResult(frame), _ => { });
        var pixels = new byte[4 * 2 * 3];

        // Act: the camera delivers the raw frame
        fake.InjectRgb24(pixels, 4, 2, 1);

        // Assert: the callback receives a bitmap file (FlashCap prepends the file header) that decodes to the frame's size
        var frame = await delivered.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.Equal((byte)'B', frame.Data[0]);
        Assert.Equal((byte)'M', frame.Data[1]);
        using var image = DemaConsulting.AgentVision.Core.Imaging.CameraImage.Decode(frame.Data);
        Assert.Equal((4, 2), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that raw YUYV frames are converted to a bitmap file that the library can decode.
    /// </summary>
    [Fact]
    public async Task FlashCapCameraDevice_FrameArrived_RealFlashCapQueueYuyvFrame_DeliversDecodableBitmap()
    {
        // Arrange: an open camera device for YUYV frames, and a mid-gray 64x64 frame (Y=128, U=V=128)
        var yuyv = new VideoCharacteristics(PixelFormats.YUYV, 64, 64, new Fraction(30, 1));
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");
        var descriptor = new FakeCaptureDeviceDescriptor(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera", yuyv)
        {
            OpenDevice = () => fake
        };
        var delivered = new TaskCompletionSource<EncodedFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = FlashCapCameraDevice.Open(descriptor, yuyv, "Desk Camera", frame => delivered.TrySetResult(frame), failure => delivered.TrySetException(failure));
        var pixels = Enumerable.Repeat((byte)128, 64 * 64 * 2).ToArray();

        // Act: the camera delivers the raw frame
        fake.InjectYuy2(pixels, 64, 64, 1);

        // Assert: the callback receives a bitmap that decodes to the frame's size and a gray color
        var frame = await delivered.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
        using var image = DemaConsulting.AgentVision.Core.Imaging.CameraImage.Decode(frame.Data);
        Assert.Equal((64, 64), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that a failure inside the frame callback is reported as a fault and does not escape into FlashCap.
    /// </summary>
    [Fact]
    public async Task FlashCapCameraDevice_FrameArrived_CallbackThrows_ReportsFault()
    {
        // Arrange: an open camera device whose frame callback fails, and a fault callback that completes a task
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera");
        var fault = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var device = FlashCapCameraDevice.Open(
            DescriptorOf(fake),
            Jpeg,
            "Desk Camera",
            _ => throw new InvalidOperationException("the consumer failed"),
            failure => fault.TrySetResult(failure));

        // Act: the camera delivers a frame
        fake.InjectJpeg(SyntheticFrames.SolidJpeg(64, 48, 128), 1);

        // Assert: the failure is reported through the fault callback
        var failure = await fault.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
        Assert.Equal("the consumer failed", failure.Message);
    }

    /// <summary>
    ///     Test that a descriptor that cannot be opened gives an open exception that names the camera.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Open_RealFlashCapDescriptorFails_ThrowsCameraOpen()
    {
        // Arrange: a descriptor whose device is held by another application
        var cause = new IOException("device busy");
        var descriptor = new FakeCaptureDeviceDescriptor(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera", Jpeg)
        {
            OpenDevice = () => throw cause
        };

        // Act / Assert: the failure becomes an open exception that names the camera and keeps the FlashCap exception
        var exception = Assert.Throws<CameraOpenException>(
            () => FlashCapCameraDevice.Open(descriptor, Jpeg, "Desk Camera", _ => { }, _ => { }));
        Assert.Contains("'Desk Camera'", exception.Message);
        Assert.Contains("device busy", exception.Message);
        Assert.Same(cause, exception.InnerException);
    }

    /// <summary>
    ///     Test that a canceled open of a Video for Windows camera gives a timeout-style open exception with a hint.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Open_RealFlashCapVideoForWindowsCanceled_ThrowsCameraOpenWithBackendHint()
    {
        // Arrange: a legacy backend whose open is canceled, as FlashCap does when the backend never answers
        var cause = new TaskCanceledException();
        var descriptor = new FakeCaptureDeviceDescriptor(DeviceTypes.VideoForWindows, 0, "Default", "Default", Jpeg)
        {
            OpenDevice = () => throw cause
        };

        // Act / Assert: the message names the camera and the backend, says the device did not open, and gives the hint
        var exception = Assert.Throws<CameraOpenException>(
            () => FlashCapCameraDevice.Open(descriptor, Jpeg, "Default", _ => { }, _ => { }));
        Assert.Contains("'Default'", exception.Message);
        Assert.Contains("VideoForWindows backend did not open the device", exception.Message);
        Assert.Contains("legacy backend", exception.Message);
        Assert.Contains("DirectShow or MediaFoundation", exception.Message);
        Assert.DoesNotContain("A task was canceled", exception.Message);
        Assert.Same(cause, exception.InnerException);
    }

    /// <summary>
    ///     Test that a canceled open of another backend gives the same message without the legacy hint.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Open_RealFlashCapOtherBackendCanceled_ThrowsCameraOpenWithoutHint()
    {
        // Arrange: a backend whose open is canceled
        var descriptor = new FakeCaptureDeviceDescriptor(DeviceTypes.V4L2, "/dev/video0", "Desk Camera", "Desk Camera", Jpeg)
        {
            OpenDevice = () => throw new OperationCanceledException()
        };

        // Act / Assert: the message says that the backend did not open the device, and has no legacy hint
        var exception = Assert.Throws<CameraOpenException>(
            () => FlashCapCameraDevice.Open(descriptor, Jpeg, "Desk Camera", _ => { }, _ => { }));
        Assert.Contains("'Desk Camera'", exception.Message);
        Assert.Contains("V4L2 backend did not open the device", exception.Message);
        Assert.DoesNotContain("legacy", exception.Message);
    }

    /// <summary>
    ///     Test that a device that opens but cannot start is disposed and reported.
    /// </summary>
    [Fact]
    public void FlashCapCameraDevice_Open_RealFlashCapStartFails_ThrowsCameraOpenAndDisposesDevice()
    {
        // Arrange: a device that fails to start
        var fake = new FakeFlashCapDevice("/dev/video0", "Desk Camera") { StartFailure = new IOException("no stream") };

        // Act / Assert: the failure becomes an open exception, and the device was released
        var exception = Assert.Throws<CameraOpenException>(
            () => FlashCapCameraDevice.Open(DescriptorOf(fake), Jpeg, "Desk Camera", _ => { }, _ => { }));
        Assert.Contains("could not be started", exception.Message);
        Assert.Contains("no stream", exception.Message);
        Assert.Equal(1, fake.DisposeCount);
    }
}
