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

namespace DemaConsulting.AgentVision.Core.Tests.TestSupport;

/// <summary>
///     A unique temporary folder for a test, removed when the test disposes it.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TempDirectory"/> class and creates the folder.
    /// </summary>
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "agentvision-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    /// <summary>
    ///     Gets the full path of the folder.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Combines the folder with a file name.
    /// </summary>
    /// <param name="name">The file name.</param>
    /// <returns>The full path of the file in the folder.</returns>
    public string File(string name) => System.IO.Path.Combine(Path, name);

    /// <summary>
    ///     Deletes the folder and everything in it.
    /// </summary>
    public void Dispose() => Directory.Delete(Path, recursive: true);
}
