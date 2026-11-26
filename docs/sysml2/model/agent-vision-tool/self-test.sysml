package AgentVisionTool_SelfTestSubsystem {
    public import AgentVisionTool_Validation::*;
    private import SoftwareItemMetadata::*;

    part def SelfTestSubsystem {
        doc /* Self-validation framework exercising the tool's own capabilities on demand. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/SelfTest/SelfTestSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-tool/self-test.md");
            verificationFiles = ("docs/verification/agent-vision-tool/self-test.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/self-test.yaml");
        }

        part validation : Validation;
    }
}
