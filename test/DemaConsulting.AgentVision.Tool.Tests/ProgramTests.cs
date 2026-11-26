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

using DemaConsulting.AgentVision.Tool.Cli;

namespace DemaConsulting.AgentVision.Tool.Tests;

/// <summary>
///     Unit tests for the Program class.
/// </summary>
[Collection("Sequential")]
public class ProgramTests
{
    /// <summary>
    ///     Test that Run with version flag displays version only.
    /// </summary>
    [Fact]
    public void Program_Run_WithVersionFlag_DisplaysVersionOnly()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--version"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains(Program.Version, output);
            Assert.DoesNotContain("Copyright", output);
            Assert.DoesNotContain("AgentVision Tool version", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with help flag displays usage information.
    /// </summary>
    [Fact]
    public void Program_Run_WithHelpFlag_DisplaysUsageInformation()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--help"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Commands:", output);
            Assert.Contains("cameras", output);
            Assert.Contains("Options:", output);
            Assert.Contains("--version", output);
            Assert.Contains("--help", output);
            Assert.Contains($"{HelpCommand.ToolName} help [topic]", output);
            Assert.EndsWith(HelpCommand.Default.Footer, output.TrimEnd());
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with validate flag runs validation.
    /// </summary>
    [Fact]
    public void Program_Run_WithValidateFlag_RunsValidation()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--validate"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Total Tests:", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with no arguments displays the banner and the guidance for a missing command.
    /// </summary>
    [Fact]
    public void Program_Run_NoArguments_DisplaysDefaultBehavior()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("AgentVision Tool version", output);
            Assert.Contains("Copyright", output);
            Assert.Contains("No command given.", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with the cameras command lists the cameras or reports that there are none.
    /// </summary>
    [Fact]
    public void Program_Run_CamerasCommand_ListsOrReportsNone()
    {
        // Arrange: setup test conditions; the real source is used, so the result depends on the machine
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["cameras"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: the banner is printed, one of the two results is reported, and the run succeeds
            var output = outWriter.ToString();
            Assert.Contains("AgentVision Tool version", output);
            Assert.True(
                output.Contains("No cameras found.") || output.Contains("Cameras found:"),
                $"Neither camera result was reported: {output}");
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with the snap command runs it: a camera that does not exist is reported as an error.
    /// </summary>
    [Fact]
    public void Program_Run_SnapCommand_RunsSnap()
    {
        // Arrange: the snap command for camera 99, which no machine has; the real source is used but never opened
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var path = Path.Combine(Path.GetTempPath(), $"program_snap_{Guid.NewGuid()}.png");
        try
        {
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            using var context = Context.Create(["snap", "--camera", "99", "--output", path]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: the banner is printed, the snap command ran and reported the camera, and the run failed
            Assert.Contains("AgentVision Tool version", outWriter.ToString());
            Assert.Contains("Error: Camera '99' was not found", errorWriter.ToString());
            Assert.Equal(1, context.ExitCode);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that the usage text lists the snap command and its options.
    /// </summary>
    [Fact]
    public void Program_Run_Help_ListsSnapAndOptions()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--help"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: the command and each of its options are listed
            var output = outWriter.ToString();
            Assert.Contains("snap", output);
            foreach (var option in new[] { "--camera", "--output", "--crop", "--width", "--height", "--fps", "--warmup" })
            {
                Assert.Contains(option, output);
            }

            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run without a command points to the usage text and the help topics.
    /// </summary>
    [Fact]
    public void Program_Run_NoCommand_DisplaysGuidance()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: the guidance names both the usage text and the help topics, and is not an error
            var output = outWriter.ToString();
            Assert.Contains($"{HelpCommand.ToolName} --help", output);
            Assert.Contains($"{HelpCommand.ToolName} help", output);
            Assert.DoesNotContain("Cameras found", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that version property returns non-empty version string.
    /// </summary>
    [Fact]
    public void Program_Version_ReturnsNonEmptyString()
    {
        // Act: execute the operation being tested
        var version = Program.Version;

        // Assert: verify expected behavior
        Assert.False(string.IsNullOrWhiteSpace(version));
    }

    /// <summary>
    ///     Test that Main with invalid arguments returns non-zero exit code.
    /// </summary>
    [Fact]
    public void Program_Main_WithInvalidArgs_ReturnsNonZeroExitCode()
    {
        // Arrange: redirect stderr to suppress error output during test
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);

            // Act: invoke Main with an invalid argument
            var result = Program.Main(["--invalid-argument"]);

            // Assert: invalid arguments produce a non-zero exit code
            Assert.Equal(1, result);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that Main answers the help verb from the help cards before the options are parsed.
    /// </summary>
    [Fact]
    public void Program_Main_WithHelpVerb_PrintsHelpIndex()
    {
        // Arrange: capture console output
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);

            // Act: invoke Main with the help verb, which is not an option
            var result = Program.Main(["help"]);

            // Assert: the topic index is printed without the banner, and the exit code is success
            var output = outWriter.ToString();
            Assert.Equal(0, result);
            Assert.Contains($"{HelpCommand.ToolName} help {HelpCommand.RootCardIds[0]}", output);
            Assert.DoesNotContain("Copyright", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with short version flag -v displays version.
    /// </summary>
    [Fact]
    public void Program_Run_WithShortVersionFlag_DisplaysVersion()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["-v"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains(Program.Version, output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with short help flag -h displays usage.
    /// </summary>
    [Fact]
    public void Program_Run_WithShortHelpFlag_DisplaysUsage()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["-h"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Options:", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Run with short help flag -? displays usage.
    /// </summary>
    [Fact]
    public void Program_Run_WithQuestionMarkFlag_DisplaysUsage()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["-?"]);

            // Act: execute the operation being tested
            Program.Run(context);

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Options:", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

