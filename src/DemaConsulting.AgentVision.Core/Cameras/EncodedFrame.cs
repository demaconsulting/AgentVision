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
///     One frame that a camera delivered, as the bytes of a complete image file.
/// </summary>
/// <remarks>
///     <para>
///         The bytes are a JPEG, PNG, or BMP file image, so a frame can be written to disk as it is, sent over
///         a network without being decoded, or decoded later with <c>CameraImage.Decode</c>. Cameras that
///         deliver raw pixels (such as YUYV) have their frames converted to a BMP before they reach this type.
///     </para>
///     <para>
///         The array is owned by the receiver of the frame: a camera source never reuses or changes it after
///         it has delivered it.
///     </para>
/// </remarks>
/// <param name="Data">The bytes of the image file. Never empty.</param>
/// <param name="Timestamp">
///     The time the camera reports for the frame, measured from an arbitrary start. Use it to order or space
///     frames, not as a clock time.
/// </param>
/// <param name="FrameIndex">The running number of the frame that the camera reports, starting at the first frame.</param>
public sealed record EncodedFrame(byte[] Data, TimeSpan Timestamp, long FrameIndex);
