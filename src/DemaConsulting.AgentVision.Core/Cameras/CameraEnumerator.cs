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

namespace DemaConsulting.AgentVision.Core.Cameras;

/// <summary>
///     Lists the cameras of the host in a deterministic order, with the formats each one supports.
/// </summary>
/// <remarks>
///     <para>
///         The enumerator owns the policy of the camera list and leaves discovery to an
///         <see cref="ICameraSource"/>: it takes a private copy of what the source reports, orders the
///         cameras and their formats, and removes duplicate formats. Applications therefore see the same
///         list for the same cameras whichever source reported them, and can refer to a camera by its
///         position in the list or by its stable <see cref="CameraInfo.Id"/>.
///     </para>
///     <para>
///         The enumerator holds no state besides its source, so it is as thread-safe as the source.
///     </para>
/// </remarks>
public sealed class CameraEnumerator
{
    /// <summary>
    ///     The source that reports the cameras.
    /// </summary>
    private readonly ICameraSource _source;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraEnumerator"/> class that discovers the cameras
    ///     of the host with the default capture library.
    /// </summary>
    public CameraEnumerator()
        : this(new FlashCapCameraSource())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraEnumerator"/> class that lists the cameras of
    ///     the given source.
    /// </summary>
    /// <param name="source">The source that reports the cameras. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public CameraEnumerator(ICameraSource source)
    {
        // A missing source could only fail later, far from the mistake, so it is rejected here
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    /// <summary>
    ///     Lists the cameras that are available now.
    /// </summary>
    /// <returns>
    ///     The cameras ordered by backend and then by identifier (both ordinal), each with its formats ordered
    ///     from the largest frame size and then the highest frame rate down, without duplicates. The list is
    ///     empty, not an error, when no camera is available, and it is independent of the source's own list.
    ///     A camera's position in the list is its index.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the source returns <see langword="null"/> instead of a list.
    /// </exception>
    /// <remarks>
    ///     Exceptions of the source propagate unchanged. The source is asked again on every call, because
    ///     cameras can be attached and removed between calls.
    /// </remarks>
    public IReadOnlyList<CameraInfo> Enumerate()
    {
        // Ask the source for the cameras of this moment; a null list breaks the source contract
        var reported = _source.GetCameras()
                       ?? throw new InvalidOperationException("The camera source returned no list.");

        // Copy every camera so that later changes made by the source or the caller cannot alter the
        // result, and put each camera's formats in their documented order
        var cameras = reported
            .Select(camera => camera with { Formats = OrderFormats(camera.Formats) })
            .ToList();

        // Order the cameras by backend and then identifier, with the ordinal comparison that makes
        // the order the same on every machine and culture
        cameras.Sort((left, right) =>
        {
            var byBackend = string.CompareOrdinal(left.Backend, right.Backend);
            return byBackend != 0 ? byBackend : string.CompareOrdinal(left.Id, right.Id);
        });

        return cameras.AsReadOnly();
    }

    /// <summary>
    ///     Orders formats from the largest frame size and the highest frame rate down and removes duplicates.
    /// </summary>
    /// <param name="formats">The formats a camera reported.</param>
    /// <returns>A new read-only list of the distinct formats in order.</returns>
    /// <remarks>
    ///     Largest first puts the formats an application most often wants at the top. The pixel format name
    ///     is the final key so that formats of equal size and rate have one fixed order.
    /// </remarks>
    private static IReadOnlyList<CameraFormat> OrderFormats(IReadOnlyList<CameraFormat> formats) =>
        formats
            .Distinct()
            .OrderByDescending(format => (long)format.Width * format.Height)
            .ThenByDescending(format => format.FramesPerSecond)
            .ThenBy(format => format.PixelFormat, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();
}
