package AgentVisionCore_CameraSnapper {
    private import SoftwareItemMetadata::*;

    part def CameraSnapper {
        doc /* Takes one picture with one call: checks the request, opens a camera, waits for the warm-up, takes one frame, optionally crops it, saves it, and always closes the camera. Also owns the request and result values. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Core/Capture/CameraSnapper.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Capture/CameraSnapperTests.cs");
            designFiles = ("docs/design/agent-vision-core/capture/camera-snapper.md");
            verificationFiles = ("docs/verification/agent-vision-core/capture/camera-snapper.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/capture/camera-snapper.yaml");
        }
    }

    dependency CameraSnapperUsesCameraOpener from CameraSnapper to AgentVisionCore_CameraOpener::CameraOpener;
    dependency CameraSnapperUsesCameraSelector from CameraSnapper to AgentVisionCore_CameraSelector::CameraSelector;
    dependency CameraSnapperUsesCameraImage from CameraSnapper to AgentVisionCore_CameraImage::CameraImage;
    dependency CameraSnapperUsesCameraEnumerator from CameraSnapper to AgentVisionCore_CameraEnumerator::CameraEnumerator;
}