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

using DemaConsulting.AgentVision.Core.Cameras;

namespace DemaConsulting.AgentVision.Tool.Tests.TestSupport;

/// <summary>
///     A hand-written camera source for tests of the cameras command: it reports a fixed list, so the
///     command can be tested without camera hardware.
/// </summary>
/// <param name="cameras">The cameras the source reports.</param>
internal sealed class FakeCameraSource(IReadOnlyList<CameraInfo> cameras) : ICameraSource
{
    /// <summary>
    ///     Gets the cameras that the source reports.
    /// </summary>
    /// <returns>The fixed list given to the constructor.</returns>
    public IReadOnlyList<CameraInfo> GetCameras() => cameras;
}
