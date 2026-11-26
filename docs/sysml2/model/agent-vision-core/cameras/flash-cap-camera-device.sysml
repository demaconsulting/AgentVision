package AgentVisionCore_FlashCapCameraDevice {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;

    part def FlashCapCameraDevice {
        doc /* One FlashCap capture device that is streaming: opens and starts it with a latest-frame-only strategy, turns each FlashCap pixel buffer into an encoded frame, and stops and releases the device in a bounded time. Also owns the encoded frame type. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.AgentVision.Core/Cameras/FlashCapCameraDevice.cs",
                "src/DemaConsulting.AgentVision.Core/Cameras/EncodedFrame.cs"
            );
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Cameras/FlashCapCameraDeviceTests.cs");
            designFiles = ("docs/design/agent-vision-core/cameras/flash-cap-camera-device.md");
            verificationFiles = ("docs/verification/agent-vision-core/cameras/flash-cap-camera-device.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/cameras/flash-cap-camera-device.yaml");
        }
    }

    // The device drives FlashCap, the runtime OTS library, and reports failures with the open exception of the
    // Capture subsystem (the item that owns the exception types is the session).
    dependency FlashCapCameraDeviceUsesFlashCap from FlashCapCameraDevice to FlashCap;
    dependency FlashCapCameraDeviceUsesCameraSession from FlashCapCameraDevice to AgentVisionCore_CameraSession::CameraSession;
}