package AgentVisionTool_Validation {
    private import SoftwareItemMetadata::*;

    part def Validation {
        doc /* Orchestrates the self-validation test suite and reports pass/fail results. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/SelfTest/Validation.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/SelfTest/ValidationTests.cs");
            designFiles = ("docs/design/agent-vision-tool/self-test/validation.md");
            verificationFiles = ("docs/verification/agent-vision-tool/self-test/validation.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/self-test/validation.yaml");
        }
    }
}
