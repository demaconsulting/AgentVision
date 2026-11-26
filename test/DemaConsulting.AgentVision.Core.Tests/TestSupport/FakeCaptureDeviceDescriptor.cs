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

using FlashCap;

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A FlashCap device descriptor that belongs to no real camera, so tests can feed descriptors through
///     the real translation of the FlashCap camera source.
/// </summary>
/// <remarks>
///     FlashCap's descriptor is abstract only because opening a device is device-specific. Enumeration reads
///     the identity, device type, name, description, and characteristics, all of which this fake provides;
///     opening is never part of enumeration, so it is not supported here.
/// </remarks>
internal sealed class FakeCaptureDeviceDescriptor : CaptureDeviceDescriptor
{
    /// <summary>
    ///     The identity reported by the descriptor.
    /// </summary>
    private readonly object? _identity;

    /// <summary>
    ///     The backend type reported by the descriptor.
    /// </summary>
    private readonly DeviceTypes _deviceType;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FakeCaptureDeviceDescriptor"/> class.
    /// </summary>
    /// <param name="deviceType">The backend type to report.</param>
    /// <param name="identity">The identity to report; may be null.</param>
    /// <param name="name">The device name to report.</param>
    /// <param name="description">The device description to report.</param>
    /// <param name="characteristics">The video characteristics to report.</param>
    public FakeCaptureDeviceDescriptor(
        DeviceTypes deviceType,
        object? identity,
        string name,
        string description,
        params VideoCharacteristics[] characteristics)
        : base(name, description, characteristics, new DefaultBufferPool())
    {
        _deviceType = deviceType;
        _identity = identity;
    }

    /// <inheritdoc />
    public override object Identity => _identity!;

    /// <inheritdoc />
    public override DeviceTypes DeviceType => _deviceType;

    /// <inheritdoc />
    protected override Task<CaptureDevice> OnOpenWithFrameProcessorAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct) =>
        throw new NotSupportedException("The fake descriptor describes a device and cannot open it.");
}
