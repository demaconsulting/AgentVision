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

using System.Globalization;
using DemaConsulting.CanvasNet.Canvas;
using DemaConsulting.CanvasNet.Codecs;

namespace DemaConsulting.AgentVision.Core.Imaging;

/// <summary>
///     A decoded picture from a camera: its pixels can be cropped and saved as PNG, JPEG, or BMP.
/// </summary>
/// <remarks>
///     <para>
///         The type is the library's own image type: it hides the imaging library behind it, so no type of that
///         library appears in the public API. An image owns its pixels, so it must be disposed when it is no
///         longer needed; cropping returns a new image that owns its own copy of the region and that is
///         independent of the source image.
///     </para>
///     <para>
///         An image is not thread-safe: do not use one instance from several threads at once. Different
///         instances, including a source image and its crops, may be used from different threads.
///     </para>
/// </remarks>
public sealed class CameraImage : IDisposable
{
    /// <summary>
    ///     The quality that JPEG files are saved with: high enough that text and faces stay legible, far
    ///     smaller than lossless.
    /// </summary>
    private const int JpegQuality = 90;

    /// <summary>
    ///     The pixels of the image, or <see langword="null"/> after the image is disposed.
    /// </summary>
    private Surface? _surface;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraImage"/> class that owns the given pixels.
    /// </summary>
    /// <param name="surface">The decoded pixels. The image takes ownership.</param>
    private CameraImage(Surface surface)
    {
        _surface = surface;
        Width = surface.Width;
        Height = surface.Height;
    }

    /// <summary>
    ///     Gets the width of the image in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    ///     Gets the height of the image in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    ///     Decodes the bytes of an image file, such as the encoded data of a captured frame.
    /// </summary>
    /// <param name="encoded">The complete bytes of a JPEG, PNG, or BMP file.</param>
    /// <returns>The decoded image. The caller must dispose it.</returns>
    /// <exception cref="InvalidDataException">
    ///     Thrown when the bytes are not a supported image or are damaged (for example a frame cut short). A
    ///     JPEG that lacks Huffman tables, as many webcams send, is repaired and decodes normally.
    /// </exception>
    public static CameraImage Decode(ReadOnlyMemory<byte> encoded) => new(FrameDecoder.Decode(encoded));

    /// <summary>
    ///     Determines the image file format that a path asks for, from its extension.
    /// </summary>
    /// <param name="path">A file path or name. May be null or empty, which has no format.</param>
    /// <param name="format">
    ///     When the method returns <see langword="true"/>, the format of the file; otherwise
    ///     <see cref="ImageFileFormat.Png"/>.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> when the extension is <c>.png</c>, <c>.jpg</c>, <c>.jpeg</c>, or <c>.bmp</c>
    ///     (any letter case); otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     Applications use the method to reject an output path before they spend time on capturing a picture
    ///     that could not be saved.
    /// </remarks>
    public static bool TryGetFormat(string? path, out ImageFileFormat format)
    {
        // A missing or extension-less path has no format
        var extension = string.IsNullOrEmpty(path) ? string.Empty : Path.GetExtension(path);
        switch (extension.ToLowerInvariant())
        {
            case ".png":
                format = ImageFileFormat.Png;
                return true;
            case ".jpg":
            case ".jpeg":
                format = ImageFileFormat.Jpeg;
                return true;
            case ".bmp":
                format = ImageFileFormat.Bmp;
                return true;
            default:
                format = ImageFileFormat.Png;
                return false;
        }
    }

    /// <summary>
    ///     Copies a rectangular region of the image into a new image.
    /// </summary>
    /// <param name="area">The region to copy, in pixels from the top-left corner of this image.</param>
    /// <returns>A new image of the size of the region. The caller must dispose it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when the region has a negative position, a width or height that is not positive, or does not
    ///     lie completely inside the image. The message names the region and the size of the image.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when this image has been disposed.</exception>
    /// <remarks>
    ///     The result owns its own pixels, so changing or disposing either image never affects the other.
    /// </remarks>
    public CameraImage Crop(CropRectangle area)
    {
        var surface = GetSurface();

        // Check the region first so that the message names the region and the image, which the imaging
        // library's own message does not
        if (area.X < 0 || area.Y < 0 || area.Width <= 0 || area.Height <= 0
            || (long)area.X + area.Width > Width || (long)area.Y + area.Height > Height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(area),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The crop area {0} (x,y,width,height) does not fit inside the {1}x{2} image: " +
                    "x and y must not be negative, width and height must be positive, and the area must not extend " +
                    "past the right or bottom edge.",
                    area,
                    Width,
                    Height));
        }

        return new CameraImage(surface.Crop(area.X, area.Y, area.Width, area.Height));
    }

    /// <summary>
    ///     Saves the image to a file, in the format that the file extension names.
    /// </summary>
    /// <param name="path">
    ///     The file to write: <c>.png</c>, <c>.jpg</c>, <c>.jpeg</c>, or <c>.bmp</c> (any letter case). An
    ///     existing file is overwritten. The directory must exist.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="path"/> is empty or its extension is not supported; the message lists the
    ///     supported extensions.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be written, for example when the directory is missing.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the file may not be written.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this image has been disposed.</exception>
    public void Save(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var surface = GetSurface();

        // The extension is the only statement of the wanted format, so an unknown one must fail loudly
        if (!TryGetFormat(path, out var format))
        {
            throw new ArgumentException(
                $"The output file '{path}' has an extension that is not supported. Use .png, .jpg, .jpeg, or .bmp.",
                nameof(path));
        }

        // Write through a file stream that is closed on every path, so a failed save never holds the file open
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(surface, stream, format);
    }

    /// <summary>
    ///     Writes the image to a stream in the given format.
    /// </summary>
    /// <param name="stream">The writable stream that receives the complete image file. It is not closed.</param>
    /// <param name="format">The file format to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="stream"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is not a defined format.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when this image has been disposed.</exception>
    public void Save(Stream stream, ImageFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Write(GetSurface(), stream, format);
    }

    /// <summary>
    ///     Releases the pixels of the image. Disposing more than once has no effect.
    /// </summary>
    public void Dispose()
    {
        _surface?.Dispose();
        _surface = null;
    }

    /// <summary>
    ///     Writes pixels to a stream with the codec of the format.
    /// </summary>
    /// <param name="surface">The pixels to write.</param>
    /// <param name="stream">The stream that receives the file.</param>
    /// <param name="format">The file format.</param>
    private static void Write(Surface surface, Stream stream, ImageFileFormat format)
    {
        // A camera picture is opaque, so the formats that can drop the alpha channel do: files are smaller
        switch (format)
        {
            case ImageFileFormat.Png:
                PngCodec.Save(surface, stream, PngColorType.Rgb);
                break;
            case ImageFileFormat.Jpeg:
                JpegCodec.Save(surface, stream, JpegQuality);
                break;
            case ImageFileFormat.Bmp:
                BmpCodec.Save(surface, stream, BmpBitDepth.Bit24);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "The image file format is not supported.");
        }
    }

    /// <summary>
    ///     Gets the pixels, or fails when the image has been disposed.
    /// </summary>
    /// <returns>The pixels of the image.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this image has been disposed.</exception>
    private Surface GetSurface() =>
        _surface ?? throw new ObjectDisposedException(nameof(CameraImage));
}
