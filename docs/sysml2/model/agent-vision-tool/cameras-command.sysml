package AgentVisionTool_CamerasCommand {
    private import SoftwareItemMetadata::*;

    part def CamerasCommand {
        doc /* The cameras command: prints the cameras that the AgentVisionCore library lists, and the formats each supports, through the context output channels. A thin front-end with no camera logic. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/CamerasCommand.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/CamerasCommandTests.cs");
            designFiles = ("docs/design/agent-vision-tool/cameras-command.md");
            verificationFiles = ("docs/verification/agent-vision-tool/cameras-command.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/cameras-command.yaml");
        }
    }

    // The command prints the list that the library's camera enumerator returns, and writes it through the
    // context. The items are named with their qualified packages, so the unit imports nothing.
    dependency CamerasCommandUsesCameraEnumerator from CamerasCommand to AgentVisionCore_CameraEnumerator::CameraEnumerator;
    dependency CamerasCommandUsesContext from CamerasCommand to AgentVisionTool_Context::Context;
}
