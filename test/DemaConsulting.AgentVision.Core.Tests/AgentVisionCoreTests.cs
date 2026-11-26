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
}
