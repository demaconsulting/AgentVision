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
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests.Cameras;

/// <summary>
///     Unit tests for the CameraEnumerator class.
/// </summary>
public class CameraEnumeratorTests
{
    /// <summary>
    ///     Creates a camera description for a test.
    /// </summary>
    /// <param name="backend">The backend of the camera.</param>
    /// <param name="id">The identifier of the camera.</param>
    /// <param name="formats">The formats of the camera.</param>
    /// <returns>The camera description.</returns>
    private static CameraInfo Camera(string backend, string id, params CameraFormat[] formats) =>
        new(id, $"Name of {id}", $"Description of {id}", backend, formats);

    /// <summary>
    ///     Test that a null source is rejected.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Constructor_NullSource_ThrowsArgumentNullException()
    {
        // Arrange: no setup; null is the input under test

        // Act / Assert: a null source throws and names the parameter
        var exception = Assert.Throws<ArgumentNullException>(() => new CameraEnumerator(null!));
        Assert.Equal("source", exception.ParamName);
    }

    /// <summary>
    ///     Test that every camera of the source is reported with all its details.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_SourceWithCameras_ReportsEveryCameraWithDetails()
    {
        // Arrange: a source with one camera that has two formats
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "V4L2:/dev/video0",
                "Desk Camera",
                "Desk Camera (V4L2)",
                "V4L2",
                [new CameraFormat(1920, 1080, "JPEG", 30), new CameraFormat(640, 480, "YUYV", 15)])
        ]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the camera is reported with its identifier, names, backend, and both formats
        var camera = Assert.Single(cameras);
        Assert.Equal("V4L2:/dev/video0", camera.Id);
        Assert.Equal("Desk Camera", camera.Name);
        Assert.Equal("Desk Camera (V4L2)", camera.Description);
        Assert.Equal("V4L2", camera.Backend);
        Assert.Equal(
            [new CameraFormat(1920, 1080, "JPEG", 30), new CameraFormat(640, 480, "YUYV", 15)],
            camera.Formats);
    }

    /// <summary>
    ///     Test that cameras are ordered by backend and then by identifier.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_UnorderedSource_SortsByBackendThenId()
    {
        // Arrange: cameras in an order that is wrong for both keys
        var enumerator = new CameraEnumerator(new FakeCameraSource(
        [
            Camera("V4L2", "V4L2:/dev/video2"),
            Camera("DirectShow", "DirectShow:b"),
            Camera("V4L2", "V4L2:/dev/video0"),
            Camera("DirectShow", "DirectShow:a")
        ]));

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: backend decides first, and the identifier decides within a backend
        Assert.Equal(
            ["DirectShow:a", "DirectShow:b", "V4L2:/dev/video0", "V4L2:/dev/video2"],
            cameras.Select(camera => camera.Id));
    }

    /// <summary>
    ///     Test that formats are ordered from the largest frame size and the highest frame rate down.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_UnorderedFormats_SortsLargestAndFastestFirst()
    {
        // Arrange: formats in an order that is wrong for every key
        var enumerator = new CameraEnumerator(new FakeCameraSource(
        [
            Camera(
                "V4L2",
                "V4L2:/dev/video0",
                new CameraFormat(640, 480, "YUYV", 30),
                new CameraFormat(1920, 1080, "YUYV", 5),
                new CameraFormat(1920, 1080, "JPEG", 30),
                new CameraFormat(1920, 1080, "YUYV", 30),
                new CameraFormat(1280, 720, "JPEG", 60))
        ]));

        // Act: enumerate the cameras
        var formats = enumerator.Enumerate()[0].Formats;

        // Assert: frame size decides first, then frame rate, then the pixel format name
        Assert.Equal(
            [
                new CameraFormat(1920, 1080, "JPEG", 30),
                new CameraFormat(1920, 1080, "YUYV", 30),
                new CameraFormat(1920, 1080, "YUYV", 5),
                new CameraFormat(1280, 720, "JPEG", 60),
                new CameraFormat(640, 480, "YUYV", 30)
            ],
            formats);
    }

    /// <summary>
    ///     Test that a format reported twice is listed once.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_DuplicateFormats_ReportsOnce()
    {
        // Arrange: a camera that reports the same format twice
        var enumerator = new CameraEnumerator(new FakeCameraSource(
        [
            Camera(
                "V4L2",
                "V4L2:/dev/video0",
                new CameraFormat(640, 480, "YUYV", 30),
                new CameraFormat(640, 480, "YUYV", 30))
        ]));

        // Act: enumerate the cameras
        var formats = enumerator.Enumerate()[0].Formats;

        // Assert: the format appears once
        Assert.Single(formats);
    }

    /// <summary>
    ///     Test that a source without cameras gives an empty list and not an error.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_EmptySource_ReturnsEmptyList()
    {
        // Arrange: a source that reports no camera
        var enumerator = new CameraEnumerator(new FakeCameraSource([]));

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the list is empty, which is a valid answer
        Assert.NotNull(cameras);
        Assert.Empty(cameras);
    }

    /// <summary>
    ///     Test that the result does not change when the source's own list changes afterward.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_ResultIsIndependentOfSourceList_KeepsReportedCameras()
    {
        // Arrange: a source whose list is a mutable list that the test changes after enumerating
        var sourceList = new List<CameraInfo> { Camera("V4L2", "V4L2:/dev/video0") };
        var enumerator = new CameraEnumerator(new FakeCameraSource(sourceList));
        var cameras = enumerator.Enumerate();

        // Act: change the source's list after the result was taken
        sourceList.Add(Camera("V4L2", "V4L2:/dev/video1"));
        sourceList.Clear();

        // Assert: the result still holds the camera that was reported
        Assert.Equal("V4L2:/dev/video0", Assert.Single(cameras).Id);
    }

    /// <summary>
    ///     Test that two calls give equal results and ask the source each time.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_CalledTwice_ReturnsEqualResultsFromFreshQueries()
    {
        // Arrange: a source with one camera that has one format
        var source = new FakeCameraSource([Camera("V4L2", "V4L2:/dev/video0", new CameraFormat(640, 480, "YUYV", 30))]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate twice
        var first = enumerator.Enumerate();
        var second = enumerator.Enumerate();

        // Assert: the same cameras and formats are reported, and the source was asked both times
        Assert.Equal(first.Select(camera => camera.Id), second.Select(camera => camera.Id));
        Assert.Equal(first[0].Formats, second[0].Formats);
        Assert.Equal(2, source.CallCount);
    }

    /// <summary>
    ///     Test that a source that returns no list is reported as a failure of the source.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_SourceReturnsNull_ThrowsInvalidOperationException()
    {
        // Arrange: a source that breaks its contract by returning null
        var enumerator = new CameraEnumerator(new FakeCameraSource([]) { Cameras = null! });

        // Act / Assert: the broken contract is reported
        Assert.Throws<InvalidOperationException>(() => enumerator.Enumerate());
    }

    /// <summary>
    ///     Test that a failure of the source reaches the caller unchanged.
    /// </summary>
    [Fact]
    public void CameraEnumerator_Enumerate_SourceThrows_PropagatesTheException()
    {
        // Arrange: a source that fails
        var failure = new IOException("The capture library failed.");
        var enumerator = new CameraEnumerator(new FakeCameraSource([]) { Failure = failure });

        // Act / Assert: the same exception object reaches the caller
        var thrown = Assert.Throws<IOException>(() => enumerator.Enumerate());
        Assert.Same(failure, thrown);
    }
}
