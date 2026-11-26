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
///     A video capture device found on the host, with the formats it supports.
/// </summary>
/// <remarks>
///     <para>
///         The description is a plain value so that applications can show and choose cameras without
///         referencing the capture library that discovered them. A computer can offer one physical camera
///         through several capture backends; each backend reports its own entry, and <see cref="Backend"/>
///         tells the entries apart.
///     </para>
///     <para>
///         Equality compares <see cref="Formats"/> by reference, so compare the lists element by element
///         when two descriptions must be compared by value.
///     </para>
/// </remarks>
/// <param name="Id">
///     The stable identifier: the backend name, a colon, and the identity the backend gives the device,
///     for example <c>V4L2:/dev/video0</c>. Never empty.
/// </param>
/// <param name="Name">The device name that the backend reports, such as the friendly name of the camera.</param>
/// <param name="Description">The device description that the backend reports; may equal <paramref name="Name"/>.</param>
/// <param name="Backend">
///     The capture backend that reports the device: <c>DirectShow</c>, <c>MediaFoundation</c>,
///     <c>VideoForWindows</c>, or <c>V4L2</c>.
/// </param>
/// <param name="Formats">
///     The video formats the device supports and that can be captured. Empty when the device offers none that
///     can be captured. Never <see langword="null"/>.
/// </param>
public sealed record CameraInfo(
    string Id,
    string Name,
    string Description,
    string Backend,
    IReadOnlyList<CameraFormat> Formats);
