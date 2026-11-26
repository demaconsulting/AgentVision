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

The library has three subsystems, **Cameras**, **Capture**, and **Imaging**, verified in their own documents (see
*Cameras Verification*, *Capture Verification*, and *Imaging Verification*), and the system tests in
`AgentVisionCoreTests.cs` exercise the public API end to end: they enumerate cameras from a custom source, confirm that
no camera gives an empty list, enumerate with the default source (the real FlashCap) on the machine that runs the test,
open cameras of a hand-written source and take snapshots (by index, identifier, and name, with a requested and a default
format, with a warm-up, from several threads, with two cameras open, and with every documented failure), crop and save a
captured frame, take a one-shot snapshot, and inspect the public signatures. When a capability is added, its subsystem
and unit verification documents and its end-to-end tests in `AgentVisionCoreTests.cs` extend this approach.

Hardware is never needed by these tests. The opt-in hardware smoke tests and the manual checklist (see *Capture
Verification*) cover what a camera-less run cannot, and they are not requirement evidence.

## Test Environment

Tests run on .NET 10.0 and .NET 11.0 on Windows and Linux. The package inspection runs in the build job on each
operating system, after the library is packed into the `packages` folder. No camera or network access is needed: the
camera tests use a hand-written source, and the tests that use the real FlashCap pass with and without a camera on the
machine. The tests that save pictures create and delete temporary folders. The hardware smoke tests run only when
`AGENTVISION_HARDWARE_TESTS` is `1` (`build.ps1 -All -Hardware`); the hardware tests of the two frameworks run one
after another.

## Acceptance Criteria

- Every assembly-inspection test passes on every supported runtime and platform.
- The package inspection finds exactly one library package and one symbols package, with exactly the four expected
  files under `lib/`, and the package declares FlashCap as a dependency.
- The assembly identity and publisher metadata match the package metadata.
- The library assembly does not reference the tool assembly.
- A custom source is listed through the public API, no camera gives an empty list, and enumeration with the default
  source completes with or without a camera.
- No FlashCap and no CanvasNet type appears in a public signature of the library.
- A hot camera serves its newest frame to repeated snapshots, opens by index, identifier, and name, opens the requested
  or the default format, discards the warm-up frames, keeps two cameras independent, serves concurrent snapshots, and
  stops its device when it is closed.
- Each failure (unknown camera, open failure, camera that stops, camera without frames) gives its documented exception.
- A captured frame can be cropped and saved as PNG and JPEG, and one call takes a cropped picture and closes the camera.

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

**AgentVisionCore_Cameras_CustomSource_Enumerated_ReportsCameras**: An application supplies its own camera source
with one camera of two formats; the enumerator reports the camera with its identifier, name, and both formats in
order, using only the public API and no camera hardware. This scenario is tested by
`AgentVisionCore_Cameras_CustomSource_Enumerated_ReportsCameras`.

**AgentVisionCore_Cameras_NoneAttached_ReturnsEmptyList**: The supplied source reports no camera; the enumerator
returns an empty list and does not throw. This scenario is tested by
`AgentVisionCore_Cameras_NoneAttached_ReturnsEmptyList`.

**AgentVisionCore_Cameras_DefaultSource_Enumerated_DoesNotThrow**: The default enumerator, which asks the real
FlashCap of the machine, completes without an exception whether or not the machine has a camera, and the Windows and
Linux runs of it are the platform evidence. This scenario is tested by
`AgentVisionCore_Cameras_DefaultSource_Enumerated_DoesNotThrow`.

**AgentVisionCore_Assembly_Loaded_PublicApiExposesNoFlashCapTypes**: Every type in a public signature of the library,
including the type arguments of generic types, is checked, and none belongs to FlashCap. This is a guard against a
change that would let a FlashCap upgrade break consumers; it is not linked to a requirement. This scenario is tested
by `AgentVisionCore_Assembly_Loaded_PublicApiExposesNoFlashCapTypes`.

**AgentVisionCore_Assembly_Loaded_PublicApiExposesNoCanvasNetTypes**: Every type in a public signature of the library is
checked, and none belongs to CanvasNet; a guard like the one for FlashCap, not linked to a requirement. This scenario is
tested by `AgentVisionCore_Assembly_Loaded_PublicApiExposesNoCanvasNetTypes`.

**AgentVisionCore_Capture_FakeCamera_Snapshot_ReturnsLatestFrameImmediately**: An open camera delivers a dark and then a
bright frame; two snapshots are the newest frame, and it decodes to the delivered size. This scenario is tested by
`AgentVisionCore_Capture_FakeCamera_Snapshot_ReturnsLatestFrameImmediately`.

**AgentVisionCore_Capture_FakeCamera_OpenByIndexIdAndName_OpensSameCamera**: A camera is opened by index, identifier,
and a name in other letter case, and all three sessions belong to it. This scenario is tested by
`AgentVisionCore_Capture_FakeCamera_OpenByIndexIdAndName_OpensSameCamera`.

**AgentVisionCore_Capture_FakeCamera_RequestedFormat_IsOpened** and
**AgentVisionCore_Capture_FakeCamera_NoRequest_OpensDefaultFormat**: A request for 1280x720 at 60 frames per second
opens that format; without a request the largest size not above full HD is opened. Each scenario is tested by the test
of the same name.

**AgentVisionCore_Capture_FakeCamera_WarmUp_DarkFramesAreNotServed**: A frame delivered during the warm-up is not
served; the frame delivered after it is. This scenario is tested by
`AgentVisionCore_Capture_FakeCamera_WarmUp_DarkFramesAreNotServed`.

**AgentVisionCore_Capture_TwoFakeCameras_CloseOne_OtherKeepsStreaming**: Two cameras are open; closing the first leaves
the second serving its newest frame. This scenario is tested by
`AgentVisionCore_Capture_TwoFakeCameras_CloseOne_OtherKeepsStreaming`.

**AgentVisionCore_Capture_FakeCamera_ConcurrentSnapshots_AllReturnFrames**: Eight threads take 200 snapshots each of a
streaming camera; all return frames. This scenario is tested by
`AgentVisionCore_Capture_FakeCamera_ConcurrentSnapshots_AllReturnFrames`.

**AgentVisionCore_Capture_FakeCamera_Dispose_StopsDeviceAndRejectsSnapshot**: Closing a session stops its device and
later snapshots fail. This scenario is tested by
`AgentVisionCore_Capture_FakeCamera_Dispose_StopsDeviceAndRejectsSnapshot`.

**AgentVisionCore_Capture_UnknownCamera_ThrowsCameraNotFound**, **AgentVisionCore_Capture_OpenFails_ThrowsCameraOpen**,
**AgentVisionCore_Capture_DeviceFaults_ThrowsCameraDisconnected**, and
**AgentVisionCore_Capture_NoFrame_ThrowsCameraTimeout**: An unknown camera, a camera that cannot be opened, a camera
that fails while open, and a camera that delivers no frame each give their documented exception. Each scenario is tested
by the test of the same name.

**AgentVisionCore_Imaging_CapturedFrame_CropAndSave_WritesPngAndJpeg**: A captured frame is decoded, cropped, and saved
as a PNG and a JPEG file. This scenario is tested by
`AgentVisionCore_Imaging_CapturedFrame_CropAndSave_WritesPngAndJpeg`.

**AgentVisionCore_Snap_FakeCamera_SavesCroppedPng**: One call saves a cropped PNG from a streaming camera and closes the
camera; the Windows and Linux runs of it are the platform evidence that CanvasNet works on both. This scenario is tested
by `AgentVisionCore_Snap_FakeCamera_SavesCroppedPng`.
