package AgentVisionTool_Context {
    private import SoftwareItemMetadata::*;

    part def Context {
        doc /* Command-line argument parser and I/O owner for one tool invocation. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/Cli/Context.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/Cli/ContextTests.cs");
            designFiles = ("docs/design/agent-vision-tool/cli/context.md");
            verificationFiles = ("docs/verification/agent-vision-tool/cli/context.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/cli/context.yaml");
        }
    }
}
