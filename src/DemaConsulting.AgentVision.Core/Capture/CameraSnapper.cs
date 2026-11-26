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
using DemaConsulting.AgentVision.Core.Imaging;

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     What a one-shot snapshot should do: which camera, where to save, and how.
/// </summary>
/// <remarks>
///     The values are checked by <see cref="CameraSnapper.Snap"/> before any camera is touched.
/// </remarks>
/// <param name="Camera">
///     The camera to use: its index as <c>agentvision cameras</c> lists it, its identifier, or its name.
/// </param>
/// <param name="OutputPath">
///     The file to write. The extension (<c>.png</c>, <c>.jpg</c>, <c>.jpeg</c>, or <c>.bmp</c>) chooses the format.
/// </param>
public sealed record SnapRequest(string Camera, string OutputPath)
{
    /// <summary>
    ///     Gets the region to keep, or <see langword="null"/> to save the whole frame.
    /// </summary>
    public CropRectangle? Crop { get; init; }

    /// <summary>
    ///     Gets the format request, warm-up, and timeouts. The defaults suit a typical webcam.
    /// </summary>
    public CameraOpenOptions Options { get; init; } = new();
}

/// <summary>
///     What a one-shot snapshot produced.
/// </summary>
/// <param name="OutputPath">The file that was written, as it was requested.</param>
/// <param name="Width">The width of the saved picture in pixels, after cropping.</param>
/// <param name="Height">The height of the saved picture in pixels, after cropping.</param>
/// <param name="Camera">The camera that was used.</param>
/// <param name="Format">The format that the camera streamed in, which can differ from the request.</param>
public sealed record SnapResult(string OutputPath, int Width, int Height, CameraInfo Camera, CameraFormat Format);

/// <summary>
///     Takes one picture: opens a camera, lets it warm up, saves one frame (optionally cropped), and closes the
///     camera again.
/// </summary>
/// <remarks>
///     <para>
///         The unit exists so that "take a picture" is one library call and the command-line tool stays a thin
///         front-end. It is the cold path: every call pays the camera's start-up and warm-up time. Applications that
///         need many pictures use <see cref="CameraOpener"/> and keep the <see cref="CameraSession"/>.
///     </para>
///     <para>
///         Everything that can be checked without the camera (the output path and its extension, the folder, the
///         crop rectangle against the size of the format that will be used) is checked first, so a wrong request
///         fails at once instead of after the warm-up. The camera is always closed, also when a step fails. The type
///         keeps no state besides its source.
///     </para>
/// </remarks>
public sealed class CameraSnapper
{
    /// <summary>
    ///     The source that lists and opens the cameras.
    /// </summary>
    private readonly ICameraSource _source;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraSnapper"/> class that uses the cameras of the host.
    /// </summary>
    public CameraSnapper()
        : this(new FlashCapCameraSource())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraSnapper"/> class that uses the cameras of a source.
    /// </summary>
    /// <param name="source">The source that lists and opens the cameras. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public CameraSnapper(ICameraSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>
    ///     Takes one picture and saves it.
    /// </summary>
    /// <param name="request">What to capture and where to save it.</param>
    /// <returns>The file that was written and the size and camera it came from.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> or one of its text values is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the camera or output path is empty, the output extension is not supported (the message lists the
    ///     supported ones), or the camera name is shared by several cameras of one backend.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when an option is invalid, or the crop is not positive, is negative, or does not fit inside the
    ///     picture (checked against the chosen format before the camera is opened, and again against the decoded
    ///     frame).
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the folder of the output path does not exist.</exception>
    /// <exception cref="CameraNotFoundException">Thrown when no camera matches the request.</exception>
    /// <exception cref="CameraOpenException">Thrown when the camera cannot be opened or offers no usable format.</exception>
    /// <exception cref="CameraTimeoutException">Thrown when the camera delivers no frame in time.</exception>
    /// <exception cref="CameraDisconnectedException">Thrown when the camera fails while it is used.</exception>
    /// <exception cref="InvalidDataException">Thrown when the frame is damaged and cannot be decoded.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the file may not be written.</exception>
    /// <remarks>
    ///     The camera is closed before the method returns or throws. An existing output file is overwritten.
    /// </remarks>
    public SnapResult Snap(SnapRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Camera);
        ArgumentNullException.ThrowIfNull(request.OutputPath);
        ArgumentNullException.ThrowIfNull(request.Options);

        // Step 1: everything that does not need the camera
        request.Options.Validate(nameof(request));
        ValidateOutputPath(request.OutputPath);
        if (request.Crop is { } crop && (crop.X < 0 || crop.Y < 0 || crop.Width <= 0 || crop.Height <= 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"The crop area {crop} (x,y,width,height) is not valid: x and y must not be negative and " +
                "width and height must be positive.");
        }

        // Step 2: choose the camera and the format now, so that a crop outside the picture fails before the warm-up
        var cameras = new CameraEnumerator(_source).Enumerate();
        var camera = CameraSelector.SelectCamera(cameras, request.Camera);
        var format = CameraSelector.SelectFormat(camera, request.Options);
        if (request.Crop is { } area
            && ((long)area.X + area.Width > format.Width || (long)area.Y + area.Height > format.Height))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"The crop area {area} (x,y,width,height) does not fit inside the {format.Width}x{format.Height} " +
                $"picture that camera '{camera.Name}' will deliver. Choose a smaller area or another size.");
        }

        // Step 3: open, take one frame, and always close the camera
        using var session = new CameraOpener(_source).Open(camera, request.Options);
        var frame = session.Snapshot();

        // Step 4: decode, crop against the real size (the camera may deliver another size than it announced), save
        using var image = frame.ToImage();
        using var cropped = request.Crop is { } region ? image.Crop(region) : null;
        var result = cropped ?? image;
        result.Save(request.OutputPath);

        return new SnapResult(request.OutputPath, result.Width, result.Height, camera, session.Format);
    }

    /// <summary>
    ///     Checks the output path without touching the camera.
    /// </summary>
    /// <param name="outputPath">The path to check.</param>
    /// <exception cref="ArgumentException">Thrown when the path is empty or the extension is not supported.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the folder of the path does not exist.</exception>
    private static void ValidateOutputPath(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("The output path must not be empty.", nameof(outputPath));
        }

        if (!CameraImage.TryGetFormat(outputPath, out _))
        {
            throw new ArgumentException(
                $"The output file '{outputPath}' has an extension that is not supported. Use .png, .jpg, .jpeg, or .bmp.",
                nameof(outputPath));
        }

        // A missing folder would only surface after the warm-up, when the picture is saved
        var folder = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException($"The folder '{folder}' of the output file does not exist.");
        }
    }
}
