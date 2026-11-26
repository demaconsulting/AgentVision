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

using System.Diagnostics;
using DemaConsulting.AgentVision.Core.Cameras;
using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Core.Imaging;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests.Hardware;

/// <summary>
///     Smoke tests that run against the real cameras of the machine. They are opt-in: they run only when the
///     environment variable <c>AGENTVISION_HARDWARE_TESTS</c> is <c>1</c>, which <c>build.ps1 -Hardware</c> sets.
/// </summary>
/// <remarks>
///     <para>
///         The tests are not linked to any requirement: a skipped test cannot be evidence, and the standard build
///         must pass on machines without a camera. They exist so that the developer of a machine with cameras can
///         check, in one command, what the camera-less suite cannot: real devices, real drivers, and real latency.
///     </para>
///     <para>
///         A test that needs more cameras than the machine has is skipped and says so. Only one entry per camera name
///         is used, because one physical camera appears once per backend on Windows and cannot be opened twice.
///     </para>
///     <para>
///         One process at a time can open a camera, but the test hosts of the two target frameworks run at the same
///         time. Each test therefore holds an exclusive lock file in the temporary folder for its whole run, so the
///         hardware tests of all test hosts run one after another. The lock belongs to the user and the machine; it
///         is released by the operating system when a test host ends, even if it crashes.
///     </para>
/// </remarks>
public sealed class CameraHardwareSmokeTests : IDisposable
{
    /// <summary>
    ///     The name of the environment variable that switches the hardware tests on.
    /// </summary>
    private const string SwitchName = "AGENTVISION_HARDWARE_TESTS";

    /// <summary>
    ///     The message that explains why the tests were skipped.
    /// </summary>
    private const string SkipReason = "Hardware tests are opt-in: set AGENTVISION_HARDWARE_TESTS=1 (build.ps1 -Hardware).";

    /// <summary>
    ///     Gets a value that says whether the hardware tests are switched on.
    /// </summary>
    public static bool HardwareEnabled => Environment.GetEnvironmentVariable(SwitchName) == "1";

    /// <summary>
    ///     The lock that keeps the test hosts of the other target frameworks away from the cameras, or
    ///     <c>null</c> when the hardware tests are switched off.
    /// </summary>
    private readonly HardwareLock? _hardwareLock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraHardwareSmokeTests"/> class. When the hardware tests are
    ///     switched on, it waits until no other test host runs a hardware test.
    /// </summary>
    public CameraHardwareSmokeTests()
    {
        _hardwareLock = HardwareEnabled ? new HardwareLock() : null;
    }

    /// <summary>
    ///     Releases the lock, after the test has closed its cameras.
    /// </summary>
    public void Dispose()
    {
        _hardwareLock?.Dispose();
    }

    /// <summary>
    ///     Lists the cameras of the machine, one per distinct name, and skips the test when there is none.
    /// </summary>
    /// <param name="minimum">The number of cameras the test needs.</param>
    /// <returns>The cameras, at least <paramref name="minimum"/> of them.</returns>
    /// <remarks>
    ///     Cameras of the legacy VideoForWindows backend are left out: that backend needs a window message pump,
    ///     which a test host and a console tool do not have, so FlashCap cannot open it and the open times out. The
    ///     same physical cameras are covered through their DirectShow or MediaFoundation entries.
    /// </remarks>
    private static List<CameraInfo> RequireCameras(int minimum)
    {
        var cameras = new CameraEnumerator().Enumerate()
            .Where(camera => camera.Formats.Count > 0)
            .Where(camera => camera.Backend != nameof(FlashCap.DeviceTypes.VideoForWindows))
            .GroupBy(camera => camera.Name)
            .Select(group => group.First())
            .ToList();
        Assert.SkipWhen(cameras.Count < minimum, $"The test needs {minimum} camera(s); this machine has {cameras.Count}.");
        return cameras;
    }

    /// <summary>
    ///     Opens the first camera with the defaults, takes a snapshot, and decodes it.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Open_FirstCameraDefaults_SnapshotDecodes()
    {
        // Arrange: the first camera of the machine
        var camera = RequireCameras(1)[0];

        // Act: open it with the defaults and take a snapshot
        using var session = new CameraOpener().Open(camera);
        var frame = session.Snapshot();
        using var image = frame.ToImage();

        // Assert: the picture has a plausible size and the camera reports the format that was opened
        Assert.True(image.Width > 0 && image.Height > 0);
        Assert.Contains(session.Format, camera.Formats);
    }

    /// <summary>
    ///     Opens every camera at once, keeps them streaming, and takes a snapshot from each.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Open_EveryCameraAtOnce_EachSnapshotDecodes()
    {
        // Arrange: every camera of the machine
        var cameras = RequireCameras(1);
        var opener = new CameraOpener();
        var sessions = new List<CameraSession>();

        try
        {
            // Act: open them all, then snapshot each while all stay open
            sessions.AddRange(cameras.Select(camera => opener.Open(camera)));
            var sizes = sessions.Select(session =>
            {
                using var image = session.Snapshot().ToImage();
                return (image.Width, image.Height);
            }).ToList();

            // Assert: every camera delivered a picture
            Assert.All(sizes, size => Assert.True(size.Width > 0 && size.Height > 0));
        }
        finally
        {
            sessions.ForEach(session => session.Dispose());
        }
    }

    /// <summary>
    ///     Opens a camera by its identifier and by its name.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Open_ByIdAndByName_OpenSameCamera()
    {
        // Arrange: the first camera of the machine
        var camera = RequireCameras(1)[0];
        var opener = new CameraOpener();

        // Act: open it by identifier, close it, and open it again by name
        using (var byId = opener.Open(camera.Id))
        {
            Assert.Equal(camera.Id, byId.Camera.Id);
        }

        using var byName = opener.Open(camera.Name);

        // Assert: the name reaches a camera of that name
        Assert.Equal(camera.Name, byName.Camera.Name);
    }

    /// <summary>
    ///     Opens two cameras and closes them in either order.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Close_TwoCamerasEitherOrder_ReleasesBoth()
    {
        // Arrange: two different cameras
        var cameras = RequireCameras(2);
        var opener = new CameraOpener();

        // Act: open both and close the first before the second, then the second before the first
        for (var round = 0; round < 2; round++)
        {
            var first = opener.Open(cameras[0]);
            var second = opener.Open(cameras[1]);
            first.Snapshot();
            second.Snapshot();
            var order = round == 0 ? new[] { first, second } : [second, first];
            order[0].Dispose();
            order[1].Dispose();

            // Assert: both are closed
            Assert.True(first.IsClosed && second.IsClosed);
        }
    }

    /// <summary>
    ///     Measures that a snapshot of a camera that is already streaming returns without waiting.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Snapshot_AfterFirst_ReturnsInUnder50Milliseconds()
    {
        // Arrange: an open camera whose first snapshot has returned
        var camera = RequireCameras(1)[0];
        using var session = new CameraOpener().Open(camera);
        session.Snapshot();

        // Act: take 20 more snapshots and time the slowest
        var slowest = TimeSpan.Zero;
        for (var count = 0; count < 20; count++)
        {
            var started = Stopwatch.GetTimestamp();
            session.Snapshot();
            var elapsed = Stopwatch.GetElapsedTime(started);
            slowest = elapsed > slowest ? elapsed : slowest;
        }

        // Assert: no snapshot of a streaming camera waits
        Assert.True(slowest < TimeSpan.FromMilliseconds(50), $"The slowest snapshot took {slowest.TotalMilliseconds:0.0} ms.");
    }

    /// <summary>
    ///     Saves a cropped PNG and a cropped JPEG through the one-shot snapper.
    /// </summary>
    [Fact(Skip = SkipReason, SkipUnless = nameof(HardwareEnabled), SkipType = typeof(CameraHardwareSmokeTests))]
    public void CameraHardware_Snap_CroppedPngAndJpeg_WritesFiles()
    {
        // Arrange: the first camera of the machine and a folder
        var camera = RequireCameras(1)[0];
        using var folder = new TempDirectory();
        var snapper = new CameraSnapper();
        var crop = new CropRectangle(0, 0, 32, 32);

        // Act: snap twice, once to each format
        var png = snapper.Snap(new SnapRequest(camera.Id, folder.File("crop.png")) { Crop = crop });
        var jpeg = snapper.Snap(new SnapRequest(camera.Id, folder.File("crop.jpg")) { Crop = crop });

        // Assert: both files exist with the size of the crop
        Assert.Equal((32, 32), (png.Width, png.Height));
        Assert.Equal((32, 32), (jpeg.Width, jpeg.Height));
        Assert.True(new FileInfo(png.OutputPath).Length > 0);
        Assert.True(new FileInfo(jpeg.OutputPath).Length > 0);
    }

    /// <summary>
    ///     An exclusive lock on a file in the temporary folder, shared by all test hosts of the user.
    /// </summary>
    /// <remarks>
    ///     The share mode <see cref="FileShare.None"/> is a mandatory lock on Windows and an advisory lock on Unix, so
    ///     either way a second process cannot hold the file at the same time. The operating system releases the lock
    ///     when the process ends, so a crashed test host cannot leave it behind. The file itself is never deleted,
    ///     because deleting it would race with a process that is about to open it.
    /// </remarks>
    private sealed class HardwareLock : IDisposable
    {
        /// <summary>
        ///     The time to wait for another test host before the test fails.
        /// </summary>
        private static readonly TimeSpan WaitLimit = TimeSpan.FromMinutes(10);

        /// <summary>
        ///     The time to wait between two attempts to take the lock.
        /// </summary>
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

        /// <summary>
        ///     The open lock file; closing it releases the lock.
        /// </summary>
        private readonly FileStream _stream;

        /// <summary>
        ///     Initializes a new instance of the <see cref="HardwareLock"/> class and waits for the lock.
        /// </summary>
        /// <exception cref="TimeoutException">Another test host held the lock for longer than the wait limit.</exception>
        public HardwareLock()
        {
            var path = Path.Combine(Path.GetTempPath(), "agentvision-hardware-tests.lock");
            var started = Stopwatch.GetTimestamp();

            while (true)
            {
                try
                {
                    _stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return;
                }
                catch (IOException) when (Stopwatch.GetElapsedTime(started) < WaitLimit)
                {
                    // Another test host holds the lock; try again shortly
                    Thread.Sleep(RetryDelay);
                }
                catch (IOException exception)
                {
                    throw new TimeoutException(
                        $"Another test host held the hardware test lock file '{path}' for more than {WaitLimit.TotalMinutes:0} minutes.",
                        exception);
                }
            }
        }

        /// <summary>
        ///     Releases the lock.
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
