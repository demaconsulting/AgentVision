package OtsDependencies {
    private import SoftwareItemMetadata::*;

    doc /* Off-the-shelf (OTS) runtime dependencies used by AgentVision. */

    part def HelpCards {
        doc /* OTS: DemaConsulting.HelpCards. */

        metadata info : SoftwareInfo {
            designFiles = ("docs/design/ots/help-cards.md");
            verificationFiles = ("docs/verification/ots/help-cards.md");
            requirementFiles = ("docs/reqstream/ots/help-cards.yaml");
        }
    }

    part def FlashCap {
        doc /* OTS: FlashCap, which discovers the video capture devices of the host and describes their formats. */

        metadata info : SoftwareInfo {
            designFiles = ("docs/design/ots/flashcap.md");
            verificationFiles = ("docs/verification/ots/flashcap.md");
            requirementFiles = ("docs/reqstream/ots/flashcap.yaml");
        }
    }
}
