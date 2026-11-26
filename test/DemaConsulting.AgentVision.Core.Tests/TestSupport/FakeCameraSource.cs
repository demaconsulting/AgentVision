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

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A hand-written camera source for tests: it reports a configured list, counts its calls, can be told to
///     fail, and opens <see cref="FakeCameraDevice"/> devices that the test drives, so the enumeration and
///     capture policy can be tested without camera hardware.
/// </summary>
/// <param name="cameras">The cameras the source reports.</param>
internal sealed class FakeCameraSource(IReadOnlyList<CameraInfo> cameras) : ICameraSource
{
    /// <summary>
    ///     The pause between two frames of a streaming fake camera.
    /// </summary>
    private const int EmitIntervalMilliseconds = 5;

    /// <summary>
    ///     Protects <see cref="_devices"/>, which tests read while sessions open on other threads.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    ///     The devices that were opened, in order.
    /// </summary>
    private readonly List<FakeCameraDevice> _devices = [];

    /// <summary>
    ///     Gets or sets the cameras that the source reports; tests change it to simulate a camera that is
    ///     attached or removed, or to simulate a source that breaks its contract.
    /// </summary>
    public IReadOnlyList<CameraInfo> Cameras { get; set; } = cameras;

    /// <summary>
    ///     Gets the exception that the source throws instead of reporting the cameras, or <see langword="null"/>.
    /// </summary>
    public Exception? Failure { get; init; }

    /// <summary>
    ///     Gets or sets the exception that <see cref="Open"/> throws, or <see langword="null"/> to open normally.
    /// </summary>
    public Exception? OpenFailure { get; set; }

    /// <summary>
    ///     Gets or sets the function that makes the frame with the given running number; when set, every opened
    ///     device streams frames made by it, one every few milliseconds, until it is disposed.
    /// </summary>
    public Func<long, EncodedFrame>? FrameFactory { get; set; }

    /// <summary>
    ///     Gets the number of times the source was asked for its cameras.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    ///     Gets a snapshot of the devices that were opened, in order.
    /// </summary>
    public IReadOnlyList<FakeCameraDevice> Devices
    {
        get
        {
            lock (_gate)
            {
                return _devices.ToList();
            }
        }
    }

    /// <summary>
    ///     Gets the most recently opened device.
    /// </summary>
    public FakeCameraDevice LastDevice => Devices[^1];

    /// <summary>
    ///     Reports the configured cameras or throws the configured failure.
    /// </summary>
    /// <returns>The configured list.</returns>
    public IReadOnlyList<CameraInfo> GetCameras()
    {
        CallCount++;
        if (Failure != null)
        {
            throw Failure;
        }

        return Cameras;
    }

    /// <summary>
    ///     Opens a fake device, or throws the configured open failure.
    /// </summary>
    /// <param name="camera">The camera to open.</param>
    /// <param name="format">The format to open.</param>
    /// <param name="frameArrived">The receiver of the frames.</param>
    /// <param name="faulted">The receiver of the failures.</param>
    /// <returns>The fake device, which records its disposal.</returns>
    public IDisposable Open(
        CameraInfo camera,
        CameraFormat format,
        Action<EncodedFrame> frameArrived,
        Action<Exception> faulted)
    {
        if (OpenFailure != null)
        {
            throw OpenFailure;
        }

        var device = new FakeCameraDevice(camera, format, frameArrived, faulted);
        lock (_gate)
        {
            _devices.Add(device);
        }

        // A streaming camera: deliver a frame every few milliseconds until the device is disposed
        if (FrameFactory is { } factory)
        {
            _ = Task.Factory.StartNew(
                () =>
                {
                    long index = 0;
                    while (!device.IsDisposed)
                    {
                        device.Emit(factory(++index));
                        Thread.Sleep(EmitIntervalMilliseconds);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        return device;
    }
}
