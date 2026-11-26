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
using DemaConsulting.AgentVision.Tool.Cli;

namespace DemaConsulting.AgentVision.Tool.Tests;

/// <summary>
///     Unit tests for the Context class.
/// </summary>
[Collection("Sequential")]
public class ContextTests
{
    /// <summary>
    ///     Test creating a context with no arguments.
    /// </summary>
    [Fact]
    public void Context_Create_NoArguments_ReturnsDefaultContext()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.False(context.Help);
        Assert.False(context.Silent);
        Assert.False(context.Validate);
        Assert.False(context.Cameras);
        Assert.False(context.Snap);
        Assert.Null(context.Camera);
        Assert.Null(context.OutputFile);
        Assert.Null(context.ResultsFile);
        Assert.Equal(1, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the version flag.
    /// </summary>
    [Fact]
    public void Context_Create_VersionFlag_SetsVersionTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--version"]);

        // Assert: verify expected behavior
        Assert.True(context.Version);
        Assert.False(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short version flag.
    /// </summary>
    [Fact]
    public void Context_Create_ShortVersionFlag_SetsVersionTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-v"]);

        // Assert: verify expected behavior
        Assert.True(context.Version);
        Assert.False(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the help flag.
    /// </summary>
    [Fact]
    public void Context_Create_HelpFlag_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--help"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short help flag -h.
    /// </summary>
    [Fact]
    public void Context_Create_ShortHelpFlag_H_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-h"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short help flag -?.
    /// </summary>
    [Fact]
    public void Context_Create_ShortHelpFlag_Question_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-?"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the silent flag.
    /// </summary>
    [Fact]
    public void Context_Create_SilentFlag_SetsSilentTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--silent"]);

        // Assert: verify expected behavior
        Assert.True(context.Silent);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the validate flag.
    /// </summary>
    [Fact]
    public void Context_Create_ValidateFlag_SetsValidateTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--validate"]);

        // Assert: verify expected behavior
        Assert.True(context.Validate);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the results flag.
    /// </summary>
    [Fact]
    public void Context_Create_ResultsFlag_SetsResultsFile()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--results", "test.trx"]);

        // Assert: verify expected behavior
        Assert.Equal("test.trx", context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the log flag.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_OpensLogFile()
    {
        // Arrange: setup test conditions
        var logFile = Path.GetTempFileName();
        try
        {
            // Act: execute the operation being tested
            using (var context = Context.Create(["--log", logFile]))
            {
                context.WriteLine("Test message");
                Assert.Equal(0, context.ExitCode);
            }

            // Assert: verify expected behavior
            // Verify log file was written
            Assert.True(File.Exists(logFile));
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("Test message", logContent);
        }
        finally
        {
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }

    /// <summary>
    ///     Test creating a context with an unknown argument throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_UnknownArgument_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--unknown"]));
        Assert.Contains("Unsupported argument", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the cameras command sets the cameras flag.
    /// </summary>
    [Fact]
    public void Context_Create_CamerasCommand_SetsCamerasTrue()
    {
        // Act: create a context from the cameras command
        using var context = Context.Create(["cameras"]);

        // Assert: the command is recorded, no action flag is set, and there is no error
        Assert.True(context.Cameras);
        Assert.False(context.Version);
        Assert.False(context.Help);
        Assert.False(context.Validate);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test that the cameras command may stand among the options.
    /// </summary>
    [Fact]
    public void Context_Create_CamerasCommandAmongOptions_SetsCamerasAndOptions()
    {
        // Act: create a context with options before and after the command
        using var context = Context.Create(["--silent", "cameras", "--depth", "2"]);

        // Assert: the command and both options are recorded
        Assert.True(context.Cameras);
        Assert.True(context.Silent);
        Assert.Equal(2, context.HeadingDepth);
    }

    /// <summary>
    ///     Test creating a context with the cameras command twice throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_RepeatedCamerasCommand_ThrowsArgumentException()
    {
        // Act & Assert: the command can be given only once
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["cameras", "cameras"]));
        Assert.Contains("only once", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the snap command and all of its options.
    /// </summary>
    [Fact]
    public void Context_Create_SnapCommand_SetsSnapAndOptions()
    {
        // Act: create a context from the snap command with every option
        using var context = Context.Create(
            ["snap", "--camera", "Desk Camera", "--output", "frame.png", "--crop", "10,20,30,40", "--width", "1280",
             "--height", "720", "--fps", "29.97", "--warmup", "500"]);

        // Assert: the command and every option are recorded, and there is no error
        Assert.True(context.Snap);
        Assert.False(context.Cameras);
        Assert.Equal("Desk Camera", context.Camera);
        Assert.Equal("frame.png", context.OutputFile);
        Assert.Equal(new CropRectangle(10, 20, 30, 40), context.Crop);
        Assert.Equal(1280, context.Width);
        Assert.Equal(720, context.Height);
        Assert.Equal(29.97, context.Fps);
        Assert.Equal(500, context.WarmupMilliseconds);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test that the snap options that were not given stay unset, so the library defaults apply.
    /// </summary>
    [Fact]
    public void Context_Create_SnapRequiredOptionsOnly_LeavesOptionalOptionsUnset()
    {
        // Act: create a context with only the required options
        using var context = Context.Create(["snap", "--camera", "0", "--output", "frame.jpg"]);

        // Assert: the optional options are not set
        Assert.True(context.Snap);
        Assert.Null(context.Crop);
        Assert.Null(context.Width);
        Assert.Null(context.Height);
        Assert.Null(context.Fps);
        Assert.Null(context.WarmupMilliseconds);
    }

    /// <summary>
    ///     Test that the snap command and its options may stand among the other options in any order.
    /// </summary>
    [Fact]
    public void Context_Create_SnapOptionsAmongOtherOptions_SetsSnapAndOptions()
    {
        // Act: create a context with the command last and a general option in between
        using var context = Context.Create(
            ["--output", "frame.bmp", "--silent", "--camera", "1", "--warmup", "0", "snap"]);

        // Assert: everything is recorded, including a warm-up of zero
        Assert.True(context.Snap);
        Assert.True(context.Silent);
        Assert.Equal("1", context.Camera);
        Assert.Equal("frame.bmp", context.OutputFile);
        Assert.Equal(0, context.WarmupMilliseconds);
    }

    /// <summary>
    ///     Test that help can be asked for the snap command without giving its required options.
    /// </summary>
    [Fact]
    public void Context_Create_SnapWithHelp_DoesNotRequireOptions()
    {
        // Act: create a context that asks for the usage text
        using var context = Context.Create(["snap", "--help"]);

        // Assert: the command and the help flag are recorded and nothing is rejected
        Assert.True(context.Snap);
        Assert.True(context.Help);
        Assert.Null(context.Camera);
    }

    /// <summary>
    ///     Test that the snap command without a camera is rejected.
    /// </summary>
    [Fact]
    public void Context_Create_SnapWithoutCamera_ThrowsArgumentException()
    {
        // Act & Assert: the message names the missing option
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["snap", "--output", "frame.png"]));
        Assert.Contains("requires --camera", exception.Message);
    }

    /// <summary>
    ///     Test that the snap command without an output file is rejected.
    /// </summary>
    [Fact]
    public void Context_Create_SnapWithoutOutput_ThrowsArgumentException()
    {
        // Act & Assert: the message names the missing option
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["snap", "--camera", "0"]));
        Assert.Contains("requires --output", exception.Message);
    }

    /// <summary>
    ///     Test that every snap option without the snap command is rejected.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <param name="value">A valid value of the option.</param>
    [Theory]
    [InlineData("--camera", "0")]
    [InlineData("--output", "frame.png")]
    [InlineData("--crop", "0,0,1,1")]
    [InlineData("--width", "640")]
    [InlineData("--height", "480")]
    [InlineData("--fps", "30")]
    [InlineData("--warmup", "100")]
    public void Context_Create_SnapOptionWithoutSnap_ThrowsArgumentException(string option, string value)
    {
        // Act & Assert: the message names the option and the command it belongs to
        var exception = Assert.Throws<ArgumentException>(() => Context.Create([option, value]));
        Assert.Contains(option, exception.Message);
        Assert.Contains("'snap'", exception.Message);
    }

    /// <summary>
    ///     Test that the snap and cameras commands cannot be combined.
    /// </summary>
    [Fact]
    public void Context_Create_SnapAndCameras_ThrowsArgumentException()
    {
        // Act & Assert: only one command is allowed
        var exception = Assert.Throws<ArgumentException>(
            () => Context.Create(["cameras", "snap", "--camera", "0", "--output", "frame.png"]));
        Assert.Contains("Only one command", exception.Message);
    }

    /// <summary>
    ///     Test that the snap command can be given only once.
    /// </summary>
    [Fact]
    public void Context_Create_RepeatedSnapCommand_ThrowsArgumentException()
    {
        // Act & Assert: the command can be given only once
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["snap", "snap"]));
        Assert.Contains("only once", exception.Message);
    }

    /// <summary>
    ///     Test that a crop that is not four valid integers is rejected.
    /// </summary>
    /// <param name="crop">The crop value.</param>
    [Theory]
    [InlineData("1,2,3")]
    [InlineData("1,2,3,4,5")]
    [InlineData("a,b,c,d")]
    [InlineData("1,2,3,x")]
    [InlineData("1.5,2,3,4")]
    [InlineData("-1,0,10,10")]
    [InlineData("0,-1,10,10")]
    [InlineData("0,0,0,10")]
    [InlineData("0,0,10,0")]
    [InlineData("0,0,-5,10")]
    [InlineData("")]
    public void Context_Create_SnapCropInvalid_ThrowsArgumentException(string crop)
    {
        // Act & Assert: the message names the option and the expected form
        var exception = Assert.Throws<ArgumentException>(
            () => Context.Create(["snap", "--camera", "0", "--output", "frame.png", "--crop", crop]));
        Assert.Contains("--crop requires four integers x,y,width,height", exception.Message);
    }

    /// <summary>
    ///     Test that sizes and the warm-up that are not integers in range are rejected.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <param name="value">An invalid value.</param>
    [Theory]
    [InlineData("--width", "0")]
    [InlineData("--width", "-5")]
    [InlineData("--width", "wide")]
    [InlineData("--height", "0")]
    [InlineData("--height", "1.5")]
    [InlineData("--warmup", "-1")]
    [InlineData("--warmup", "60001")]
    [InlineData("--warmup", "soon")]
    public void Context_Create_SnapIntegerOptionInvalid_ThrowsArgumentException(string option, string value)
    {
        // Act & Assert: the message names the option
        var exception = Assert.Throws<ArgumentException>(
            () => Context.Create(["snap", "--camera", "0", "--output", "frame.png", option, value]));
        Assert.Contains($"{option} requires an integer between", exception.Message);
    }

    /// <summary>
    ///     Test that a frame rate that is not a number above zero is rejected.
    /// </summary>
    /// <param name="value">An invalid value.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("-30")]
    [InlineData("fast")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("")]
    public void Context_Create_SnapFpsInvalid_ThrowsArgumentException(string value)
    {
        // Act & Assert: the message names the option
        var exception = Assert.Throws<ArgumentException>(
            () => Context.Create(["snap", "--camera", "0", "--output", "frame.png", "--fps", value]));
        Assert.Contains("--fps requires a number above 0", exception.Message);
    }

    /// <summary>
    ///     Test that every snap option without its value is rejected.
    /// </summary>
    /// <param name="option">The option.</param>
    [Theory]
    [InlineData("--camera")]
    [InlineData("--output")]
    [InlineData("--crop")]
    [InlineData("--width")]
    [InlineData("--height")]
    [InlineData("--fps")]
    [InlineData("--warmup")]
    public void Context_Create_SnapOptionWithoutValue_ThrowsArgumentException(string option)
    {
        // Act & Assert: the option is the last argument, so its value is missing
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["snap", option]));
        Assert.Contains($"{option} requires", exception.Message);
    }

    /// <summary>
    ///     Test that the frame rate is read with the invariant culture, so a dot is the decimal separator everywhere.
    /// </summary>
    [Fact]
    public void Context_Create_SnapFpsWithDecimalPoint_UsesInvariantCulture()
    {
        // Arrange: a culture whose decimal separator is a comma
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            // Act: create a context with a fractional frame rate
            using var context = Context.Create(["snap", "--camera", "0", "--output", "f.png", "--fps", "7.5"]);

            // Assert: the value is 7.5
            Assert.Equal(7.5, context.Fps);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    ///     Test creating a context with an unknown command throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_UnknownCommand_ThrowsArgumentException()
    {
        // Act & Assert: a word that is not a known command is an unsupported argument
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["list"]));
        Assert.Contains("Unsupported argument 'list'", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --log flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--log"]));
        Assert.Contains("--log", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --results flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_ResultsFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--results"]));
        Assert.Contains("--results", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the --result alias flag (legacy alias for --results).
    /// </summary>
    [Fact]
    public void Context_Create_ResultAliasFlag_SetsResultsFile()
    {
        // Act: execute the operation using the legacy --result alias
        using var context = Context.Create(["--result", "test.trx"]);

        // Assert: verify --result sets ResultsFile identically to --results
        Assert.Equal("test.trx", context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with --result flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_ResultAliasFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--result"]));
        Assert.Contains("--result", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the depth flag.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_SetsHeadingDepth()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--depth", "3"]);

        // Assert: verify expected behavior
        Assert.Equal(3, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with no depth flag returns default heading depth of 1.
    /// </summary>
    [Fact]
    public void Context_Create_NoDepthFlag_ReturnsDefaultHeadingDepth()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify default depth is 1
        Assert.Equal(1, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with --depth flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and non-integer value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_NonIntegerValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "abc"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and zero value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_ZeroValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "0"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and value exceeding maximum throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_ExceedsMaxValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "7"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test WriteLine writes to console output when not silent.
    /// </summary>
    [Fact]
    public void Context_WriteLine_NotSilent_WritesToConsole()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteLine("Test message");

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Test message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test WriteLine does not write to console when silent.
    /// </summary>
    [Fact]
    public void Context_WriteLine_Silent_DoesNotWriteToConsole()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--silent"]);

            // Act: execute the operation being tested
            context.WriteLine("Test message");

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.DoesNotContain("Test message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test WriteError does not write to console when silent.
    /// </summary>
    [Fact]
    public void Context_WriteError_Silent_DoesNotWriteToConsole()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create(["--silent"]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert - error output should be suppressed in silent mode
            var output = errWriter.ToString();
            Assert.DoesNotContain("Test error message", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError sets exit code to 1.
    /// </summary>
    [Fact]
    public void Context_WriteError_SetsErrorExitCode()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert: verify expected behavior
            Assert.Equal(1, context.ExitCode);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError writes message to console when not silent.
    /// </summary>
    [Fact]
    public void Context_WriteError_NotSilent_WritesToConsole()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert: verify expected behavior
            var output = errWriter.ToString();
            Assert.Contains("Test error message", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError writes message to log file when logging is enabled.
    /// </summary>
    [Fact]
    public void Context_WriteError_WritesToLogFile()
    {
        // Arrange: setup test conditions
        var logFile = Path.GetTempFileName();
        try
        {
            // Act - use silent to avoid console output; verify the error still goes to the log
            using (var context = Context.Create(["--silent", "--log", logFile]))
            {
                context.WriteError("Test error in log");
                Assert.Equal(1, context.ExitCode);
            }

            // Assert - log file should contain the error message
            Assert.True(File.Exists(logFile));
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("Test error in log", logContent);
        }
        finally
        {
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }

    /// <summary>
    ///     Test creating a context with --log flag pointing to an invalid path throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_InvalidPath_ThrowsInvalidOperationException()
    {
        // Arrange: a path that cannot be opened as a file (directory or invalid characters)
        // Use a directory path so it cannot be opened as a file
        var invalidLogPath = Path.GetTempPath(); // temp directory itself, not a file

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Context.Create(["--log", invalidLogPath]));
    }
}


