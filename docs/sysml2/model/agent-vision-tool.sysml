package AgentVisionTool {
    public import AgentVisionTool_Program::*;
    public import AgentVisionTool_CamerasCommand::*;
    public import AgentVisionTool_CliSubsystem::*;
    public import AgentVisionTool_SelfTestSubsystem::*;
    public import AgentVisionTool_UtilitiesSubsystem::*;
    private import SoftwareItemMetadata::*;

    doc
    /*
     * AgentVisionTool is the .NET command-line application (command agentvision) that
     * exposes AgentVision, low-latency webcam capture for AI agents, from the command
     * line. It is a thin front-end only: argument parsing, console and log output, exit
     * codes, built-in help, and self-validation. All camera logic belongs in the
     * AgentVisionCore library.
     */

    part def AgentVisionToolSystem {
        doc /* The AgentVisionTool system as a whole: the thin command-line front-end. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/IntegrationTests.cs");
            designFiles = ("docs/design/agent-vision-tool.md");
            verificationFiles = ("docs/verification/agent-vision-tool.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool.yaml");
        }

        part program : Program;
        part camerasCommand : CamerasCommand;
        part cli : CliSubsystem;
        part selfTest : SelfTestSubsystem;
        part utilities : UtilitiesSubsystem;
    }

    // The tool is a thin front-end for the AgentVisionCore library, which never depends on the tool.
    dependency AgentVisionToolUsesAgentVisionCore from AgentVisionToolSystem to AgentVisionCore::AgentVisionCoreSystem;
}
