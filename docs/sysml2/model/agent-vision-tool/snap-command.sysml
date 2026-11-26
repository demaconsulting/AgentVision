package AgentVisionTool_SnapCommand {
    private import SoftwareItemMetadata::*;

    part def SnapCommand {
        doc /* The snap command: maps the options of the context to a request of the AgentVisionCore library's camera snapper, saves one picture of a camera to a file, and reports the result or the error through the context output channels. A thin front-end with no camera logic. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/SnapCommand.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/SnapCommandTests.cs");
            designFiles = ("docs/design/agent-vision-tool/snap-command.md");
            verificationFiles = ("docs/verification/agent-vision-tool/snap-command.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/snap-command.yaml");
        }
    }

    // The command asks the library's camera snapper for one picture and writes the result through the
    // context. The items are named with their qualified packages, so the unit imports nothing.
    dependency SnapCommandUsesCameraSnapper from SnapCommand to AgentVisionCore_CameraSnapper::CameraSnapper;
    dependency SnapCommandUsesContext from SnapCommand to AgentVisionTool_Context::Context;
}
