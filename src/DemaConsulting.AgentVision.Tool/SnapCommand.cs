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

using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Tool.Cli;

namespace DemaConsulting.AgentVision.Tool;

/// <summary>
///     Implements the <c>snap</c> command: opens a camera, saves one frame to a file, and closes the camera.
/// </summary>
/// <remarks>
///     The command is a thin front-end. Everything to do with the camera (choosing it, the format, the warm-up,
///     the crop, and saving the file) belongs to the AgentVision.Core library and is done by
///     <see cref="CameraSnapper"/>; this unit only maps the options of the <see cref="Context"/> to a request,
///     reports the result or the error through the context, and so decides the exit code. Problems of the camera,
///     the request, or the file are expected and become a message and exit code 1; anything else propagates to the
///     entry point, as for the other commands.
/// </remarks>
internal static class SnapCommand
{
    /// <summary>
    ///     Takes one picture with the cameras of the host.
    /// </summary>
    /// <param name="context">The context with the options and the output. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the context lacks the camera or the output file, which a context created from a command line
    ///     that asks for the command always has.
    /// </exception>
    public static void Run(Context context) => Run(context, new CameraSnapper());

    /// <summary>
    ///     Takes one picture with the given snapper.
    /// </summary>
    /// <param name="context">The context with the options and the output. Must not be null.</param>
    /// <param name="snapper">The snapper that takes the picture. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="context"/> or <paramref name="snapper"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the context lacks the camera or the output file.
    /// </exception>
    /// <remarks>
    ///     This overload exists so tests can take a picture from a source that needs no hardware, and so that another
    ///     host of the command can supply its own snapper. The snapper closes the camera before it returns or throws.
    /// </remarks>
    public static void Run(Context context, CameraSnapper snapper)
    {
        // Reject missing collaborators before any output is produced
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(snapper);

        var request = BuildRequest(context);

        // Expected problems become one clear message and exit code 1
        try
        {
            var result = snapper.Snap(request);
            context.WriteLine(
                $"Saved {result.OutputPath} ({result.Width}x{result.Height}) from {result.Camera.Name}");
        }
        catch (Exception ex) when (ex is CameraException
                                       or ArgumentException
                                       or IOException
                                       or UnauthorizedAccessException
                                       or InvalidDataException)
        {
            context.WriteError($"Error: {Describe(ex)}");
        }
    }

    /// <summary>
    ///     Maps the options of the context to a request of the library.
    /// </summary>
    /// <param name="context">The context with the options.</param>
    /// <returns>The request.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the camera or the output file is missing.</exception>
    private static SnapRequest BuildRequest(Context context)
    {
        var camera = context.Camera
                     ?? throw new InvalidOperationException("The 'snap' command requires --camera <index|id|name>");
        var output = context.OutputFile
                     ?? throw new InvalidOperationException("The 'snap' command requires --output <file>");

        // Options that were not given keep the defaults of the library
        var options = new CameraOpenOptions
        {
            Width = context.Width,
            Height = context.Height,
            FramesPerSecond = context.Fps
        };
        if (context.WarmupMilliseconds is { } warmup)
        {
            options = options with { WarmUp = TimeSpan.FromMilliseconds(warmup) };
        }

        return new SnapRequest(camera, output) { Crop = context.Crop, Options = options };
    }

    /// <summary>
    ///     Gets the message of an exception without the parameter suffix that the runtime appends to argument
    ///     exceptions, which names a parameter of the library that means nothing on the command line.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The message.</returns>
    private static string Describe(Exception exception)
    {
        var message = exception.Message;
        if (exception is ArgumentException { ParamName: { Length: > 0 } name })
        {
            var suffix = $" (Parameter '{name}')";
            var position = message.IndexOf(suffix, StringComparison.Ordinal);
            if (position >= 0)
            {
                message = message.Remove(position, suffix.Length);
            }
        }

        return message;
    }
}
