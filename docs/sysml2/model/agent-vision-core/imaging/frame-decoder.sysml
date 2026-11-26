package AgentVisionCore_FrameDecoder {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;

    part def FrameDecoder {
        doc /* Decodes the bytes of a camera frame (JPEG, PNG, or BMP) into pixels with CanvasNet, and repairs the defects of camera frames: a JPEG without Huffman tables, a top-down bitmap, and a 32-bit bitmap whose unused byte is zero. Holds the standard Huffman tables. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.AgentVision.Core/Imaging/FrameDecoder.cs",
                "src/DemaConsulting.AgentVision.Core/Imaging/StandardJpegTables.cs"
            );
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Imaging/FrameDecoderTests.cs");
            designFiles = ("docs/design/agent-vision-core/imaging/frame-decoder.md");
            verificationFiles = ("docs/verification/agent-vision-core/imaging/frame-decoder.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/imaging/frame-decoder.yaml");
        }
    }

    // The decoder uses CanvasNet, the runtime OTS imaging library.
    dependency FrameDecoderUsesCanvasNet from FrameDecoder to CanvasNet;
}