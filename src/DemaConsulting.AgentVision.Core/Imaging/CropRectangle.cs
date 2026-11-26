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

namespace DemaConsulting.AgentVision.Core.Imaging;

/// <summary>
///     A rectangular region of an image, in pixels, measured from the top-left corner.
/// </summary>
/// <remarks>
///     The rectangle is a plain value so that applications can describe a region without referencing the
///     imaging library. Its values are not validated on construction, because whether a rectangle is valid
///     depends on the image it is applied to; <see cref="CameraImage.Crop"/> and the capture operations
///     that accept a rectangle check it and report what is wrong.
/// </remarks>
/// <param name="X">The zero-based column of the left edge. Must not be negative to be valid.</param>
/// <param name="Y">The zero-based row of the top edge. Must not be negative to be valid.</param>
/// <param name="Width">The width in pixels. Must be positive to be valid.</param>
/// <param name="Height">The height in pixels. Must be positive to be valid.</param>
public readonly record struct CropRectangle(int X, int Y, int Width, int Height)
{
    /// <summary>
    ///     Formats the rectangle as <c>x,y,width,height</c>, the form that the command line accepts.
    /// </summary>
    /// <returns>The four values separated by commas, in the invariant culture.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{X},{Y},{Width},{Height}");
}
