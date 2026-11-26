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
using DemaConsulting.AgentVision.Core.Cameras;
using DemaConsulting.AgentVision.Tool.Cli;

namespace DemaConsulting.AgentVision.Tool;

/// <summary>
///     Implements the <c>cameras</c> command: lists the available cameras and the formats they support.
/// </summary>
/// <remarks>
///     The command is a thin front-end. All discovery and ordering belongs to the AgentVision.Core library;
///     this unit only prints what <see cref="CameraEnumerator"/> returns, through <see cref="Context"/>
///     so that <c>--silent</c> and <c>--log</c> apply. Finding no camera is a normal result, not an error,
///     so the exit code stays 0.
/// </remarks>
internal static class CamerasCommand
{
    /// <summary>
    ///     Lists the cameras of the host.
    /// </summary>
    /// <param name="context">The context that receives the output. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static void Run(Context context) => Run(context, new CameraEnumerator());

    /// <summary>
    ///     Lists the cameras that the given enumerator reports.
    /// </summary>
    /// <param name="context">The context that receives the output. Must not be null.</param>
    /// <param name="enumerator">The enumerator that lists the cameras. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="context"/> or <paramref name="enumerator"/> is null.
    /// </exception>
    /// <remarks>
    ///     This overload exists so tests can list cameras from a source that needs no hardware. Failures of
    ///     the enumerator propagate to the caller; the entry point reports them as unexpected errors.
    /// </remarks>
    public static void Run(Context context, CameraEnumerator enumerator)
    {
        // Reject missing collaborators before any output is produced
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(enumerator);

        // Finding no camera is a valid answer: say so plainly and succeed
        var cameras = enumerator.Enumerate();
        if (cameras.Count == 0)
        {
            context.WriteLine("No cameras found.");
            return;
        }

        // Summarize, then describe each camera under its index, which is its position in the list
        context.WriteLine($"Cameras found: {cameras.Count}");
        for (var index = 0; index < cameras.Count; index++)
        {
            WriteCamera(context, index, cameras[index]);
        }
    }

    /// <summary>
    ///     Writes the description of one camera.
    /// </summary>
    /// <param name="context">The context that receives the output.</param>
    /// <param name="index">The position of the camera in the list.</param>
    /// <param name="camera">The camera to describe.</param>
    private static void WriteCamera(Context context, int index, CameraInfo camera)
    {
        context.WriteLine("");
        context.WriteLine($"Camera {index}: {camera.Name}");
        context.WriteLine($"  Id:          {camera.Id}");
        context.WriteLine($"  Backend:     {camera.Backend}");
        context.WriteLine($"  Description: {camera.Description}");

        // A camera may offer no format that can be captured; it is still listed
        if (camera.Formats.Count == 0)
        {
            context.WriteLine("  Formats:     none that can be captured");
            return;
        }

        // Pad the size and pixel format columns to their longest entry so the formats line up
        context.WriteLine($"  Formats ({camera.Formats.Count}):");
        var sizes = camera.Formats.Select(format => $"{format.Width}x{format.Height}").ToList();
        var sizeWidth = sizes.Max(size => size.Length);
        var pixelWidth = camera.Formats.Max(format => format.PixelFormat.Length);
        for (var i = 0; i < camera.Formats.Count; i++)
        {
            var format = camera.Formats[i];
            var size = sizes[i].PadRight(sizeWidth);
            var pixel = format.PixelFormat.PadRight(pixelWidth);
            context.WriteLine($"    {size}  {pixel}  {FormatFrameRate(format.FramesPerSecond)} fps");
        }
    }

    /// <summary>
    ///     Formats a frame rate with the invariant culture, with at most three decimals and no trailing zeros.
    /// </summary>
    /// <param name="framesPerSecond">The frame rate in frames per second.</param>
    /// <returns>The frame rate text, such as <c>30</c> or <c>29.97</c>.</returns>
    private static string FormatFrameRate(double framesPerSecond) =>
        framesPerSecond.ToString("0.###", CultureInfo.InvariantCulture);
}
