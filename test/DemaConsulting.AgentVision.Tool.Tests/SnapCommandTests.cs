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
using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Core.Imaging;
using DemaConsulting.AgentVision.Tool.Cli;
using DemaConsulting.AgentVision.Tool.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Tool.Tests;

/// <summary>
///     Unit tests for the SnapCommand class.
/// </summary>
[Collection("Sequential")]
public class SnapCommandTests
{
    /// <summary>
    ///     The camera that the fake source reports, with a large and a small format.
    /// </summary>
    private static readonly CameraInfo DeskCamera = new(
        "Fake:0",
        "Desk Camera",
        "Desk Camera (Fake)",
        "Fake",
        [new CameraFormat(64, 48, "JPEG", 30), new CameraFormat(32, 24, "JPEG", 15)]);

    /// <summary>
    ///     What a run of the command wrote and returned.
    /// </summary>
    /// <param name="Output">The text written to the console output.</param>
    /// <param name="Error">The text written to the console error channel.</param>
    /// <param name="ExitCode">The exit code of the context.</param>
    private sealed record RunResult(string Output, string Error, int ExitCode);

    /// <summary>
    ///     Runs the command over a fake source and captures what it writes.
    /// </summary>
    /// <param name="source">The fake source.</param>
    /// <param name="arguments">The command-line arguments of the context.</param>
    /// <returns>The captured output, error output, and exit code.</returns>
    private static RunResult RunCommand(FakeCameraSource source, params string[] arguments)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            using var context = Context.Create(arguments);

            SnapCommand.Run(context, new CameraSnapper(source));

            return new RunResult(outWriter.ToString(), errorWriter.ToString(), context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Creates a fake source with the desk camera.
    /// </summary>
    /// <returns>The source.</returns>
    private static FakeCameraSource CreateSource() => new([DeskCamera]);

    /// <summary>
    ///     Creates a folder for the output files of one test.
    /// </summary>
    /// <returns>The path of the new folder.</returns>
    private static string CreateFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"snap_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    ///     Reads the size of a picture file through the library.
    /// </summary>
    /// <param name="path">The picture file.</param>
    /// <returns>The width and height in pixels.</returns>
    private static (int Width, int Height) ReadSize(string path)
    {
        using var image = CameraImage.Decode(File.ReadAllBytes(path));
        return (image.Width, image.Height);
    }

    /// <summary>
    ///     Test that the command saves a picture of the fake camera and reports it.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_FakeCamera_SavesFileAndReportsIt()
    {
        // Arrange: a fake camera and an output file in a new folder
        var folder = CreateFolder();
        try
        {
            var path = Path.Combine(folder, "frame.png");

            // Act: snap camera 0 without warm-up
            var result = RunCommand(CreateSource(), "snap", "--camera", "0", "--output", path, "--warmup", "0");

            // Assert: the file has the size of the best format, the message reports it, and nothing failed
            Assert.Equal(0, result.ExitCode);
            Assert.Equal($"Saved {path} (64x48) from Desk Camera", result.Output.TrimEnd());
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal((64, 48), ReadSize(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that the camera can be chosen by its name and that the camera is closed afterwards.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_CameraByName_SavesFileAndClosesCamera()
    {
        // Arrange: a fake camera
        var folder = CreateFolder();
        try
        {
            var source = CreateSource();

            // Act: snap the camera by its name
            var result = RunCommand(
                source, "snap", "--camera", "Desk Camera", "--output", Path.Combine(folder, "a.jpg"), "--warmup", "0");

            // Assert: the file exists, and the one device that was opened was disposed
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(Path.Combine(folder, "a.jpg")));
            Assert.Equal(1, source.OpenCount);
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that the crop, size, frame rate, and warm-up options reach the library.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_WithCropAndFormat_PassesOptions()
    {
        // Arrange: a fake camera with a large and a small format
        var folder = CreateFolder();
        try
        {
            var path = Path.Combine(folder, "crop.bmp");
            var source = CreateSource();

            // Act: ask for the small format and a region of it
            var result = RunCommand(
                source,
                "snap", "--camera", "0", "--output", path, "--width", "32", "--height", "24", "--fps", "15",
                "--crop", "4,2,8,6", "--warmup", "20");

            // Assert: the small format was opened and the file holds only the region
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(new CameraFormat(32, 24, "JPEG", 15), source.LastFormat);
            Assert.Equal((8, 6), ReadSize(path));
            Assert.Contains("(8x6)", result.Output);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that an unknown camera gives an error message and exit code 1, and that no camera is opened.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_CameraNotFound_WritesErrorAndExitCodeOne()
    {
        // Arrange: a fake source with one camera and a request for another
        var folder = CreateFolder();
        try
        {
            var path = Path.Combine(folder, "none.png");
            var source = CreateSource();

            // Act: snap camera 99
            var result = RunCommand(source, "snap", "--camera", "99", "--output", path);

            // Assert: the error names the camera on the error channel, nothing was opened or written
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Error: Camera '99' was not found", result.Error);
            Assert.Contains("Desk Camera", result.Error);
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(0, source.OpenCount);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a camera that cannot be opened gives an error message and exit code 1.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_OpenFailure_WritesErrorAndExitCodeOne()
    {
        // Arrange: a source whose camera cannot be opened, as when another program holds it
        var folder = CreateFolder();
        try
        {
            var source = new FakeCameraSource([DeskCamera]) { OpenFailure = new IOException("The camera is in use.") };

            // Act: snap the camera
            var result = RunCommand(source, "snap", "--camera", "0", "--output", Path.Combine(folder, "a.png"));

            // Assert: the error names the camera and the cause
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Error:", result.Error);
            Assert.Contains("Desk Camera", result.Error);
            Assert.False(File.Exists(Path.Combine(folder, "a.png")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a camera that fails while it is used gives an error message and exit code 1, and is released.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_CameraFails_WritesErrorAndReleasesCamera()
    {
        // Arrange: a camera that reports a failure as soon as it is opened and delivers no frame
        var folder = CreateFolder();
        try
        {
            var source = new FakeCameraSource([DeskCamera])
            {
                Streams = false,
                FaultAfterOpen = new IOException("The camera was unplugged.")
            };

            // Act: snap the camera
            var result = RunCommand(
                source, "snap", "--camera", "0", "--output", Path.Combine(folder, "a.png"), "--warmup", "0");

            // Assert: the failure is reported, no file is written, and the camera was released
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Error:", result.Error);
            Assert.False(File.Exists(Path.Combine(folder, "a.png")));
            Assert.Equal(1, source.OpenCount);
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a missing output folder gives an error message and exit code 1 before the camera is opened.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_OutputDirectoryMissing_WritesErrorAndExitCodeOne()
    {
        // Arrange: an output file in a folder that does not exist
        var path = Path.Combine(Path.GetTempPath(), $"snap_missing_{Guid.NewGuid()}", "frame.png");
        var source = CreateSource();

        // Act: snap the camera
        var result = RunCommand(source, "snap", "--camera", "0", "--output", path);

        // Assert: the error says so and the camera was never opened
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("does not exist", result.Error);
        Assert.Equal(0, source.OpenCount);
    }

    /// <summary>
    ///     Test that an unsupported file extension gives a clear error message without library parameter details.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_UnsupportedExtension_WritesErrorWithoutParameterSuffix()
    {
        // Arrange: a request for a GIF file
        var source = CreateSource();

        // Act: snap the camera
        var result = RunCommand(source, "snap", "--camera", "0", "--output", "frame.gif");

        // Assert: the error lists what is supported, hides the parameter name of the library, and opens nothing
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("not supported", result.Error);
        Assert.Contains(".png", result.Error);
        Assert.DoesNotContain("(Parameter", result.Error);
        Assert.Equal(0, source.OpenCount);
    }

    /// <summary>
    ///     Test that a crop outside the picture gives an error message and exit code 1 before the camera is opened.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_CropOutsidePicture_WritesErrorAndExitCodeOne()
    {
        // Arrange: a crop that reaches beyond the 64x48 picture
        var folder = CreateFolder();
        try
        {
            var source = CreateSource();

            // Act: snap the camera
            var result = RunCommand(
                source, "snap", "--camera", "0", "--output", Path.Combine(folder, "a.png"), "--crop", "60,40,10,10");

            // Assert: the error says that the area does not fit, and the camera was never opened
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("does not fit", result.Error);
            Assert.DoesNotContain("(Parameter", result.Error);
            Assert.Equal(0, source.OpenCount);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a silent context keeps the result off the console but still saves the file.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_SilentContext_WritesNothingToConsole()
    {
        // Arrange: a fake camera
        var folder = CreateFolder();
        try
        {
            var path = Path.Combine(folder, "quiet.png");

            // Act: snap with the silent flag
            var result = RunCommand(CreateSource(), "--silent", "snap", "--camera", "0", "--output", path, "--warmup", "0");

            // Assert: nothing reached the console, the run succeeded, and the file exists
            Assert.Equal(string.Empty, result.Output);
            Assert.Equal(string.Empty, result.Error);
            Assert.Equal(0, result.ExitCode);
            Assert.True(File.Exists(path));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a silent context still fails with exit code 1 when the camera is unknown.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_SilentContextWithError_StillExitsNonZero()
    {
        // Arrange / Act: snap an unknown camera with the silent flag
        var result = RunCommand(CreateSource(), "--silent", "snap", "--camera", "7", "--output", "none.png");

        // Assert: the console is quiet, and the exit code still reports the failure
        Assert.Equal(string.Empty, result.Error);
        Assert.Equal(1, result.ExitCode);
    }

    /// <summary>
    ///     Test that the result and the errors are also written to the log file when logging is on.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_LogFile_ReceivesOutput()
    {
        // Arrange: a fake camera, a folder, and a log file
        var folder = CreateFolder();
        try
        {
            var logFile = Path.Combine(folder, "snap.log");
            var path = Path.Combine(folder, "logged.png");

            // Act: a successful snap and a failed one, both logged
            RunCommand(
                CreateSource(),
                "--silent", "--log", logFile, "snap", "--camera", "0", "--output", path, "--warmup", "0");
            var failedLog = Path.Combine(folder, "failed.log");
            RunCommand(CreateSource(), "--silent", "--log", failedLog, "snap", "--camera", "5", "--output", path);

            // Assert: the log files hold the result and the error
            Assert.Contains("Saved ", File.ReadAllText(logFile));
            Assert.Contains("Error: Camera '5' was not found", File.ReadAllText(failedLog));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    ///     Test that a context without the required options is rejected by the command that is called directly.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_ContextWithoutCamera_ThrowsInvalidOperationException()
    {
        // Arrange: a context that asks for help and so has no camera or output file
        using var context = Context.Create(["snap", "--help"]);

        // Act / Assert: the command refuses to run without them
        Assert.Throws<InvalidOperationException>(() => SnapCommand.Run(context, new CameraSnapper(CreateSource())));
    }

    /// <summary>
    ///     Test that a context without an output file is rejected by the command that is called directly.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_ContextWithoutOutput_ThrowsInvalidOperationException()
    {
        // Arrange: a context that has a camera but no output file
        using var context = Context.Create(["snap", "--camera", "0", "--help"]);

        // Act / Assert: the command refuses to run without it
        Assert.Throws<InvalidOperationException>(() => SnapCommand.Run(context, new CameraSnapper(CreateSource())));
    }

    /// <summary>
    ///     Test that a null context is rejected.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_NullContext_ThrowsArgumentNullException()
    {
        // Arrange: a valid snapper
        var snapper = new CameraSnapper(CreateSource());

        // Act / Assert: a null context throws
        Assert.Throws<ArgumentNullException>(() => SnapCommand.Run(null!, snapper));
    }

    /// <summary>
    ///     Test that a null snapper is rejected.
    /// </summary>
    [Fact]
    public void SnapCommand_Run_NullSnapper_ThrowsArgumentNullException()
    {
        // Arrange: a valid context
        using var context = Context.Create(["--silent"]);

        // Act / Assert: a null snapper throws
        Assert.Throws<ArgumentNullException>(() => SnapCommand.Run(context, null!));
    }
}
