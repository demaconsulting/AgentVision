package AgentVisionCore_FlashCapCameraSource {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;

    part def FlashCapCameraSource {
        doc /* The default camera source: asks FlashCap for the video capture devices of the host and translates each descriptor into the library's own camera types. The only unit that references FlashCap, so no FlashCap type crosses the camera source boundary. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Core/Cameras/FlashCapCameraSource.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Cameras/FlashCapCameraSourceTests.cs");
            designFiles = ("docs/design/agent-vision-core/cameras/flash-cap-camera-source.md");
            verificationFiles = ("docs/verification/agent-vision-core/cameras/flash-cap-camera-source.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/cameras/flash-cap-camera-source.yaml");
        }
    }

    // The source discovers the devices and their formats with FlashCap, the runtime OTS library.
    dependency FlashCapCameraSourceUsesFlashCap from FlashCapCameraSource to FlashCap;
}
