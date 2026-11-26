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

namespace DemaConsulting.AgentVision.Core.Imaging;

/// <summary>
///     The image file formats that a <see cref="CameraImage"/> can be saved as.
/// </summary>
/// <remarks>
///     The formats are the ones that need no further dependency and that every image viewer and every
///     AI model reads: lossless PNG, lossy JPEG, and uncompressed BMP. The format of a file is chosen from
///     its extension (see <see cref="CameraImage.TryGetFormat"/>), so an application rarely names a
///     format itself.
/// </remarks>
public enum ImageFileFormat
{
    /// <summary>
    ///     Portable Network Graphics: lossless, the right choice when pixels must be preserved. Extension
    ///     <c>.png</c>.
    /// </summary>
    Png,

    /// <summary>
    ///     JPEG: lossy and small, the right choice for photographs that are sent over a network. Saved at
    ///     quality 90. Extensions <c>.jpg</c> and <c>.jpeg</c>.
    /// </summary>
    Jpeg,

    /// <summary>
    ///     Windows bitmap: uncompressed, 24 bits per pixel. Extension <c>.bmp</c>.
    /// </summary>
    Bmp
}
