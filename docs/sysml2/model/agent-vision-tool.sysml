package AgentVisionTool {
    public import AgentVisionTool_Program::*;
    public import AgentVisionTool_CliSubsystem::*;
    public import AgentVisionTool_SelfTestSubsystem::*;
    public import AgentVisionTool_UtilitiesSubsystem::*;
    private import SoftwareItemMetadata::*;

    doc
    /*
     * AgentVision is a template .NET command-line application demonstrating
     * DemaConsulting's compliance/documentation tooling conventions.
     */

    part def AgentVisionToolSystem {
        doc /* The AgentVision system as a whole. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/IntegrationTests.cs");
            designFiles = ("docs/design/agent-vision-tool.md");
            verificationFiles = ("docs/verification/agent-vision-tool.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool.yaml");
        }

        part program : Program;
        part cli : CliSubsystem;
        part selfTest : SelfTestSubsystem;
        part utilities : UtilitiesSubsystem;
    }
}
