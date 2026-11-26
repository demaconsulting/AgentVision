package AgentVisionCore_CameraSelector {
    private import SoftwareItemMetadata::*;

    part def CameraSelector {
        doc /* Decides which camera a selector (index, identifier, or name) means and which of its formats a request for a size and frame rate means: pure functions that are tested exactly. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.AgentVision.Core/Capture/CameraSelector.cs");
            testFiles = ("test/DemaConsulting.AgentVision.Core.Tests/Capture/CameraSelectorTests.cs");
            designFiles = ("docs/design/agent-vision-core/capture/camera-selector.md");
            verificationFiles = ("docs/verification/agent-vision-core/capture/camera-selector.md");
            requirementFiles = ("docs/reqstream/agent-vision-core/capture/camera-selector.yaml");
        }
    }

    // The selector reads the camera and format descriptions of the Cameras subsystem; the items are named with their
    // qualified packages so that no import (and no import cycle) is created.
    dependency CameraSelectorUsesCameraEnumerator from CameraSelector to AgentVisionCore_CameraEnumerator::CameraEnumerator;
}