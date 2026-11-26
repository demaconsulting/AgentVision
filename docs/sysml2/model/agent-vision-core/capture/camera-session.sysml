package AgentVisionCore_CameraSession {
    private import SoftwareItemMetadata::*;

    part def CameraSession {
        doc /* An open ("hot") camera: receives the frames of the device, discards the warm-up, publishes the newest frame, serves snapshots from any thread without waiting, and stops the device when disposed. Also owns the frame, the open options, and the exception types of the Capture subsystem. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.AgentVision.Core/Capture/CameraSession.cs",
                "src/DemaConsulting.AgentVision.Core/Capture/CameraFrame.cs",
                "src/DemaConsulting.AgentVision.Core/Capture/CameraOpenOptions.cs",
                "src/DemaConsulting.AgentVision.Core/Capture/CameraException.cs"
            );
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Capture/CameraSessionTests.cs");
            designFiles = ("docs/design/agent-vision-core/capture/camera-session.md");
            verificationFiles = ("docs/verification/agent-vision-core/capture/camera-session.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/capture/camera-session.yaml");
        }
    }

    // A frame decodes on demand into a picture of the Imaging subsystem.
    dependency CameraSessionUsesCameraImage from CameraSession to AgentVisionCore_CameraImage::CameraImage;
}