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

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     The options that control how a camera is opened: the format that is requested, how long it warms up,
///     and how long the library waits for frames.
/// </summary>
/// <remarks>
///     <para>
///         Every option has a default that works for a typical webcam, so <c>new CameraOpenOptions()</c> is a
///         sensible request. The size and frame rate are preferences, never requirements: the camera is opened
///         with the format that comes closest, and the format that was chosen is reported by the session.
///     </para>
///     <para>
///         The warm-up exists because most webcams deliver dark, unfocused, or badly exposed frames for the
///         first moments after they start streaming (auto exposure and white balance are still settling), and an
///         agent must not be shown those frames. Frames that arrive before the warm-up ends are discarded.
///     </para>
///     <para>
///         The values are checked when the options are used (by <see cref="CameraOpener"/> and
///         <see cref="CameraSnapper"/>), and an invalid value is reported with an
///         <see cref="ArgumentOutOfRangeException"/> that names the option.
///     </para>
/// </remarks>
public sealed record CameraOpenOptions
{
    /// <summary>
    ///     The longest duration that any time option accepts: far beyond any sensible wait, and small enough that
    ///     adding two durations can never overflow.
    /// </summary>
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(1);

    /// <summary>
    ///     Gets the requested frame width in pixels, or <see langword="null"/> for no preference.
    /// </summary>
    /// <remarks>When set, it must be positive.</remarks>
    public int? Width { get; init; }

    /// <summary>
    ///     Gets the requested frame height in pixels, or <see langword="null"/> for no preference.
    /// </summary>
    /// <remarks>When set, it must be positive.</remarks>
    public int? Height { get; init; }

    /// <summary>
    ///     Gets the requested frame rate in frames per second, or <see langword="null"/> for no preference.
    /// </summary>
    /// <remarks>When set, it must be a positive, finite number.</remarks>
    public double? FramesPerSecond { get; init; }

    /// <summary>
    ///     Gets how long frames are discarded after the camera starts streaming. The default is 1000 ms.
    /// </summary>
    /// <remarks>Zero means no warm-up. It must not be negative or longer than <see cref="MaxDuration"/>.</remarks>
    public TimeSpan WarmUp { get; init; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    ///     Gets how long the first snapshot waits for a frame after the warm-up ends. The default is 5 seconds.
    /// </summary>
    /// <remarks>It must be positive and not longer than <see cref="MaxDuration"/>.</remarks>
    public TimeSpan FirstFrameTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Gets how old the latest frame may be before the camera counts as disconnected. The default is 5 seconds.
    /// </summary>
    /// <remarks>
    ///     A streaming camera delivers many frames per second, so a frame that is much older means that the
    ///     camera stopped delivering. It must be positive and not longer than <see cref="MaxDuration"/>.
    /// </remarks>
    public TimeSpan StaleFrameTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Checks every option and reports the first invalid one.
    /// </summary>
    /// <param name="parameterName">
    ///     The name of the caller's parameter that carried the options; it becomes the parameter name of the
    ///     exception, and the message names the invalid option.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when a size or frame rate is not positive, a frame rate is not finite, the warm-up is negative,
    ///     a timeout is not positive, or a duration exceeds <see cref="MaxDuration"/>.
    /// </exception>
    internal void Validate(string parameterName)
    {
        // Sizes and rates are preferences, but a non-positive one can never be satisfied
        if (Width is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, Width, "The requested Width must be positive.");
        }

        if (Height is <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, Height, "The requested Height must be positive.");
        }

        if (FramesPerSecond is <= 0 || (FramesPerSecond is { } rate && !double.IsFinite(rate)))
        {
            throw new ArgumentOutOfRangeException(
                parameterName, FramesPerSecond, "The requested FramesPerSecond must be a positive number.");
        }

        // Durations: the warm-up may be zero, waits must be positive, and all are bounded
        if (WarmUp < TimeSpan.Zero || WarmUp > MaxDuration)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, WarmUp, "The WarmUp must be between zero and one hour.");
        }

        if (FirstFrameTimeout <= TimeSpan.Zero || FirstFrameTimeout > MaxDuration)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, FirstFrameTimeout, "The FirstFrameTimeout must be positive and at most one hour.");
        }

        if (StaleFrameTimeout <= TimeSpan.Zero || StaleFrameTimeout > MaxDuration)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, StaleFrameTimeout, "The StaleFrameTimeout must be positive and at most one hour.");
        }
    }
}
