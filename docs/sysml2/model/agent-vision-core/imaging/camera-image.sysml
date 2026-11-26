package AgentVisionCore_CameraImage {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;

    part def CameraImage {
        doc /* The library's picture: reports its size, crops a region into a new independent image, and saves as PNG, JPEG, or BMP by the file extension. Also owns the crop rectangle and the file format types. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.AgentVision.Core/Imaging/CameraImage.cs",
                "src/DemaConsulting.AgentVision.Core/Imaging/CropRectangle.cs",
                "src/DemaConsulting.AgentVision.Core/Imaging/ImageFileFormat.cs"
            );
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Imaging/CameraImageTests.cs");
            designFiles = ("docs/design/agent-vision-core/imaging/camera-image.md");
            verificationFiles = ("docs/verification/agent-vision-core/imaging/camera-image.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/imaging/camera-image.yaml");
        }
    }

    // The image is created by the frame decoder and crops and saves with CanvasNet.
    dependency CameraImageUsesFrameDecoder from CameraImage to AgentVisionCore_FrameDecoder::FrameDecoder;
    dependency CameraImageUsesCanvasNet from CameraImage to CanvasNet;
}