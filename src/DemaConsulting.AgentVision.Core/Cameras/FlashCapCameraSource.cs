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

namespace DemaConsulting.AgentVision.Core.Cameras;

/// <summary>
///     Reports the cameras of the host by asking FlashCap, and translates what FlashCap describes into the
///     library's own camera types.
/// </summary>
/// <remarks>
///     <para>
///         This is the only type of the library that references FlashCap. It exists so that FlashCap types
///         never cross the <see cref="ICameraSource"/> boundary: the rest of the library, and every consumer
///         of the public API, sees only <see cref="CameraInfo"/> and <see cref="CameraFormat"/>.
///     </para>
///     <para>
///         FlashCap reports one entry per capture backend, so on Windows a single physical camera usually
///         appears three times (DirectShow, Media Foundation, and Video for Windows). Every entry is
///         reported with its backend; choosing between the entries is left to the code that opens a camera.
///         The type holds no state and does not cache, so it is thread-safe whenever the FlashCap
///         enumeration it calls is.
///     </para>
/// </remarks>
internal sealed class FlashCapCameraSource : ICameraSource
{
    /// <summary>
    ///     Enumerates the FlashCap descriptors of the devices that are present.
    /// </summary>
    private readonly Func<IEnumerable<CaptureDeviceDescriptor>> _enumerate;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FlashCapCameraSource"/> class that enumerates the
    ///     devices of the host with FlashCap.
    /// </summary>
    public FlashCapCameraSource()
        : this(() => new CaptureDevices().EnumerateDescriptors())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="FlashCapCameraSource"/> class that translates the
    ///     descriptors returned by the given function.
    /// </summary>
    /// <param name="enumerate">
    ///     Returns the FlashCap descriptors of the devices that are present. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="enumerate"/> is null.</exception>
    /// <remarks>
    ///     The function is a seam for tests: it lets them feed descriptors that do not belong to a real
    ///     camera through the real translation.
    /// </remarks>
    internal FlashCapCameraSource(Func<IEnumerable<CaptureDeviceDescriptor>> enumerate)
    {
        ArgumentNullException.ThrowIfNull(enumerate);
        _enumerate = enumerate;
    }

    /// <summary>
    ///     Gets the cameras that FlashCap reports as present now.
    /// </summary>
    /// <returns>
    ///     One <see cref="CameraInfo"/> per FlashCap descriptor, in the order FlashCap reports them. An empty
    ///     list when FlashCap finds no device.
    /// </returns>
    /// <remarks>
    ///     FlashCap returns an empty set, not an error, when no camera is attached, and so does this method.
    ///     Any other FlashCap failure propagates unchanged.
    /// </remarks>
    public IReadOnlyList<CameraInfo> GetCameras() =>
        _enumerate().Select(ToCameraInfo).ToList().AsReadOnly();

    /// <summary>
    ///     Translates one FlashCap descriptor into a camera description.
    /// </summary>
    /// <param name="descriptor">The FlashCap descriptor of one device.</param>
    /// <returns>The camera description.</returns>
    /// <remarks>
    ///     The identifier joins the backend and the backend's own identity, because an identity is only
    ///     unique within its backend (a Video for Windows identity is a small number, a DirectShow identity
    ///     a device path). Characteristics that FlashCap marks as unknown are not formats an application can
    ///     use, so they are left out, while the device itself is still reported so that the user can see it.
    /// </remarks>
    private static CameraInfo ToCameraInfo(CaptureDeviceDescriptor descriptor)
    {
        var backend = descriptor.DeviceType.ToString();

        // The identity is an opaque object whose text form is its stable value; a missing identity gives
        // an identifier with an empty part after the backend rather than a failure
        var id = $"{backend}:{descriptor.Identity}";

        var formats = descriptor.Characteristics
            .Where(characteristics => characteristics.PixelFormat != PixelFormats.Unknown)
            .Select(ToCameraFormat)
            .ToList()
            .AsReadOnly();

        return new CameraInfo(id, descriptor.Name, descriptor.Description, backend, formats);
    }

    /// <summary>
    ///     Translates one FlashCap video characteristic into a camera format.
    /// </summary>
    /// <param name="characteristics">The FlashCap video characteristic.</param>
    /// <returns>The camera format.</returns>
    /// <remarks>
    ///     FlashCap holds the frame rate as an exact fraction (for example 30000 over 1001); it is reported
    ///     as a double, and a zero fraction becomes zero.
    /// </remarks>
    private static CameraFormat ToCameraFormat(VideoCharacteristics characteristics) =>
        new(
            characteristics.Width,
            characteristics.Height,
            characteristics.PixelFormat.ToString(),
            (double)characteristics.FramesPerSecond);
}
