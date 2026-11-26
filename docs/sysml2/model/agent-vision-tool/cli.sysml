package AgentVisionTool_CliSubsystem {
    public import AgentVisionTool_Context::*;
    public import AgentVisionTool_HelpCommand::*;
    private import SoftwareItemMetadata::*;

    part def CliSubsystem {
        doc /* Command-line argument parsing, output-channel management, exit-code tracking, and the built-in help command. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/Cli/CliSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-tool/cli.md");
            verificationFiles = ("docs/verification/agent-vision-tool/cli.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/cli.yaml");
        }

        part context : Context;
        part helpCommand : HelpCommand;
    }
}
