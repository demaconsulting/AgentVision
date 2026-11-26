package AgentVisionCore_CamerasSubsystem {
    public import AgentVisionCore_CameraEnumerator::*;
    public import AgentVisionCore_FlashCapCameraSource::*;
    public import AgentVisionCore_FlashCapCameraDevice::*;
    private import SoftwareItemMetadata::*;

    part def CamerasSubsystem {
        doc /* Enumerates the video capture devices of the host and the formats each supports, and starts a camera streaming: the enumerator owns the policy of the list, the FlashCap source discovers and opens the devices, and the FlashCap device streams one of them. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Cameras/CamerasSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-core/cameras.md");
            verificationFiles = ("docs/verification/agent-vision-core/cameras.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/cameras.yaml");
        }

        part cameraEnumerator : CameraEnumerator;
        part flashCapCameraSource : FlashCapCameraSource;
        part flashCapCameraDevice : FlashCapCameraDevice;
    }
}
