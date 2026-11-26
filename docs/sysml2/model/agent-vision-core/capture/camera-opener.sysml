package AgentVisionCore_CameraOpener {
    private import SoftwareItemMetadata::*;

    part def CameraOpener {
        doc /* Opens cameras: checks the options, selects the camera and the format, starts the device through the camera source, and returns a streaming session; translates failures of the source into the library's exceptions and leaves no device open when it fails. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Core/Capture/CameraOpener.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Capture/CameraOpenerTests.cs");
            designFiles = ("docs/design/agent-vision-core/capture/camera-opener.md");
            verificationFiles = ("docs/verification/agent-vision-core/capture/camera-opener.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/capture/camera-opener.yaml");
        }
    }

    dependency CameraOpenerUsesCameraSelector from CameraOpener to AgentVisionCore_CameraSelector::CameraSelector;
    dependency CameraOpenerCreatesCameraSession from CameraOpener to AgentVisionCore_CameraSession::CameraSession;
    dependency CameraOpenerUsesCameraEnumerator from CameraOpener to AgentVisionCore_CameraEnumerator::CameraEnumerator;
    dependency CameraOpenerUsesFlashCapCameraSource from CameraOpener to AgentVisionCore_FlashCapCameraSource::FlashCapCameraSource;
}