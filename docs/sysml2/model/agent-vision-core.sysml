package AgentVisionCore {
    public import AgentVisionCore_CamerasSubsystem::*;
    private import OtsDependencies::*;
    private import SoftwareItemMetadata::*;

    doc
    /*
     * AgentVisionCore is the DemaConsulting.AgentVision.Core .NET library. It owns ALL camera
     * logic and is distributed as a NuGet package. It is consumed by the agentvision
     * command-line tool (a thin front-end) and by any other .NET application, and never depends
     * on the tool. The library currently enumerates cameras (the Cameras subsystem); further
     * camera capabilities are added as subsystems and units with their own requirements, design,
     * and verification.
     */

    part def AgentVisionCoreSystem {
        doc /* The DemaConsulting.AgentVision.Core library system as a whole: the home of all camera logic. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/AgentVisionCoreTests.cs");
            designFiles = ("docs/design/agent-vision-core.md");
            verificationFiles = ("docs/verification/agent-vision-core.md");
            requirementFiles = ("docs/reqstream/agent-vision-core.yaml");
        }

        part cameras : CamerasSubsystem;

        // Runtime OTS dependency: ships as a package dependency of the library.
        part flashCap : FlashCap;
    }
}
