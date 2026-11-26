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
///     The base of every failure that capturing from a camera can report.
/// </summary>
/// <remarks>
///     Applications that want one handler for "the camera did not work" catch this type; the derived types
///     say what went wrong. The message of every exception is written for a person: it names the camera or
///     selector and says what to do next. Argument mistakes (a null value, an invalid option) are reported
///     with the standard argument exceptions, not with this type.
/// </remarks>
public abstract class CameraException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the failure and how to respond.</param>
    /// <param name="innerException">The failure that caused this one, or <see langword="null"/>.</param>
    protected CameraException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
///     The exception thrown when no camera matches the index, identifier, or name that was asked for.
/// </summary>
/// <remarks>
///     The message lists the cameras that are available, so the caller (often an AI agent) can pick a valid
///     one without another round trip.
/// </remarks>
public sealed class CameraNotFoundException : CameraException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraNotFoundException"/> class.
    /// </summary>
    /// <param name="selector">The index, identifier, or name that matched no camera.</param>
    /// <param name="available">One line per available camera: its index, name, and backend.</param>
    public CameraNotFoundException(string selector, IReadOnlyList<string> available)
        : base(BuildMessage(selector, available))
    {
        Selector = selector;
        Available = available;
    }

    /// <summary>
    ///     Gets the index, identifier, or name that matched no camera.
    /// </summary>
    public string Selector { get; }

    /// <summary>
    ///     Gets one line per available camera (index, name, and backend); empty when the machine has no camera.
    /// </summary>
    public IReadOnlyList<string> Available { get; }

    /// <summary>
    ///     Builds the message that names the selector and lists the available cameras.
    /// </summary>
    /// <param name="selector">The selector that matched no camera.</param>
    /// <param name="available">The available cameras, one line each.</param>
    /// <returns>The message.</returns>
    private static string BuildMessage(string selector, IReadOnlyList<string> available) =>
        available.Count == 0
            ? $"Camera '{selector}' was not found: no camera is available on this machine."
            : $"Camera '{selector}' was not found. Available cameras: {string.Join("; ", available)}.";
}

/// <summary>
///     The exception thrown when a camera cannot be opened or started, or offers nothing that can be captured.
/// </summary>
/// <remarks>
///     Typical causes are a camera that another application holds, a missing permission, a camera that was
///     unplugged after it was listed, or a camera whose formats the library cannot decode. The inner
///     exception, when present, is the failure of the capture library.
/// </remarks>
public sealed class CameraOpenException : CameraException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraOpenException"/> class.
    /// </summary>
    /// <param name="message">The message that names the camera and describes the failure.</param>
    /// <param name="innerException">The failure of the capture library, or <see langword="null"/>.</param>
    public CameraOpenException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
///     The exception thrown when an open camera stops delivering frames or reports a device failure.
/// </summary>
/// <remarks>
///     The camera was unplugged, was taken by another application, or crashed. The session cannot recover:
///     dispose it and open the camera again.
/// </remarks>
public sealed class CameraDisconnectedException : CameraException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraDisconnectedException"/> class.
    /// </summary>
    /// <param name="message">The message that names the camera and describes the failure.</param>
    /// <param name="innerException">The failure the device reported, or <see langword="null"/>.</param>
    public CameraDisconnectedException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
///     The exception thrown when a camera delivers no usable frame within the time allowed.
/// </summary>
/// <remarks>
///     The camera opened but did not produce a frame after the warm-up: it may be covered, busy, or too slow
///     at the requested format. A longer first-frame timeout or a smaller format can help.
/// </remarks>
public sealed class CameraTimeoutException : CameraException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CameraTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The message that names the camera and the time that was allowed.</param>
    public CameraTimeoutException(string message)
        : base(message)
    {
    }
}
