package AgentVisionTool_PathHelpers {
    private import SoftwareItemMetadata::*;

    part def PathHelpers {
        doc /* Safe path-combination utility guarding against path-traversal. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Tool/Utilities/PathHelpers.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/Utilities/PathHelpersTests.cs");
            designFiles = ("docs/design/agent-vision-tool/utilities/path-helpers.md");
            verificationFiles = ("docs/verification/agent-vision-tool/utilities/path-helpers.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/utilities/path-helpers.yaml");
        }
    }
}
