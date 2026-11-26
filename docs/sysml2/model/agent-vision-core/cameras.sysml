package AgentVisionCore_CamerasSubsystem {
    public import AgentVisionCore_CameraEnumerator::*;
    public import AgentVisionCore_FlashCapCameraSource::*;
    private import SoftwareItemMetadata::*;

    part def CamerasSubsystem {
        doc /* Enumerates the video capture devices of the host and the formats each supports: the enumerator owns the policy of the list, and the FlashCap source discovers the devices. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Cameras/CamerasSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-core/cameras.md");
            verificationFiles = ("docs/verification/agent-vision-core/cameras.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/cameras.yaml");
        }

        part cameraEnumerator : CameraEnumerator;
        part flashCapCameraSource : FlashCapCameraSource;
    }
}
