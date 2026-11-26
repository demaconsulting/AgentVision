package AgentVisionCore_CameraEnumerator {
    private import SoftwareItemMetadata::*;

    part def CameraEnumerator {
        doc /* Lists the cameras of the host in a deterministic order: copies what a camera source reports, orders the cameras and their formats, removes duplicate formats, and treats an empty answer as valid. Also owns the public camera types (CameraInfo, CameraFormat, and the ICameraSource seam). */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.AgentVision.Core/Cameras/CameraEnumerator.cs",
                "src/DemaConsulting.AgentVision.Core/Cameras/CameraInfo.cs",
                "src/DemaConsulting.AgentVision.Core/Cameras/CameraFormat.cs",
                "src/DemaConsulting.AgentVision.Core/Cameras/ICameraSource.cs"
            );
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Cameras/CameraEnumeratorTests.cs");
            designFiles = ("docs/design/agent-vision-core/cameras/camera-enumerator.md");
            verificationFiles = ("docs/verification/agent-vision-core/cameras/camera-enumerator.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/cameras/camera-enumerator.yaml");
        }
    }

    // The default constructor creates the FlashCap-backed source; the item is named with its qualified
    // package so that no import (and no import cycle) is created between the two units.
    dependency CameraEnumeratorUsesFlashCapCameraSource from CameraEnumerator to AgentVisionCore_FlashCapCameraSource::FlashCapCameraSource;
}
