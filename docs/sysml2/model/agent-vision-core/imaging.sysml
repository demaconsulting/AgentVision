package AgentVisionCore_ImagingSubsystem {
    public import AgentVisionCore_FrameDecoder::*;
    public import AgentVisionCore_CameraImage::*;
    private import SoftwareItemMetadata::*;

    part def ImagingSubsystem {
        doc /* Turns the bytes of a camera frame into a picture that can be cropped and saved as PNG, JPEG, or BMP: the frame decoder decodes the frame and repairs the defects of camera frames, and the camera image is the picture. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Imaging/ImagingSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-core/imaging.md");
            verificationFiles = ("docs/verification/agent-vision-core/imaging.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/imaging.yaml");
        }

        part frameDecoder : FrameDecoder;
        part cameraImage : CameraImage;
    }
}