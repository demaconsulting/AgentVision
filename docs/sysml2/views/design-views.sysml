// Views rendered into docs/design/generated/ for the Design document.
// Per SysML2Tools syntax, 'expose' is only valid inside a named 'view' usage
// (not a 'view def' definition), and it - not 'render' - is what scopes the
// rendered diagram content.
//
// The repository holds two systems: the AgentVisionCore library and the AgentVisionTool
// command-line tool. Each system has its own view; units are shown by their subsystem's view.
package AgentVisionDesignViews {
    private import StandardViewDefinitions::*;
    private import AgentVisionCore::*;
    private import AgentVisionTool::*;

    view SoftwareStructureView : InterconnectionView {
        expose AgentVisionCore::AgentVisionCoreSystem;
        expose AgentVisionTool::AgentVisionToolSystem;
        filter not (@SysML::Documentation);
    }

    view AgentVisionCoreView {
        expose AgentVisionCoreSystem;
    }

    view AgentVisionToolView {
        expose AgentVisionToolSystem;
    }

    view CliView {
        expose CliSubsystem;
    }

    view SelfTestView {
        expose SelfTestSubsystem;
    }

    view UtilitiesView {
        expose UtilitiesSubsystem;
    }

    view OtsView : GeneralView {
        expose OtsDependencies::*;
        filter not (@SysML::Documentation);
    }
}
