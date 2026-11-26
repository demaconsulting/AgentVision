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
using DemaConsulting.AgentVision.Tool.Cli;
using DemaConsulting.AgentVision.Tool.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Tool.Tests;

/// <summary>
///     Unit tests for the CamerasCommand class.
/// </summary>
[Collection("Sequential")]
public class CamerasCommandTests
{
    /// <summary>
    ///     Runs the command with the given cameras and returns what it wrote to the console.
    /// </summary>
    /// <param name="cameras">The cameras the fake source reports.</param>
    /// <param name="arguments">The command-line arguments of the context.</param>
    /// <param name="exitCode">Receives the exit code of the context.</param>
    /// <returns>The console output.</returns>
    private static string RunCommand(IReadOnlyList<CameraInfo> cameras, string[] arguments, out int exitCode)
    {
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(arguments);

            CamerasCommand.Run(context, new CameraEnumerator(new FakeCameraSource(cameras)));

            exitCode = context.ExitCode;
            return outWriter.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that the command reports that no camera was found and still succeeds.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_NoCameras_WritesNoCamerasFoundAndExitCodeZero()
    {
        // Arrange: a source without cameras
        IReadOnlyList<CameraInfo> cameras = [];

        // Act: run the command
        var output = RunCommand(cameras, [], out var exitCode);

        // Assert: the plain message is written, and finding nothing is not an error
        Assert.Equal("No cameras found.", output.TrimEnd());
        Assert.Equal(0, exitCode);
    }

    /// <summary>
    ///     Test that the command lists each camera with its details and formats.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_WithCameras_ListsCamerasAndFormats()
    {
        // Arrange: two cameras, the first with two formats of different widths
        IReadOnlyList<CameraInfo> cameras =
        [
            new CameraInfo(
                "V4L2:/dev/video0",
                "Desk Camera",
                "Desk Camera (V4L2)",
                "V4L2",
                [new CameraFormat(1920, 1080, "JPEG", 30), new CameraFormat(640, 480, "YUYV", 15)]),
            new CameraInfo("V4L2:/dev/video2", "Side Camera", "Side Camera (V4L2)", "V4L2", [])
        ];

        // Act: run the command
        var output = RunCommand(cameras, [], out var exitCode);

        // Assert: the summary, each camera under its index, and the aligned formats are written
        Assert.Equal(0, exitCode);
        Assert.Contains("Cameras found: 2", output);
        Assert.Contains("Camera 0: Desk Camera", output);
        Assert.Contains("  Id:          V4L2:/dev/video0", output);
        Assert.Contains("  Backend:     V4L2", output);
        Assert.Contains("  Description: Desk Camera (V4L2)", output);
        Assert.Contains("  Formats (2):", output);
        Assert.Contains("    1920x1080  JPEG  30 fps", output);
        Assert.Contains("    640x480    YUYV  15 fps", output);
        Assert.Contains("Camera 1: Side Camera", output);
        Assert.Contains("  Formats:     none that can be captured", output);
    }

    /// <summary>
    ///     Test that a fractional frame rate is printed with the invariant culture and without trailing zeros.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_FractionalFrameRate_FormatsWithoutTrailingZeros()
    {
        // Arrange: formats with a fractional, a whole, and a long frame rate
        IReadOnlyList<CameraInfo> cameras =
        [
            new CameraInfo(
                "V4L2:/dev/video0",
                "Camera",
                "Camera",
                "V4L2",
                [
                    new CameraFormat(1280, 720, "YUYV", 29.97),
                    new CameraFormat(640, 480, "YUYV", 30.0),
                    new CameraFormat(320, 240, "YUYV", 29.97002997)
                ])
        ];

        // Act: run the command
        var output = RunCommand(cameras, [], out _);

        // Assert: the rates use a dot, at most three decimals, and no trailing zeros
        Assert.Contains("  29.97 fps", output);
        Assert.Contains("  30 fps", output);
        Assert.DoesNotContain("30.0", output);
        Assert.DoesNotContain("29.9700", output);
    }

    /// <summary>
    ///     Test that a silent context keeps the listing off the console.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_SilentContext_WritesNothingToConsole()
    {
        // Arrange: a source with one camera and the silent flag
        IReadOnlyList<CameraInfo> cameras = [new CameraInfo("V4L2:/dev/video0", "Camera", "Camera", "V4L2", [])];

        // Act: run the command with the silent flag
        var output = RunCommand(cameras, ["--silent"], out var exitCode);

        // Assert: nothing reached the console, and the run still succeeded
        Assert.Equal(string.Empty, output);
        Assert.Equal(0, exitCode);
    }

    /// <summary>
    ///     Test that the listing is also written to the log file when logging is on.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_LogFile_ReceivesOutput()
    {
        // Arrange: a source with one camera and a unique log file
        IReadOnlyList<CameraInfo> cameras = [new CameraInfo("V4L2:/dev/video0", "Log Camera", "Camera", "V4L2", [])];
        var logFile = Path.Combine(Path.GetTempPath(), $"cameras_test_{Guid.NewGuid()}.log");
        try
        {
            // Act: run the command with a silent context that logs
            RunCommand(cameras, ["--silent", "--log", logFile], out _);

            // Assert: the log file holds the listing
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("Cameras found: 1", logContent);
            Assert.Contains("Camera 0: Log Camera", logContent);
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
    ///     Test that a null context is rejected.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_NullContext_ThrowsArgumentNullException()
    {
        // Arrange: a valid enumerator
        var enumerator = new CameraEnumerator(new FakeCameraSource([]));

        // Act / Assert: a null context throws
        Assert.Throws<ArgumentNullException>(() => CamerasCommand.Run(null!, enumerator));
    }

    /// <summary>
    ///     Test that a null enumerator is rejected.
    /// </summary>
    [Fact]
    public void CamerasCommand_Run_NullEnumerator_ThrowsArgumentNullException()
    {
        // Arrange: a valid context
        using var context = Context.Create(["--silent"]);

        // Act / Assert: a null enumerator throws
        Assert.Throws<ArgumentNullException>(() => CamerasCommand.Run(context, null!));
    }
}
