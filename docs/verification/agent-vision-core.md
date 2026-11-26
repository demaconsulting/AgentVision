# AgentVisionCore

## Verification Approach

System-level verification combines two kinds of evidence:

- **Package inspection**: after packing the library, the build pipeline runs FileAssert
  (`.fileassert.yaml`, tag `packages`) against the real `DemaConsulting.AgentVision.Core.<version>.nupkg`
  and its `.snupkg`. The checks assert the package id, the `lib/net10.0` and `lib/net11.0` assemblies and XML
  documentation, and the publisher, license, README, and icon metadata. This is the evidence for the NuGet package
  requirement, because it inspects the artifact that is actually published.
- **Assembly inspection**: tests in `AgentVisionCoreTests.cs` load the compiled `DemaConsulting.AgentVision.Core`
  assembly by name and inspect its identity, metadata, target framework, and referenced assemblies through
  reflection.

The library has no subsystems or units yet, so there is nothing else to verify. When a capability is added, its
subsystem and unit verification documents and its end-to-end tests in `AgentVisionCoreTests.cs` extend this
approach.

## Test Environment

Tests run on .NET 10.0 and .NET 11.0 on Windows and Linux. The package inspection runs in the build job on each
operating system, after the library is packed into the `packages` folder. No camera, network access, or temporary
files are needed.

## Acceptance Criteria

- Every assembly-inspection test passes on every supported runtime and platform.
- The package inspection finds exactly one library package and one symbols package, with exactly the four expected
  files under `lib/`.
- The assembly identity and publisher metadata match the package metadata.
- The library assembly does not reference the tool assembly.

## Test Scenarios

**AgentVisionCore_Package_Packed_ContainsLibraryAssemblies**: The packed library package holds the `net10.0` and
`net11.0` assemblies with their XML documentation and nothing else under `lib/`, and the symbols package is present.
This scenario is checked by the FileAssert test of the same name.

**AgentVisionCore_Package_Packed_HasPackageMetadata**: The packed library package is named
`DemaConsulting.AgentVision.Core` and carries the publisher, MIT license expression, README, and icon. This scenario
is checked by the FileAssert test of the same name.

**AgentVisionCore_Assembly_Loaded_HasPackageName**: The library assembly is loaded by its package name and carries
that name. This scenario is tested by `AgentVisionCore_Assembly_Loaded_HasPackageName`, and the Windows and Linux
runs of it are the platform evidence.

**AgentVisionCore_Assembly_Loaded_HasPackageMetadata**: The loaded assembly reports the package product and the
publisher company. This scenario is tested by `AgentVisionCore_Assembly_Loaded_HasPackageMetadata`.

**AgentVisionCore_Assembly_Loaded_TargetsRunningRuntime**: The loaded assembly targets the major .NET version the test
is running on, so each target framework run proves the library build for that runtime. This scenario is tested by
`AgentVisionCore_Assembly_Loaded_TargetsRunningRuntime`.

**AgentVisionCore_Assembly_Loaded_DoesNotReferenceTool**: None of the assemblies the library references is the tool
assembly. This scenario is tested by `AgentVisionCore_Assembly_Loaded_DoesNotReferenceTool`.
