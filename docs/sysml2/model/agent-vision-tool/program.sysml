package AgentVisionTool_Program {
    private import SoftwareItemMetadata::*;

    part def Program {
        doc /* Entry point and execution orchestrator. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/Program.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/ProgramTests.cs");
            designFiles = ("docs/design/agent-vision-tool/program.md");
            verificationFiles = ("docs/verification/agent-vision-tool/program.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/program.yaml");
        }
    }
}
