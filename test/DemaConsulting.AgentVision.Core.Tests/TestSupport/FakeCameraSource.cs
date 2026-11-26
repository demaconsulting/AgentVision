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

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A hand-written camera source for tests: it reports a configured list, counts its calls, and can be
///     told to fail, so the enumeration policy can be tested without camera hardware.
/// </summary>
/// <param name="cameras">The cameras the source reports.</param>
internal sealed class FakeCameraSource(IReadOnlyList<CameraInfo> cameras) : ICameraSource
{
    /// <summary>
    ///     Gets or sets the cameras that the source reports; tests change it to simulate a camera that is
    ///     attached or removed, or to simulate a source that breaks its contract.
    /// </summary>
    public IReadOnlyList<CameraInfo> Cameras { get; set; } = cameras;

    /// <summary>
    ///     Gets the exception that the source throws instead of reporting the cameras, or <see langword="null"/>.
    /// </summary>
    public Exception? Failure { get; init; }

    /// <summary>
    ///     Gets the number of times the source was asked for its cameras.
    /// </summary>
    public int CallCount { get; private set; }

    /// <summary>
    ///     Reports the configured cameras or throws the configured failure.
    /// </summary>
    /// <returns>The configured list.</returns>
    public IReadOnlyList<CameraInfo> GetCameras()
    {
        CallCount++;
        if (Failure != null)
        {
            throw Failure;
        }

        return Cameras;
    }
}
