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

using System.Reflection;
using System.Runtime.InteropServices;
using FlashCap;

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A FlashCap capture device that belongs to no real camera: it follows the real FlashCap lifecycle (open,
///     start, stop, dispose) and delivers frames that a test injects.
/// </summary>
/// <remarks>
///     <para>
///         FlashCap's own frame processors do the queuing and the call of the application's callback, so the code
///         under test runs the real FlashCap path. What a real driver would do, copying the camera's bytes into
///         FlashCap's pixel buffer, is done here by <see cref="OnCapture"/>: FlashCap offers no public way to fill
///         a pixel buffer, so the method calls FlashCap's internal copy routine by reflection. This is the single
///         place of the test suite that depends on a FlashCap internal.
///     </para>
/// </remarks>
internal sealed class FakeFlashCapDevice : CaptureDevice
{
    /// <summary>
    ///     The size of the bitmap info header that the driver keeps for the stream.
    /// </summary>
    private const int HeaderSize = 40;

    /// <summary>
    ///     The compression code of motion JPEG frames in a bitmap info header: the four characters "MJPG".
    /// </summary>
    private const int MjpgCompression = 0x47504A4D;

    /// <summary>
    ///     The compression code of YUY2 frames in a bitmap info header: the four characters "YUY2".
    /// </summary>
    private const int Yuy2Compression = 0x32595559;

    /// <summary>
    ///     FlashCap's internal routine that copies a driver frame into a pixel buffer.
    /// </summary>
    private static readonly MethodInfo CopyInMethod =
        typeof(PixelBuffer).GetMethod("CopyIn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        ?? throw new InvalidOperationException("FlashCap no longer has the PixelBuffer.CopyIn routine.");

    /// <summary>
    ///     The unmanaged copy of the bitmap info header of the stream; freed when the device is disposed.
    /// </summary>
    private readonly IntPtr _header;

    /// <summary>
    ///     The processor that FlashCap gave the device when it was initialized.
    /// </summary>
    private FrameProcessor? _processor;

    /// <summary>
    ///     The transcode format that FlashCap asked the device to use.
    /// </summary>
    private TranscodeFormats _transcode;

    /// <summary>
    ///     The numbers of times the device was started, stopped, and disposed.
    /// </summary>
    private int _startCount;

    /// <summary>
    ///     The number of times the device was stopped.
    /// </summary>
    private int _stopCount;

    /// <summary>
    ///     The number of times the device was disposed.
    /// </summary>
    private int _disposeCount;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FakeFlashCapDevice"/> class.
    /// </summary>
    /// <param name="identity">The identity of the device.</param>
    /// <param name="name">The name of the device.</param>
    public FakeFlashCapDevice(object identity, string name)
        : base(identity, name)
    {
        // Unmanaged memory is used because FlashCap reads the header through a pointer
        _header = Marshal.AllocHGlobal(HeaderSize);
        for (var offset = 0; offset < HeaderSize; offset += sizeof(int))
        {
            Marshal.WriteInt32(_header, offset, 0);
        }
    }

    /// <summary>
    ///     Gets or sets the failure that starting the device throws, or <see langword="null"/> to start normally.
    /// </summary>
    public Exception? StartFailure { get; set; }

    /// <summary>
    ///     Gets the video characteristics that FlashCap initialized the device with, or <see langword="null"/>
    ///     before the device was opened.
    /// </summary>
    public VideoCharacteristics? InitializedWith { get; private set; }

    /// <summary>
    ///     Gets the number of times the device was started.
    /// </summary>
    public int StartCount => Volatile.Read(ref _startCount);

    /// <summary>
    ///     Gets the number of times the device was stopped.
    /// </summary>
    public int StopCount => Volatile.Read(ref _stopCount);

    /// <summary>
    ///     Gets the number of times the device was disposed.
    /// </summary>
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    /// <summary>
    ///     Delivers one motion JPEG frame to the application, as the driver of a camera would.
    /// </summary>
    /// <param name="jpeg">The bytes of the JPEG frame.</param>
    /// <param name="frameIndex">The frame number.</param>
    /// <exception cref="InvalidOperationException">Thrown when the device has not been initialized.</exception>
    public void InjectJpeg(byte[] jpeg, long frameIndex) =>
        Inject(jpeg, frameIndex, 0, 0, 0, MjpgCompression);

    /// <summary>
    ///     Delivers one raw 24-bit frame to the application, as the driver of a camera would.
    /// </summary>
    /// <param name="pixels">The pixel bytes of the frame, without any header.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="frameIndex">The frame number.</param>
    /// <exception cref="InvalidOperationException">Thrown when the device has not been initialized.</exception>
    public void InjectRgb24(byte[] pixels, int width, int height, long frameIndex) =>
        Inject(pixels, frameIndex, width, height, 24, 0);

    /// <summary>
    ///     Delivers one raw YUY2 (YUYV) frame to the application, as the driver of a camera would.
    /// </summary>
    /// <param name="pixels">The pixel bytes of the frame, without any header: two bytes per pixel.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <param name="frameIndex">The frame number.</param>
    /// <exception cref="InvalidOperationException">Thrown when the device has not been initialized.</exception>
    public void InjectYuy2(byte[] pixels, int width, int height, long frameIndex) =>
        Inject(pixels, frameIndex, width, height, 16, Yuy2Compression);

    /// <summary>
    ///     Delivers one frame with the given bitmap header to the application.
    /// </summary>
    /// <param name="image">The bytes of the frame, as the driver delivers them.</param>
    /// <param name="frameIndex">The frame number.</param>
    /// <param name="width">The width in the header.</param>
    /// <param name="height">The height in the header.</param>
    /// <param name="bitsPerPixel">The bit count in the header.</param>
    /// <param name="compression">The compression code in the header.</param>
    private void Inject(byte[] image, long frameIndex, int width, int height, short bitsPerPixel, int compression)
    {
        var processor = _processor ?? throw new InvalidOperationException("The fake device is not initialized.");

        // The driver keeps the bitmap info header of the stream; FlashCap reads it with every frame
        Marshal.WriteInt32(_header, 0, HeaderSize);
        Marshal.WriteInt32(_header, 4, width);
        Marshal.WriteInt32(_header, 8, height);
        Marshal.WriteInt16(_header, 12, 1);
        Marshal.WriteInt16(_header, 14, bitsPerPixel);
        Marshal.WriteInt32(_header, 16, compression);
        Marshal.WriteInt32(_header, 20, image.Length);

        var handle = GCHandle.Alloc(image, GCHandleType.Pinned);
        try
        {
            processor.OnFrameArrived(this, handle.AddrOfPinnedObject(), image.Length, frameIndex * 33_000, frameIndex);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    ///     Gives the device what a real device receives when FlashCap initializes it.
    /// </summary>
    /// <param name="characteristics">The video characteristics that were requested.</param>
    /// <param name="transcodeFormat">The transcode format that FlashCap asked for.</param>
    /// <param name="frameProcessor">The processor that queues the frames and calls the application.</param>
    internal void Attach(VideoCharacteristics characteristics, TranscodeFormats transcodeFormat, FrameProcessor frameProcessor)
    {
        InitializedWith = characteristics;
        _transcode = transcodeFormat;
        _processor = frameProcessor;
    }

    /// <inheritdoc />
    protected override Task OnInitializeAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct)
    {
        Attach(characteristics, transcodeFormat, frameProcessor);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnStartAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _startCount);
        return StartFailure != null ? Task.FromException(StartFailure) : Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnStopAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _stopCount);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnDisposeAsync()
    {
        // Free the header once, on the first disposal; no frame is injected after disposal
        if (Interlocked.Increment(ref _disposeCount) == 1)
        {
            Marshal.FreeHGlobal(_header);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnCapture(IntPtr pData, int size, long timestampMicroseconds, long frameIndex, PixelBuffer buffer) =>
        CopyInMethod.Invoke(buffer, [_header, pData, size, timestampMicroseconds, frameIndex, _transcode]);
}
