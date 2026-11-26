package AgentVisionCore_CaptureSubsystem {
    public import AgentVisionCore_CameraSelector::*;
    public import AgentVisionCore_CameraOpener::*;
    public import AgentVisionCore_CameraSession::*;
    public import AgentVisionCore_CameraSnapper::*;
    private import SoftwareItemMetadata::*;

    part def CaptureSubsystem {
        doc /* Keeps cameras open ("hot") so that their newest frame can be read with almost no delay: the selector decides which camera and format a request means, the opener opens a camera, the session is the open camera that holds the newest frame, and the snapper takes one picture with one call. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Capture/CaptureSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-core/capture.md");
            verificationFiles = ("docs/verification/agent-vision-core/capture.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/capture.yaml");
        }

        part cameraSelector : CameraSelector;
        part cameraOpener : CameraOpener;
        part cameraSession : CameraSession;
        part cameraSnapper : CameraSnapper;
    }
}