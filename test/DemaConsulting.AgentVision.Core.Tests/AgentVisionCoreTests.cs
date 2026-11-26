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

using System.Reflection;
using System.Runtime.Versioning;
using DemaConsulting.AgentVision.Core.Cameras;
using DemaConsulting.AgentVision.Core.Capture;
using DemaConsulting.AgentVision.Core.Imaging;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests;

/// <summary>
///     System-level tests for the AgentVisionCore library.
/// </summary>
/// <remarks>
///     The package-level tests load the compiled assembly by name and verify the properties that
///     every release of the library must retain: its identity, its metadata, its target framework,
///     and its independence from the tool.
/// </remarks>
public class AgentVisionCoreTests
{
    /// <summary>
    ///     The simple name of the AgentVisionCore library assembly, which is also its NuGet package ID.
    /// </summary>
    private const string LibraryAssemblyName = "DemaConsulting.AgentVision.Core";

    /// <summary>
    ///     Loads the AgentVisionCore library assembly by name.
    /// </summary>
    /// <returns>The loaded AgentVisionCore library assembly.</returns>
    private static Assembly LoadLibrary() => Assembly.Load(new AssemblyName(LibraryAssemblyName));

    /// <summary>
    ///     Test that the library assembly loads and carries the NuGet package name.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_HasPackageName()
    {
        // Arrange: no setup required; the library is referenced by this test project

        // Act: load the library assembly by its package name
        var assembly = LoadLibrary();

        // Assert: the loaded assembly carries the package name
        Assert.Equal(LibraryAssemblyName, assembly.GetName().Name);
    }

    /// <summary>
    ///     Test that the library assembly carries the package product and company metadata.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_HasPackageMetadata()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: read the product and company metadata attributes
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

        // Assert: the metadata identifies the package and its publisher
        Assert.Multiple(
            () => Assert.Equal(LibraryAssemblyName, product),
            () => Assert.Equal("DEMA Consulting", company));
    }

    /// <summary>
    ///     Test that the library assembly loaded by the test run targets the runtime the test runs on,
    ///     so each target framework run proves the library build for that runtime.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_TargetsRunningRuntime()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: read the target framework the assembly was compiled for
        var framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        // Assert: the assembly targets the major .NET version the test is running on
        Assert.Equal($".NETCoreApp,Version=v{Environment.Version.Major}.0", framework);
    }

    /// <summary>
    ///     Test that the library assembly does not reference the AgentVision tool.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_DoesNotReferenceTool()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: list the names of every assembly the library references
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name);

        // Assert: the library never depends on the tool, keeping the tool a thin front-end
        Assert.DoesNotContain("DemaConsulting.AgentVision.Tool", references);
    }

    /// <summary>
    ///     Test that an application can enumerate the cameras of its own source through the public API.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_CustomSource_Enumerated_ReportsCameras()
    {
        // Arrange: a custom source with one camera, and an enumerator that uses it
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Custom:1",
                "Custom Camera",
                "Custom Camera (custom)",
                "Custom",
                [new CameraFormat(1280, 720, "NV12", 30), new CameraFormat(640, 480, "YUYV", 15)])
        ]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the identifier, name, and every format with its size, pixel format, and frame rate are reported
        var camera = Assert.Single(cameras);
        Assert.Equal("Custom:1", camera.Id);
        Assert.Equal("Custom Camera", camera.Name);
        Assert.Equal(
            [new CameraFormat(1280, 720, "NV12", 30), new CameraFormat(640, 480, "YUYV", 15)],
            camera.Formats);
    }

    /// <summary>
    ///     Test that a source without cameras gives an empty list and not an error.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_NoneAttached_ReturnsEmptyList()
    {
        // Arrange: an enumerator whose source has no camera
        var enumerator = new CameraEnumerator(new FakeCameraSource([]));

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the list is empty, which is a valid answer and not an error
        Assert.NotNull(cameras);
        Assert.Empty(cameras);
    }

    /// <summary>
    ///     Test that the default enumerator, which asks the real FlashCap of this machine, completes with
    ///     or without a camera.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_DefaultSource_Enumerated_DoesNotThrow()
    {
        // Arrange: the default enumerator; the result depends on the machine's cameras
        var enumerator = new CameraEnumerator();

        // Act: enumerate the cameras
        var exception = Record.Exception(() => enumerator.Enumerate());

        // Assert: enumeration completed; a machine without a camera gives an empty list, not a failure
        Assert.Null(exception);
    }

    /// <summary>
    ///     The longest a test waits for something that should happen at once.
    /// </summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Test that a hot camera hands out its latest frame at once, repeatedly, and that the frame decodes.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_FakeCamera_Snapshot_ReturnsLatestFrameImmediately()
    {
        // Arrange: an open camera that has delivered a dark and then a bright frame
        var source = TestCameras.Source();
        using var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 10, 1));
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 240, 2));

        // Act: take several snapshots, and decode the last
        var first = session.Snapshot();
        var second = session.Snapshot();
        using var image = second.ToImage();

        // Assert: both snapshots are the newest frame, which decodes to the picture that the camera delivered
        Assert.Equal((2, 2), (first.Sequence, second.Sequence));
        Assert.Equal((64, 48), (image.Width, image.Height));
    }

    /// <summary>
    ///     Test that an index, an identifier, and a name each open the same camera.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_FakeCamera_OpenByIndexIdAndName_OpensSameCamera()
    {
        // Arrange: a source with two cameras
        var source = new FakeCameraSource([TestCameras.Camera("Fake:a", "Alpha"), TestCameras.Camera("Fake:b", "Beta")]);
        var opener = new CameraOpener(source);

        // Act: open Beta by index, by identifier, and by name
        using var byIndex = opener.Open("1", TestCameras.QuickOptions());
        using var byId = opener.Open("Fake:b", TestCameras.QuickOptions());
        using var byName = opener.Open("BETA", TestCameras.QuickOptions());

        // Assert: all three sessions belong to Beta
        Assert.All(new[] { byIndex, byId, byName }, session => Assert.Equal("Fake:b", session.Camera.Id));
    }

    /// <summary>
    ///     Test that a requested size and frame rate are opened when the camera offers them.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_FakeCamera_RequestedFormat_IsOpened()
    {
        // Arrange: a camera with three sizes and rates
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Fake:0",
                "Cam",
                "Cam",
                "Fake",
                [
                    new CameraFormat(1920, 1080, "JPEG", 30),
                    new CameraFormat(1280, 720, "JPEG", 60),
                    new CameraFormat(640, 480, "JPEG", 30)
                ])
        ]);

        // Act: open with a request for 1280x720 at 60 frames per second
        using var session = new CameraOpener(source).Open(
            "0",
            new CameraOpenOptions { Width = 1280, Height = 720, FramesPerSecond = 60, WarmUp = TimeSpan.Zero });

        // Assert: the camera is opened with that format
        Assert.Equal(new CameraFormat(1280, 720, "JPEG", 60), session.Format);
    }

    /// <summary>
    ///     Test that without a request a sensible default format is opened.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_FakeCamera_NoRequest_OpensDefaultFormat()
    {
        // Arrange: a camera that offers 4K, full HD, and VGA
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Fake:0",
                "Cam",
                "Cam",
                "Fake",
                [
                    new CameraFormat(3840, 2160, "JPEG", 30),
                    new CameraFormat(1920, 1080, "JPEG", 30),
                    new CameraFormat(640, 480, "JPEG", 30)
                ])
        ]);

        // Act: open without a request
        using var session = new CameraOpener(source).Open("0", new CameraOpenOptions { WarmUp = TimeSpan.Zero });

        // Assert: full HD, the largest size that is not above full HD
        Assert.Equal(new CameraFormat(1920, 1080, "JPEG", 30), session.Format);
    }

    /// <summary>
    ///     Test that frames that arrive during the warm-up are not served.
    /// </summary>
    [Fact]
    public async Task AgentVisionCore_Capture_FakeCamera_WarmUp_DarkFramesAreNotServed()
    {
        // Arrange: a camera with a 400 ms warm-up that delivers a dark frame at once
        var source = TestCameras.Source();
        var options = new CameraOpenOptions { WarmUp = TimeSpan.FromMilliseconds(400), FirstFrameTimeout = TimeSpan.FromSeconds(10) };
        using var session = new CameraOpener(source).Open("0", options);
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 5, 1));

        // Act: let the warm-up pass, deliver a bright frame, and take a snapshot
        await Task.Delay(600, TestContext.Current.CancellationToken);
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 200, 2));
        var frame = session.Snapshot();

        // Assert: the bright frame is served, never the dark one
        Assert.Equal(2, frame.Sequence);
    }

    /// <summary>
    ///     Test that two cameras stream independently and that closing one leaves the other open.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_TwoFakeCameras_CloseOne_OtherKeepsStreaming()
    {
        // Arrange: two cameras, both open and streaming
        var source = new FakeCameraSource([TestCameras.Camera("Fake:0", "One"), TestCameras.Camera("Fake:1", "Two")]);
        var opener = new CameraOpener(source);
        var one = opener.Open("0", TestCameras.QuickOptions());
        using var two = opener.Open("1", TestCameras.QuickOptions());
        source.Devices[0].Emit(SyntheticFrames.OpaqueFrame(1));
        source.Devices[1].Emit(SyntheticFrames.OpaqueFrame(2));

        // Act: close the first camera and deliver another frame to the second
        one.Dispose();
        source.Devices[1].Emit(SyntheticFrames.OpaqueFrame(3));

        // Assert: the first is closed, and the second still serves its newest frame
        Assert.True(one.IsClosed);
        Assert.Equal(3, two.Snapshot().Sequence);
    }

    /// <summary>
    ///     Test that many threads can snapshot a streaming camera at the same time.
    /// </summary>
    [Fact]
    public async Task AgentVisionCore_Capture_FakeCamera_ConcurrentSnapshots_AllReturnFrames()
    {
        // Arrange: a camera that streams by itself
        using var session = new CameraOpener(TestCameras.StreamingSource()).Open("0", TestCameras.QuickOptions());
        session.Snapshot();

        // Act: eight threads take 200 snapshots each
        var workers = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(
                () => Enumerable.Range(0, 200).Select(_ => session.Snapshot()).ToList(),
                TestContext.Current.CancellationToken))
            .ToArray();
        var results = await Task.WhenAll(workers).WaitAsync(Patience, TestContext.Current.CancellationToken);

        // Assert: every snapshot returned a frame
        Assert.All(results, frames => Assert.Equal(200, frames.Count));
        Assert.All(results.SelectMany(frames => frames), Assert.NotNull);
    }

    /// <summary>
    ///     Test that closing a camera stops the device and that snapshots of the closed session fail.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_FakeCamera_Dispose_StopsDeviceAndRejectsSnapshot()
    {
        // Arrange: an open camera with a frame
        var source = TestCameras.Source();
        var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));

        // Act: close it
        session.Dispose();

        // Assert: the device is stopped, and snapshots fail
        Assert.True(source.LastDevice.IsDisposed);
        Assert.Throws<ObjectDisposedException>(session.Snapshot);
    }

    /// <summary>
    ///     Test that an unknown camera gives the not-found exception, which lists the cameras that exist.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_UnknownCamera_ThrowsCameraNotFound()
    {
        // Arrange: a source with one camera
        var opener = new CameraOpener(TestCameras.Source());

        // Act / Assert: opening an unknown camera is reported, with the available cameras in the message
        var exception = Assert.Throws<CameraNotFoundException>(() => opener.Open("Missing"));
        Assert.Contains("Fake Camera", exception.Message);
    }

    /// <summary>
    ///     Test that a camera that cannot be opened gives the open exception.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_OpenFails_ThrowsCameraOpen()
    {
        // Arrange: a source whose camera is held by another application
        var source = TestCameras.Source();
        source.OpenFailure = new IOException("held by another application");

        // Act / Assert: opening fails with the open exception, which names the camera and the cause
        var exception = Assert.Throws<CameraOpenException>(() => new CameraOpener(source).Open("0"));
        Assert.Contains("held by another application", exception.Message);
    }

    /// <summary>
    ///     Test that a camera that fails while open gives the disconnected exception.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_DeviceFaults_ThrowsCameraDisconnected()
    {
        // Arrange: an open camera that then fails
        var source = TestCameras.Source();
        using var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        source.LastDevice.Emit(SyntheticFrames.OpaqueFrame(1));
        source.LastDevice.Fault(new IOException("unplugged"));

        // Act / Assert: snapshots fail with the disconnected exception
        Assert.Throws<CameraDisconnectedException>(session.Snapshot);
    }

    /// <summary>
    ///     Test that a camera that delivers no frame gives the timeout exception.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Capture_NoFrame_ThrowsCameraTimeout()
    {
        // Arrange: an open camera that delivers nothing, with a short first-frame timeout
        var options = new CameraOpenOptions { WarmUp = TimeSpan.Zero, FirstFrameTimeout = TimeSpan.FromMilliseconds(100) };
        using var session = new CameraOpener(TestCameras.Source()).Open("0", options);

        // Act / Assert: the first snapshot times out
        Assert.Throws<CameraTimeoutException>(session.Snapshot);
    }

    /// <summary>
    ///     Test that a captured frame can be cropped and saved as PNG and as JPEG.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Imaging_CapturedFrame_CropAndSave_WritesPngAndJpeg()
    {
        // Arrange: a captured frame and a folder
        using var folder = new TempDirectory();
        var source = TestCameras.Source();
        using var session = new CameraOpener(source).Open("0", TestCameras.QuickOptions());
        source.LastDevice.Emit(SyntheticFrames.SolidFrame(64, 48, 180, 1));

        // Act: decode the snapshot, crop it, and save the crop in both formats
        using var image = session.Snapshot().ToImage();
        using var cropped = image.Crop(new CropRectangle(10, 10, 20, 16));
        cropped.Save(folder.File("crop.png"));
        cropped.Save(folder.File("crop.jpg"));

        // Assert: both files exist, and the crop has the size of the region
        Assert.Equal((20, 16), (cropped.Width, cropped.Height));
        Assert.True(new FileInfo(folder.File("crop.png")).Length > 0);
        Assert.True(new FileInfo(folder.File("crop.jpg")).Length > 0);
    }

    /// <summary>
    ///     Test that one call opens a camera, saves one cropped picture, and closes the camera, with the
    ///     real image libraries on every platform.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Snap_FakeCamera_SavesCroppedPng()
    {
        // Arrange: a streaming camera, and a request that crops a region and saves a PNG
        using var folder = new TempDirectory();
        var source = TestCameras.StreamingSource();
        var request = new SnapRequest("0", folder.File("snap.png"))
        {
            Crop = new CropRectangle(4, 4, 24, 16),
            Options = TestCameras.QuickOptions()
        };

        // Act: snap
        var result = new CameraSnapper(source).Snap(request);

        // Assert: the picture was saved with the size of the crop, and the camera was closed
        Assert.Equal((24, 16), (result.Width, result.Height));
        Assert.True(File.Exists(request.OutputPath));
        Assert.True(source.LastDevice.IsDisposed);
    }

    /// <summary>
    ///     Test that no FlashCap type appears in the public API of the library, so a FlashCap upgrade
    ///     cannot break consumers.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_PublicApiExposesNoFlashCapTypes()
    {
        // Arrange / Act: find the types of FlashCap that appear in public signatures of the library
        var leaked = PublicTypesInNamespace("FlashCap");

        // Assert: none are exposed
        Assert.Empty(leaked);
    }

    /// <summary>
    ///     Test that no CanvasNet type appears in the public API of the library, so a CanvasNet upgrade
    ///     cannot break consumers.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_PublicApiExposesNoCanvasNetTypes()
    {
        // Arrange / Act: find the types of CanvasNet that appear in public signatures of the library
        var leaked = PublicTypesInNamespace("DemaConsulting.CanvasNet");

        // Assert: none are exposed
        Assert.Empty(leaked);
    }

    /// <summary>
    ///     Finds the types of a third-party namespace that appear in the public signatures of the library.
    /// </summary>
    /// <param name="namespacePrefix">The namespace prefix of the third-party library.</param>
    /// <returns>The full names of the types that leak into the public API.</returns>
    private static List<string?> PublicTypesInNamespace(string namespacePrefix)
    {
        // Load the library and collect every type used in its public signatures
        var assembly = LoadLibrary();
        var used = new List<Type>();
        foreach (var type in assembly.GetExportedTypes())
        {
            // An interface has no base type
            if (type.BaseType != null)
            {
                used.Add(type.BaseType);
            }

            used.AddRange(type.GetInterfaces());
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // Non-public members are not part of the API, so only visible signatures count
                switch (member)
                {
                    case PropertyInfo property when property.GetMethod?.IsPublic == true:
                        used.Add(property.PropertyType);
                        break;
                    case MethodBase { IsPublic: true } method:
                        used.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
                        if (method is MethodInfo info)
                        {
                            used.Add(info.ReturnType);
                        }

                        break;
                    case FieldInfo { IsPublic: true } field:
                        used.Add(field.FieldType);
                        break;
                }
            }
        }

        // Find any used type, including the type arguments of generic types, that belongs to the namespace
        return used
            .SelectMany(ExpandType)
            .Where(type => type.Namespace?.StartsWith(namespacePrefix, StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .ToList();
    }

    /// <summary>
    ///     Expands a type into itself and every type it is built from: element types of arrays and
    ///     references, and the type arguments of generic types.
    /// </summary>
    /// <param name="type">The type to expand.</param>
    /// <returns>The type and the types it is built from.</returns>
    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;

        // Arrays, references, and pointers are built from their element type
        if (type.HasElementType)
        {
            foreach (var inner in ExpandType(type.GetElementType()!))
            {
                yield return inner;
            }
        }

        // Generic types are built from their type arguments
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in ExpandType(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
