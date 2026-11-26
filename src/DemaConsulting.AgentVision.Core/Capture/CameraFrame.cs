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
using DemaConsulting.AgentVision.Core.Imaging;

namespace DemaConsulting.AgentVision.Core.Capture;

/// <summary>
///     One frame that a <see cref="CameraSession"/> holds: the encoded picture and when it arrived.
/// </summary>
/// <remarks>
///     <para>
///         A frame is immutable, so a snapshot taken by one thread can be handed to others and kept for as long as
///         needed; the session replaces its latest frame with a new object and never changes one that it gave out.
///         Keeping the frame encoded makes taking a snapshot nearly free; the cost of decoding is paid only by
///         callers that need pixels (<see cref="ToImage"/>), and a daemon can forward the encoded bytes as they are.
///     </para>
///     <para>
///         The type is thread-safe: all of its members only read immutable state.
///     </para>
/// </remarks>
public sealed class CameraFrame
{
    /// <summary>
    ///     The monotonic timestamp (see <see cref="Stopwatch.GetTimestamp"/>) at which the frame arrived.
    /// </summary>
    private readonly long _arrivedTimestamp;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraFrame"/> class.
    /// </summary>
    /// <param name="sequence">The running number of the frame as the camera reports it.</param>
    /// <param name="data">The bytes of the encoded image file. Not copied: the frame takes ownership.</param>
    /// <param name="arrivedTimestamp">The monotonic timestamp at which the frame arrived.</param>
    /// <param name="capturedAt">The wall-clock time at which the frame arrived.</param>
    internal CameraFrame(long sequence, ReadOnlyMemory<byte> data, long arrivedTimestamp, DateTimeOffset capturedAt)
    {
        Sequence = sequence;
        EncodedData = data;
        _arrivedTimestamp = arrivedTimestamp;
        CapturedAt = capturedAt;
    }

    /// <summary>
    ///     Gets the running number of the frame, as reported by the camera.
    /// </summary>
    /// <remarks>
    ///     Two snapshots with the same sequence number are the same frame, so a caller can tell whether a new
    ///     frame has arrived between two snapshots.
    /// </remarks>
    public long Sequence { get; }

    /// <summary>
    ///     Gets the wall-clock time at which the library received the frame.
    /// </summary>
    public DateTimeOffset CapturedAt { get; }

    /// <summary>
    ///     Gets how long ago the frame arrived, measured with a monotonic clock.
    /// </summary>
    /// <remarks>The age grows with every read; it is not fixed when the frame is created.</remarks>
    public TimeSpan Age => Stopwatch.GetElapsedTime(_arrivedTimestamp);

    /// <summary>
    ///     Gets the bytes of the frame as a complete JPEG, PNG, or BMP image file.
    /// </summary>
    /// <remarks>
    ///     The memory is shared by every holder of the frame and must be treated as read-only.
    /// </remarks>
    public ReadOnlyMemory<byte> EncodedData { get; }

    /// <summary>
    ///     Decodes the frame into an image that can be cropped and saved.
    /// </summary>
    /// <returns>A new image owned by the caller, who must dispose it.</returns>
    /// <exception cref="InvalidDataException">Thrown when the frame is damaged and cannot be decoded.</exception>
    /// <remarks>
    ///     Decoding costs time that depends on the frame size (tens of milliseconds for a full-HD JPEG), so it is
    ///     done on demand and on the calling thread, never in the camera's capture thread. Each call decodes again
    ///     and returns an independent image.
    /// </remarks>
    public CameraImage ToImage() => CameraImage.Decode(EncodedData);
}
