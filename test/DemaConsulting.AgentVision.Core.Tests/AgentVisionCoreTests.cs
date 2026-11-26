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

using System.Reflection;
using System.Runtime.Versioning;
using DemaConsulting.AgentVision.Core.Cameras;
using DemaConsulting.AgentVision.Core.Tests.TestSupport;

namespace DemaConsulting.AgentVision.Core.Tests;

/// <summary>
///     System-level tests for the AgentVisionCore library.
/// </summary>
/// <remarks>
///     The package-level tests load the compiled assembly by name and verify the properties that
///     every release of the library must retain: its identity, its metadata, its target framework,
///     and its independence from the tool.
/// </remarks>
public class AgentVisionCoreTests
{
    /// <summary>
    ///     The simple name of the AgentVisionCore library assembly, which is also its NuGet package ID.
    /// </summary>
    private const string LibraryAssemblyName = "DemaConsulting.AgentVision.Core";

    /// <summary>
    ///     Loads the AgentVisionCore library assembly by name.
    /// </summary>
    /// <returns>The loaded AgentVisionCore library assembly.</returns>
    private static Assembly LoadLibrary() => Assembly.Load(new AssemblyName(LibraryAssemblyName));

    /// <summary>
    ///     Test that the library assembly loads and carries the NuGet package name.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_HasPackageName()
    {
        // Arrange: no setup required; the library is referenced by this test project

        // Act: load the library assembly by its package name
        var assembly = LoadLibrary();

        // Assert: the loaded assembly carries the package name
        Assert.Equal(LibraryAssemblyName, assembly.GetName().Name);
    }

    /// <summary>
    ///     Test that the library assembly carries the package product and company metadata.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_HasPackageMetadata()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: read the product and company metadata attributes
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

        // Assert: the metadata identifies the package and its publisher
        Assert.Multiple(
            () => Assert.Equal(LibraryAssemblyName, product),
            () => Assert.Equal("DEMA Consulting", company));
    }

    /// <summary>
    ///     Test that the library assembly loaded by the test run targets the runtime the test runs on,
    ///     so each target framework run proves the library build for that runtime.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_TargetsRunningRuntime()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: read the target framework the assembly was compiled for
        var framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        // Assert: the assembly targets the major .NET version the test is running on
        Assert.Equal($".NETCoreApp,Version=v{Environment.Version.Major}.0", framework);
    }

    /// <summary>
    ///     Test that the library assembly does not reference the AgentVision tool.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_DoesNotReferenceTool()
    {
        // Arrange: load the library assembly
        var assembly = LoadLibrary();

        // Act: list the names of every assembly the library references
        var references = assembly.GetReferencedAssemblies().Select(reference => reference.Name);

        // Assert: the library never depends on the tool, keeping the tool a thin front-end
        Assert.DoesNotContain("DemaConsulting.AgentVision.Tool", references);
    }

    /// <summary>
    ///     Test that an application can enumerate the cameras of its own source through the public API.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_CustomSource_Enumerated_ReportsCameras()
    {
        // Arrange: a custom source with one camera, and an enumerator that uses it
        var source = new FakeCameraSource(
        [
            new CameraInfo(
                "Custom:1",
                "Custom Camera",
                "Custom Camera (custom)",
                "Custom",
                [new CameraFormat(1280, 720, "NV12", 30), new CameraFormat(640, 480, "YUYV", 15)])
        ]);
        var enumerator = new CameraEnumerator(source);

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the identifier, name, and every format with its size, pixel format, and frame rate are reported
        var camera = Assert.Single(cameras);
        Assert.Equal("Custom:1", camera.Id);
        Assert.Equal("Custom Camera", camera.Name);
        Assert.Equal(
            [new CameraFormat(1280, 720, "NV12", 30), new CameraFormat(640, 480, "YUYV", 15)],
            camera.Formats);
    }

    /// <summary>
    ///     Test that a source without cameras gives an empty list and not an error.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_NoneAttached_ReturnsEmptyList()
    {
        // Arrange: an enumerator whose source has no camera
        var enumerator = new CameraEnumerator(new FakeCameraSource([]));

        // Act: enumerate the cameras
        var cameras = enumerator.Enumerate();

        // Assert: the list is empty, which is a valid answer and not an error
        Assert.NotNull(cameras);
        Assert.Empty(cameras);
    }

    /// <summary>
    ///     Test that the default enumerator, which asks the real FlashCap of this machine, completes with
    ///     or without a camera.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Cameras_DefaultSource_Enumerated_DoesNotThrow()
    {
        // Arrange: the default enumerator; the result depends on the machine's cameras
        var enumerator = new CameraEnumerator();

        // Act: enumerate the cameras
        var exception = Record.Exception(() => enumerator.Enumerate());

        // Assert: enumeration completed; a machine without a camera gives an empty list, not a failure
        Assert.Null(exception);
    }

    /// <summary>
    ///     Test that no FlashCap type appears in the public API of the library, so a FlashCap upgrade
    ///     cannot break consumers.
    /// </summary>
    [Fact]
    public void AgentVisionCore_Assembly_Loaded_PublicApiExposesNoFlashCapTypes()
    {
        // Arrange: load the library and collect every type used in its public signatures
        var assembly = LoadLibrary();
        var used = new List<Type>();
        foreach (var type in assembly.GetExportedTypes())
        {
            // An interface has no base type
            if (type.BaseType != null)
            {
                used.Add(type.BaseType);
            }

            used.AddRange(type.GetInterfaces());
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                // Non-public members are not part of the API, so only visible signatures count
                switch (member)
                {
                    case PropertyInfo property when property.GetMethod?.IsPublic == true:
                        used.Add(property.PropertyType);
                        break;
                    case MethodBase { IsPublic: true } method:
                        used.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
                        if (method is MethodInfo info)
                        {
                            used.Add(info.ReturnType);
                        }

                        break;
                    case FieldInfo { IsPublic: true } field:
                        used.Add(field.FieldType);
                        break;
                }
            }
        }

        // Act: find any used type, including the type arguments of generic types, that belongs to FlashCap
        var leaked = used
            .SelectMany(ExpandType)
            .Where(type => type.Namespace?.StartsWith("FlashCap", StringComparison.Ordinal) == true)
            .Select(type => type.FullName)
            .ToList();

        // Assert: none are exposed
        Assert.Empty(leaked);
    }

    /// <summary>
    ///     Expands a type into itself and every type it is built from: element types of arrays and
    ///     references, and the type arguments of generic types.
    /// </summary>
    /// <param name="type">The type to expand.</param>
    /// <returns>The type and the types it is built from.</returns>
    private static IEnumerable<Type> ExpandType(Type type)
    {
        yield return type;

        // Arrays, references, and pointers are built from their element type
        if (type.HasElementType)
        {
            foreach (var inner in ExpandType(type.GetElementType()!))
            {
                yield return inner;
            }
        }

        // Generic types are built from their type arguments
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in ExpandType(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
