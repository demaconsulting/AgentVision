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
///     One video format that a camera offers: a frame size, a pixel format, and a frame rate.
/// </summary>
/// <remarks>
///     The format is a plain value so that applications can list, compare, and choose formats without
///     referencing the capture library that discovered them. Two formats are equal when all four values
///     are equal, which lets the enumerator drop duplicates the capture library may report.
/// </remarks>
/// <param name="Width">The frame width in pixels. Positive for every format a camera offers.</param>
/// <param name="Height">The frame height in pixels. Positive for every format a camera offers.</param>
/// <param name="PixelFormat">
///     The name of the pixel format, such as <c>JPEG</c>, <c>YUYV</c>, or <c>NV12</c>. Never empty.
/// </param>
/// <param name="FramesPerSecond">
///     The frame rate in frames per second. Zero when the camera does not report a rate.
/// </param>
public sealed record CameraFormat(int Width, int Height, string PixelFormat, double FramesPerSecond);
