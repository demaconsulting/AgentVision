package AgentVisionTool_UtilitiesSubsystem {
    public import AgentVisionTool_PathHelpers::*;
    private import SoftwareItemMetadata::*;

    part def UtilitiesSubsystem {
        doc /* Shared, independently testable helper functions used by other subsystems. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Tool.Tests/Utilities/UtilitiesSubsystemTests.cs");
            designFiles = ("docs/design/agent-vision-tool/utilities.md");
            verificationFiles = ("docs/verification/agent-vision-tool/utilities.md");
            requirementFiles = ("docs/reqstream/agent-vision-tool/utilities.yaml");
        }

        part pathHelpers : PathHelpers;
    }
}
