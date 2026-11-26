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
using DemaConsulting.AgentVision.Core.Imaging;

namespace DemaConsulting.AgentVision.Tool.Cli;

/// <summary>
///     Context class that handles command-line arguments and program output.
/// </summary>
internal sealed class Context : IDisposable
{
    /// <summary>
    ///     The longest warm-up that <c>--warmup</c> accepts: one minute, far beyond what any camera needs.
    /// </summary>
    private const int MaxWarmupMilliseconds = 60000;

    /// <summary>
    ///     Log file stream writer (if logging is enabled).
    /// </summary>
    private StreamWriter? _logWriter;

    /// <summary>
    ///     Indicates whether errors have been reported.
    /// </summary>
    private bool _hasErrors;

    /// <summary>
    ///     Gets a value indicating whether the version flag was specified.
    /// </summary>
    public bool Version { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the help flag was specified.
    /// </summary>
    public bool Help { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the silent flag was specified.
    /// </summary>
    public bool Silent { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the validate flag was specified.
    /// </summary>
    public bool Validate { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the <c>cameras</c> command was specified.
    /// </summary>
    public bool Cameras { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the <c>snap</c> command was specified.
    /// </summary>
    public bool Snap { get; private init; }

    /// <summary>
    ///     Gets the camera of the <c>snap</c> command (index, identifier, or name), or <see langword="null"/> when
    ///     <c>--camera</c> was not given.
    /// </summary>
    public string? Camera { get; private init; }

    /// <summary>
    ///     Gets the file that the <c>snap</c> command writes, or <see langword="null"/> when <c>--output</c> was not
    ///     given.
    /// </summary>
    public string? OutputFile { get; private init; }

    /// <summary>
    ///     Gets the region of the picture to keep, or <see langword="null"/> to keep the whole picture.
    /// </summary>
    public CropRectangle? Crop { get; private init; }

    /// <summary>
    ///     Gets the requested frame width in pixels, or <see langword="null"/> for no preference.
    /// </summary>
    public int? Width { get; private init; }

    /// <summary>
    ///     Gets the requested frame height in pixels, or <see langword="null"/> for no preference.
    /// </summary>
    public int? Height { get; private init; }

    /// <summary>
    ///     Gets the requested frame rate in frames per second, or <see langword="null"/> for no preference.
    /// </summary>
    public double? Fps { get; private init; }

    /// <summary>
    ///     Gets the warm-up in milliseconds, or <see langword="null"/> for the default of the library.
    /// </summary>
    public int? WarmupMilliseconds { get; private init; }

    /// <summary>
    ///     Gets the validation results file path.
    /// </summary>
    public string? ResultsFile { get; private init; }

    /// <summary>
    ///     Gets the heading depth for markdown output (default is 1).
    /// </summary>
    public int HeadingDepth { get; private init; } = 1;

    /// <summary>
    ///     Gets the proposed exit code for the application (0 for success, 1 for errors).
    /// </summary>
    public int ExitCode => _hasErrors ? 1 : 0;

    /// <summary>
    ///     Private constructor - use Create factory method instead.
    /// </summary>
    private Context()
    {
    }

    /// <summary>
    ///     Creates a Context instance from command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A new Context instance.</returns>
    /// <exception cref="ArgumentException">Thrown when arguments are invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the specified log file cannot be opened.</exception>
    public static Context Create(string[] args)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(args);

        var parser = new ArgumentParser();
        parser.ParseArguments(args);

        var result = new Context
        {
            Version = parser.Version,
            Help = parser.Help,
            Silent = parser.Silent,
            Validate = parser.Validate,
            Cameras = parser.Cameras,
            Snap = parser.Snap,
            Camera = parser.Camera,
            OutputFile = parser.OutputFile,
            Crop = parser.Crop,
            Width = parser.Width,
            Height = parser.Height,
            Fps = parser.Fps,
            WarmupMilliseconds = parser.WarmupMilliseconds,
            ResultsFile = parser.ResultsFile,
            HeadingDepth = parser.HeadingDepth
        };

        // Open log file if specified
        if (parser.LogFile != null)
        {
            result.OpenLogFile(parser.LogFile);
        }

        return result;
    }

    /// <summary>
    ///     Opens the log file for writing
    /// </summary>
    /// <param name="logFile">Log file path</param>
    private void OpenLogFile(string logFile)
    {
        try
        {
            // Open with AutoFlush enabled so log entries are immediately written to disk
            // even if the application terminates unexpectedly before Dispose is called
            _logWriter = new StreamWriter(logFile, append: false) { AutoFlush = true };
        }
        // Generic catch is justified here to wrap any file system exception with context.
        // Expected exceptions include IOException, UnauthorizedAccessException, ArgumentException,
        // NotSupportedException, and other file system-related exceptions.
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open log file '{logFile}': {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Helper class for parsing command-line arguments
    /// </summary>
    private sealed class ArgumentParser
    {
        /// <summary>
        ///     Gets a value indicating whether the version flag was specified.
        /// </summary>
        public bool Version { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the help flag was specified.
        /// </summary>
        public bool Help { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the silent flag was specified.
        /// </summary>
        public bool Silent { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the validate flag was specified.
        /// </summary>
        public bool Validate { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the <c>cameras</c> command was specified.
        /// </summary>
        public bool Cameras { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the <c>snap</c> command was specified.
        /// </summary>
        public bool Snap { get; private set; }

        /// <summary>
        ///     Gets the camera of the <c>snap</c> command.
        /// </summary>
        public string? Camera { get; private set; }

        /// <summary>
        ///     Gets the output file of the <c>snap</c> command.
        /// </summary>
        public string? OutputFile { get; private set; }

        /// <summary>
        ///     Gets the crop region of the <c>snap</c> command.
        /// </summary>
        public CropRectangle? Crop { get; private set; }

        /// <summary>
        ///     Gets the requested frame width.
        /// </summary>
        public int? Width { get; private set; }

        /// <summary>
        ///     Gets the requested frame height.
        /// </summary>
        public int? Height { get; private set; }

        /// <summary>
        ///     Gets the requested frame rate.
        /// </summary>
        public double? Fps { get; private set; }

        /// <summary>
        ///     Gets the warm-up in milliseconds.
        /// </summary>
        public int? WarmupMilliseconds { get; private set; }

        /// <summary>
        ///     Gets the log file path.
        /// </summary>
        public string? LogFile { get; private set; }

        /// <summary>
        ///     Gets the validation results file path.
        /// </summary>
        public string? ResultsFile { get; private set; }

        /// <summary>
        ///     Gets the heading depth for markdown output.
        /// </summary>
        public int HeadingDepth { get; private set; } = 1;

        /// <summary>
        ///     Parses command-line arguments
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public void ParseArguments(string[] args)
        {
            // Validate input
            ArgumentNullException.ThrowIfNull(args);

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];
                i = ParseArgument(arg, args, i);
            }

            ValidateCombination();
        }

        /// <summary>
        ///     Checks that the commands and options that were given belong together.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when two commands are given, a <c>snap</c> option is given without the command, or the
        ///     <c>snap</c> command lacks <c>--camera</c> or <c>--output</c>.
        /// </exception>
        private void ValidateCombination()
        {
            // Only one command may be given, because each command is a whole run of its own
            if (Snap && Cameras)
            {
                throw new ArgumentException("Only one command can be given: use either 'snap' or 'cameras'");
            }

            // A snap option does nothing without the command, which is almost certainly a mistake
            if (!Snap)
            {
                var stray = FirstSnapOption();
                if (stray != null)
                {
                    throw new ArgumentException($"{stray} can be used only with the 'snap' command");
                }

                return;
            }

            // The snap command needs its camera and its file, unless the user only asked for help or the version
            if (Help || Version)
            {
                return;
            }

            if (Camera == null)
            {
                throw new ArgumentException("The 'snap' command requires --camera <index|id|name>");
            }

            if (OutputFile == null)
            {
                throw new ArgumentException("The 'snap' command requires --output <file>");
            }
        }

        /// <summary>
        ///     Finds the first option of the <c>snap</c> command that was given.
        /// </summary>
        /// <returns>The option name, or <see langword="null"/> when none was given.</returns>
        private string? FirstSnapOption()
        {
            if (Camera != null)
            {
                return "--camera";
            }

            if (OutputFile != null)
            {
                return "--output";
            }

            if (Crop != null)
            {
                return "--crop";
            }

            if (Width != null)
            {
                return "--width";
            }

            if (Height != null)
            {
                return "--height";
            }

            if (Fps != null)
            {
                return "--fps";
            }

            return WarmupMilliseconds != null ? "--warmup" : null;
        }

        /// <summary>
        ///     Parses a single argument
        /// </summary>
        /// <param name="arg">Argument to parse</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <returns>Updated index</returns>
        private int ParseArgument(string arg, string[] args, int index)
        {
            switch (arg)
            {
                case "-v":
                case "--version":
                    Version = true;
                    return index;

                case "-?":
                case "-h":
                case "--help":
                    Help = true;
                    return index;

                case "--silent":
                    Silent = true;
                    return index;

                case "--validate":
                    Validate = true;
                    return index;

                case "cameras":
                    // A command may be given only once; repeating it is almost certainly a mistake
                    if (Cameras)
                    {
                        throw new ArgumentException("The 'cameras' command can be given only once", nameof(args));
                    }

                    Cameras = true;
                    return index;

                case "snap":
                    // A command may be given only once; repeating it is almost certainly a mistake
                    if (Snap)
                    {
                        throw new ArgumentException("The 'snap' command can be given only once", nameof(args));
                    }

                    Snap = true;
                    return index;

                case "--camera":
                    Camera = GetRequiredStringArgument(arg, args, index, "a camera index, identifier, or name argument");
                    return index + 1;

                case "--output":
                    OutputFile = GetRequiredStringArgument(arg, args, index, "an output filename argument");
                    return index + 1;

                case "--crop":
                    Crop = GetRequiredCropArgument(arg, args, index);
                    return index + 1;

                case "--width":
                    Width = GetRequiredIntArgument(arg, args, index, "a width in pixels");
                    return index + 1;

                case "--height":
                    Height = GetRequiredIntArgument(arg, args, index, "a height in pixels");
                    return index + 1;

                case "--fps":
                    Fps = GetRequiredFrameRateArgument(arg, args, index);
                    return index + 1;

                case "--warmup":
                    WarmupMilliseconds = GetRequiredIntArgument(
                        arg, args, index, "a warm-up in milliseconds", 0, MaxWarmupMilliseconds);
                    return index + 1;

                case "--log":
                    LogFile = GetRequiredStringArgument(arg, args, index, "a filename argument");
                    return index + 1;

                case "--results":
                case "--result":
                    ResultsFile = GetRequiredStringArgument(arg, args, index, "a results filename argument");
                    return index + 1;

                case "--depth":
                    HeadingDepth = GetRequiredIntArgument(arg, args, index, "a heading depth argument", 1, 6);
                    return index + 1;

                default:
                    throw new ArgumentException($"Unsupported argument '{arg}'", nameof(args));
            }
        }

        /// <summary>
        ///     Gets a required string argument value
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <param name="description">Description of what's required</param>
        /// <returns>Argument value</returns>
        private static string GetRequiredStringArgument(string arg, string[] args, int index, string description)
        {
            if (index >= args.Length)
            {
                throw new ArgumentException($"{arg} requires {description}", nameof(args));
            }

            return args[index];
        }

        /// <summary>
        ///     Gets a required integer argument value
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <param name="description">Description of what's required</param>
        /// <param name="min">Minimum valid value (inclusive)</param>
        /// <param name="max">Maximum valid value (inclusive)</param>
        /// <returns>Argument value as an integer in [min, max]</returns>
        private static int GetRequiredIntArgument(string arg, string[] args, int index, string description, int min = 1, int max = int.MaxValue)
        {
            var value = GetRequiredStringArgument(arg, args, index, description);
            if (!int.TryParse(value, out var result) || result < min || result > max)
            {
                throw new ArgumentException($"{arg} requires an integer between {min} and {max} for {description}", nameof(args));
            }

            return result;
        }

        /// <summary>
        ///     Gets a required crop region argument in the form <c>x,y,width,height</c>
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <returns>The crop region</returns>
        private static CropRectangle GetRequiredCropArgument(string arg, string[] args, int index)
        {
            var value = GetRequiredStringArgument(arg, args, index, "a region argument x,y,width,height");

            // Four integers, in the invariant culture so that the result does not depend on the machine
            var parts = value.Split(',');
            var numbers = new int[4];
            var valid = parts.Length == numbers.Length;
            for (var i = 0; valid && i < numbers.Length; i++)
            {
                valid = int.TryParse(
                    parts[i], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out numbers[i]);
            }

            if (!valid || numbers[0] < 0 || numbers[1] < 0 || numbers[2] <= 0 || numbers[3] <= 0)
            {
                throw new ArgumentException(
                    $"{arg} requires four integers x,y,width,height, where x and y are not negative and " +
                    $"width and height are positive, but got '{value}'",
                    nameof(args));
            }

            return new CropRectangle(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        /// <summary>
        ///     Gets a required frame rate argument, a number above zero
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <returns>The frame rate in frames per second</returns>
        private static double GetRequiredFrameRateArgument(string arg, string[] args, int index)
        {
            var value = GetRequiredStringArgument(arg, args, index, "a frame rate in frames per second");
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                || !double.IsFinite(result)
                || result <= 0)
            {
                throw new ArgumentException(
                    $"{arg} requires a number above 0 for a frame rate in frames per second, but got '{value}'",
                    nameof(args));
            }

            return result;
        }
    }

    /// <summary>
    ///     Writes a line of output to the console and log file (if logging is enabled).
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <remarks>
    ///     Output is written to stdout. When <see cref="Silent"/> is <c>true</c>, stdout output is
    ///     suppressed, but the message is still written to the log file when one is open.
    /// </remarks>
    public void WriteLine(string message)
    {
        // Write to console unless silent mode is enabled
        if (!Silent)
        {
            Console.WriteLine(message);
        }

        // Write to log file if logging is enabled
        _logWriter?.WriteLine(message);
    }

    /// <summary>
    ///     Writes an error message to the error console and log file (if logging is enabled).
    /// </summary>
    /// <param name="message">The error message to write.</param>
    /// <remarks>
    ///     <c>_hasErrors</c> is set to <c>true</c> unconditionally, so <see cref="ExitCode"/> will
    ///     return 1 regardless of whether <see cref="Silent"/> suppresses the console output.
    ///     Stderr output is suppressed when <see cref="Silent"/> is <c>true</c>, but the message
    ///     is still written to the log file when one is open.
    /// </remarks>
    public void WriteError(string message)
    {
        // Mark that we have encountered errors
        _hasErrors = true;

        // Write to error console unless silent mode is enabled
        if (!Silent)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        // Write to log file if logging is enabled
        _logWriter?.WriteLine(message);
    }

    /// <summary>
    ///     Disposes resources used by the Context.
    /// </summary>
    public void Dispose()
    {
        // Close and dispose the log file writer if it exists
        _logWriter?.Dispose();
        _logWriter = null;
    }
}
