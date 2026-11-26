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

using System.Globalization;
using DemaConsulting.AgentVision.Core.Cameras;

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     Decides which camera and which format a request for "this camera, about this size" means.
/// </summary>
/// <remarks>
///     <para>
///         The unit holds the policy that is easy to get wrong and tedious to test through hardware: how a user's
///         words map to one camera (an index as <c>agentvision cameras</c> prints it, an identifier, or a name),
///         and which of the formats a camera offers is the best match for a request. Both are pure functions of
///         their arguments, so every rule can be tested exactly.
///     </para>
///     <para>
///         The type is stateless and thread-safe.
///     </para>
/// </remarks>
internal static class CameraSelector
{
    /// <summary>
    ///     The pixel formats that the library can turn into pictures, in order of preference.
    /// </summary>
    /// <remarks>
    ///     JPEG first because it needs the least USB bandwidth and its frames are used as they are; the raw
    ///     formats follow in the order of how cheap their conversion is; the formats that FlashCap cannot
    ///     convert to a 24-bit or 32-bit picture (RGB8, RGB15, RGB16) are left out on purpose.
    /// </remarks>
    private static readonly string[] PixelFormatPreference =
        ["JPEG", "YUYV", "NV12", "UYVY", "RGB24", "RGB32", "ARGB32", "PNG"];

    /// <summary>
    ///     The widest default frame, in pixels: full HD is plenty for an agent and keeps the transfer light.
    /// </summary>
    private const int DefaultMaxWidth = 1920;

    /// <summary>
    ///     The tallest default frame, in pixels.
    /// </summary>
    private const int DefaultMaxHeight = 1080;

    /// <summary>
    ///     The lowest frame rate that the default choice accepts, so that the picture is not stale and the
    ///     camera's exposure settles quickly.
    /// </summary>
    private const double DefaultMinFramesPerSecond = 15;

    /// <summary>
    ///     Finds the camera that a selector names.
    /// </summary>
    /// <param name="cameras">
    ///     The available cameras in enumeration order (see <see cref="CameraEnumerator.Enumerate"/>), so that an
    ///     index here is the index that the listing shows.
    /// </param>
    /// <param name="selector">
    ///     A non-negative integer (the index in <paramref name="cameras"/>), the exact identifier of a camera, or
    ///     the name of a camera (letter case does not matter).
    /// </param>
    /// <returns>The selected camera.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="selector"/> is empty or white space, or when it is a name that
    ///     several cameras of the same backend share (the message says to use the index or the identifier).</exception>
    /// <exception cref="CameraNotFoundException">Thrown when no camera matches; the message lists the available cameras.</exception>
    /// <remarks>
    ///     The rules apply in this order. A selector that is a number is an index, and an index that is out of
    ///     range is an error: it is not tried as a name, because the user meant an index. Otherwise an exact,
    ///     case-sensitive identifier match wins. Otherwise the name is compared exactly but ignoring letter case.
    ///     Windows can report one camera through several backends under the same name, so a name that matches
    ///     cameras of different backends picks the first backend in the order of <paramref name="cameras"/>
    ///     (DirectShow, then Media Foundation, then V4L2, then Video for Windows); only several matches inside
    ///     that backend are ambiguous.
    /// </remarks>
    public static CameraInfo SelectCamera(IReadOnlyList<CameraInfo> cameras, string selector)
    {
        ArgumentNullException.ThrowIfNull(cameras);
        ArgumentNullException.ThrowIfNull(selector);
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentException("The camera selector must not be empty.", nameof(selector));
        }

        var text = selector.Trim();

        // A number is an index into the listing; it is never retried as a name
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            return index < cameras.Count ? cameras[index] : throw NotFound(cameras, text);
        }

        // An exact identifier is unambiguous
        var byId = cameras.FirstOrDefault(camera => string.Equals(camera.Id, text, StringComparison.Ordinal));
        if (byId != null)
        {
            return byId;
        }

        // A name: the first backend with a match wins, and several matches inside it are ambiguous
        var byName = cameras.Where(camera => string.Equals(camera.Name, text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byName.Count == 0)
        {
            throw NotFound(cameras, text);
        }

        var sameBackend = byName.Where(camera => camera.Backend == byName[0].Backend).ToList();
        if (sameBackend.Count > 1)
        {
            var choices = string.Join(
                ", ",
                cameras
                    .Select((camera, index) => (Camera: camera, Index: index))
                    .Where(entry => sameBackend.Contains(entry.Camera))
                    .Select(entry => $"{entry.Index} ({entry.Camera.Id})"));
            throw new ArgumentException(
                $"The camera name '{text}' is shared by several cameras: {choices}. " +
                "Use the index or the identifier to say which one.",
                nameof(selector));
        }

        return byName[0];
    }

    /// <summary>
    ///     Chooses the format to open a camera with.
    /// </summary>
    /// <param name="camera">The camera, with the formats it offers.</param>
    /// <param name="options">The requested size and frame rate; only those three options are used.</param>
    /// <returns>One of the formats of <paramref name="camera"/>; never fails because a requested size is absent.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="CameraOpenException">Thrown when the camera offers no format that can be captured.</exception>
    /// <remarks>
    ///     <para>
    ///         Only formats the library can turn into pictures are candidates. Without a request, the choice is
    ///         the largest size that is not above 1920x1080 and offers at least 15 frames per second, then the
    ///         highest frame rate, then the preferred pixel format; when no format meets both limits, the nearest
    ///         one to them is taken, so a camera that offers only 4K still opens (at its smallest size that is
    ///         fast enough).
    ///     </para>
    ///     <para>
    ///         With a request, the formats are ranked by how far their size is from the requested width and height
    ///         (an unspecified dimension counts as a match), then by the frame rate (not below the requested rate
    ///         first, then the closest), then by the preferred pixel format. A request for a size the camera does not
    ///         offer therefore opens the nearest size; the caller can read the chosen format from the session.
    ///     </para>
    /// </remarks>
    public static CameraFormat SelectFormat(CameraInfo camera, CameraOpenOptions options)
    {
        ArgumentNullException.ThrowIfNull(camera);
        ArgumentNullException.ThrowIfNull(options);

        // Only formats that become pictures are candidates; the preference order is also their usable list
        var candidates = camera.Formats
            .Where(format => PreferenceOf(format) >= 0)
            .ToList();
        if (candidates.Count == 0)
        {
            var offered = camera.Formats.Count == 0
                ? "no format"
                : "only formats that cannot be converted to a picture (" +
                  string.Join(", ", camera.Formats.Select(format => format.PixelFormat).Distinct()) + ")";
            throw new CameraOpenException(
                $"Camera '{camera.Name}' offers {offered}. The supported pixel formats are " +
                $"{string.Join(", ", PixelFormatPreference)}.");
        }

        // Rank every candidate and take the first of the best: ties keep the enumeration order
        var sizeRequested = options.Width != null || options.Height != null;
        return candidates
            .Select(format => (Format: format, Key: RankKey(format, options, sizeRequested)))
            .MinBy(entry => entry.Key, KeyComparer.Instance)
            .Format;
    }

    /// <summary>
    ///     Gets the position of a format's pixel format in the preference order.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The zero-based preference, or -1 when the pixel format cannot be converted to a picture.</returns>
    private static int PreferenceOf(CameraFormat format) =>
        Array.FindIndex(PixelFormatPreference, name => string.Equals(name, format.PixelFormat, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Builds the sort key of a format: smaller keys are better, compared one value after the other.
    /// </summary>
    /// <param name="format">The candidate format.</param>
    /// <param name="options">The request.</param>
    /// <param name="sizeRequested">Whether a width or height was requested.</param>
    /// <returns>The key values, most significant first.</returns>
    private static double[] RankKey(CameraFormat format, CameraOpenOptions options, bool sizeRequested)
    {
        var area = (double)format.Width * format.Height;
        var preference = PreferenceOf(format);

        // The frame-rate keys: a requested rate prefers "not below" and then the closest; otherwise the
        // highest rate is best
        double rateNotBelow;
        double rateDistance;
        if (options.FramesPerSecond is { } requestedRate)
        {
            rateNotBelow = format.FramesPerSecond >= requestedRate ? 0 : 1;
            rateDistance = Math.Abs(format.FramesPerSecond - requestedRate);
        }
        else
        {
            rateNotBelow = 0;
            rateDistance = 0;
        }

        if (sizeRequested)
        {
            // A requested size comes first: nearest width and height, an unspecified dimension counts as a match
            var distance = (options.Width is { } width ? Math.Abs(format.Width - (double)width) : 0)
                           + (options.Height is { } height ? Math.Abs(format.Height - (double)height) : 0);
            return [distance, rateNotBelow, rateDistance, -format.FramesPerSecond, preference];
        }

        // No size requested: the default policy. A rate that is too low for the default is a worse choice than
        // a larger size, so the rate comes first; then a size within the limits, the largest of those (or the
        // smallest when everything is larger), then the highest rate
        var gate = RateGate(format, options);
        var withinLimit = format.Width <= DefaultMaxWidth && format.Height <= DefaultMaxHeight;
        return
        [
            gate,
            rateDistance,
            withinLimit ? 0 : 1,
            withinLimit ? -area : area,
            -format.FramesPerSecond,
            preference
        ];
    }

    /// <summary>
    ///     Gets the rate-gate key of a format for a request without a size.
    /// </summary>
    /// <param name="format">The candidate format.</param>
    /// <param name="options">The request.</param>
    /// <returns>
    ///     Zero when the format is acceptable and one when it is not: a requested rate is acceptable when the
    ///     format does not fall below it; without a request, a format is acceptable when its rate is at least the
    ///     default minimum, or when the camera does not report a rate (zero).
    /// </returns>
    private static double RateGate(CameraFormat format, CameraOpenOptions options)
    {
        if (options.FramesPerSecond is { } requested)
        {
            return format.FramesPerSecond >= requested ? 0 : 1;
        }

        var unknown = format.FramesPerSecond == 0;
        return unknown || format.FramesPerSecond >= DefaultMinFramesPerSecond ? 0 : 1;
    }

    /// <summary>
    ///     Creates the exception for a selector that matches no camera.
    /// </summary>
    /// <param name="cameras">The available cameras.</param>
    /// <param name="selector">The selector that matched none.</param>
    /// <returns>The exception to throw.</returns>
    private static CameraNotFoundException NotFound(IReadOnlyList<CameraInfo> cameras, string selector)
    {
        var available = cameras
            .Select((camera, index) => string.Create(CultureInfo.InvariantCulture, $"{index}: {camera.Name} ({camera.Backend})"))
            .ToList();
        return new CameraNotFoundException(selector, available);
    }

    /// <summary>
    ///     Compares two sort keys value by value, smaller first.
    /// </summary>
    private sealed class KeyComparer : IComparer<double[]>
    {
        /// <summary>
        ///     Gets the shared instance.
        /// </summary>
        public static readonly KeyComparer Instance = new();

        /// <summary>
        ///     Compares two keys of the same length.
        /// </summary>
        /// <param name="x">The first key.</param>
        /// <param name="y">The second key.</param>
        /// <returns>A negative number when <paramref name="x"/> is better, positive when worse, zero when equal.</returns>
        public int Compare(double[]? x, double[]? y)
        {
            for (var i = 0; i < x!.Length; i++)
            {
                var result = x[i].CompareTo(y![i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}
