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

namespace DemaConsulting.AgentVision.Tool.Tests.TestSupport;

/// <summary>
///     A hand-written camera source for tests of the cameras and snap commands: it reports a fixed list and opens
///     fake devices that stream synthetic frames, so the commands can be tested without camera hardware.
/// </summary>
/// <param name="cameras">The cameras the source reports.</param>
internal sealed class FakeCameraSource(IReadOnlyList<CameraInfo> cameras) : ICameraSource
{
    /// <summary>
    ///     The pause between two frames of a streaming fake camera.
    /// </summary>
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(5);

    /// <summary>
    ///     The number of devices that were opened.
    /// </summary>
    private int _openCount;

    /// <summary>
    ///     The number of devices that were disposed.
    /// </summary>
    private int _disposeCount;

    /// <summary>
    ///     Gets the exception that <see cref="Open"/> throws, or <see langword="null"/> to open normally.
    /// </summary>
    public Exception? OpenFailure { get; init; }

    /// <summary>
    ///     Gets a value that says whether the opened devices deliver frames; when <see langword="false"/> the camera
    ///     stays silent, like a camera that never starts.
    /// </summary>
    public bool Streams { get; init; } = true;

    /// <summary>
    ///     Gets the exception that the opened device reports as its failure, as a camera that is unplugged does, or
    ///     <see langword="null"/> for a device that does not fail.
    /// </summary>
    public Exception? FaultAfterOpen { get; init; }

    /// <summary>
    ///     Gets the number of devices that were opened.
    /// </summary>
    public int OpenCount => Volatile.Read(ref _openCount);

    /// <summary>
    ///     Gets the number of devices that were disposed.
    /// </summary>
    public int DisposeCount => Volatile.Read(ref _disposeCount);

    /// <summary>
    ///     Gets the format of the device that was opened last, or <see langword="null"/> when none was opened.
    /// </summary>
    public CameraFormat? LastFormat { get; private set; }

    /// <summary>
    ///     Gets the cameras that the source reports.
    /// </summary>
    /// <returns>The fixed list given to the constructor.</returns>
    public IReadOnlyList<CameraInfo> GetCameras() => cameras;

    /// <summary>
    ///     Opens a fake device that delivers synthetic frames of the opened size until it is disposed, or throws the
    ///     configured failure.
    /// </summary>
    /// <param name="camera">The camera to open.</param>
    /// <param name="format">The format to open.</param>
    /// <param name="frameArrived">The receiver of the frames.</param>
    /// <param name="faulted">The receiver of the failures.</param>
    /// <returns>The device, which stops delivering frames and is counted when it is disposed.</returns>
    public IDisposable Open(
        CameraInfo camera,
        CameraFormat format,
        Action<EncodedFrame> frameArrived,
        Action<Exception> faulted)
    {
        if (OpenFailure != null)
        {
            throw OpenFailure;
        }

        Interlocked.Increment(ref _openCount);
        LastFormat = format;
        var device = new Device(() => Interlocked.Increment(ref _disposeCount));
        if (FaultAfterOpen != null)
        {
            faulted(FaultAfterOpen);
        }

        if (Streams)
        {
            _ = Task.Run(() => StreamAsync(format, frameArrived, device.Token), CancellationToken.None);
        }

        return device;
    }

    /// <summary>
    ///     Delivers a synthetic frame every few milliseconds until the token is canceled.
    /// </summary>
    /// <param name="format">The format that decides the size of the frames.</param>
    /// <param name="frameArrived">The receiver of the frames.</param>
    /// <param name="cancellationToken">The token that is canceled when the device is disposed.</param>
    /// <returns>A task that ends when the device is disposed.</returns>
    private static async Task StreamAsync(
        CameraFormat format,
        Action<EncodedFrame> frameArrived,
        CancellationToken cancellationToken)
    {
        long index = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                index++;
                frameArrived(new EncodedFrame(GradientBmp(format.Width, format.Height), TimeSpan.FromMilliseconds(index * 33), index));
                await Task.Delay(FrameInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // The device was disposed: the stream ends
        }
    }

    /// <summary>
    ///     Builds an uncompressed 24-bit BMP file, bottom-up as FlashCap writes it, whose pixel at (x, y) is
    ///     (x, y, 128) modulo 256, so that every position can be told apart after a crop.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <returns>The bytes of the BMP file.</returns>
    private static byte[] GradientBmp(int width, int height)
    {
        // Rows are padded to a multiple of four bytes
        var stride = ((width * 3) + 3) / 4 * 4;
        var pixelBytes = stride * height;
        var data = new byte[14 + 40 + pixelBytes];

        // File header: signature, file size, pixel data offset
        data[0] = (byte)'B';
        data[1] = (byte)'M';
        BitConverter.GetBytes(data.Length).CopyTo(data, 2);
        BitConverter.GetBytes(14 + 40).CopyTo(data, 10);

        // Info header: size, width, height, planes, bits per pixel, no compression, pixel byte count
        BitConverter.GetBytes(40).CopyTo(data, 14);
        BitConverter.GetBytes(width).CopyTo(data, 18);
        BitConverter.GetBytes(height).CopyTo(data, 22);
        BitConverter.GetBytes((short)1).CopyTo(data, 26);
        BitConverter.GetBytes((short)24).CopyTo(data, 28);
        BitConverter.GetBytes(pixelBytes).CopyTo(data, 34);

        // Pixels are stored as blue, green, red, and the first stored row is the bottom row
        for (var y = 0; y < height; y++)
        {
            var row = height - 1 - y;
            for (var x = 0; x < width; x++)
            {
                var offset = 54 + (row * stride) + (x * 3);
                data[offset] = 128;
                data[offset + 1] = (byte)y;
                data[offset + 2] = (byte)x;
            }
        }

        return data;
    }

    /// <summary>
    ///     A fake device: disposing it stops the stream and is counted by the source.
    /// </summary>
    /// <param name="disposed">Called once, when the device is disposed for the first time.</param>
    private sealed class Device(Action disposed) : IDisposable
    {
        /// <summary>
        ///     The source of the token that ends the stream.
        /// </summary>
        private readonly CancellationTokenSource _stop = new();

        /// <summary>
        ///     The number of times the device was disposed.
        /// </summary>
        private int _disposals;

        /// <summary>
        ///     Gets the token that is canceled when the device is disposed.
        /// </summary>
        public CancellationToken Token => _stop.Token;

        /// <summary>
        ///     Stops the stream and reports the first disposal.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposals) == 1)
            {
                _stop.Cancel();
                _stop.Dispose();
                disposed();
            }
        }
    }
}
