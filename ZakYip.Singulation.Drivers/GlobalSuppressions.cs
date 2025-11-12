// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Hardware drivers must be resilient - communication errors should not crash the system
// Drivers are designed to log errors and report status through events

// Event aggregators must catch all exceptions to prevent breaking event subscribers
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event handlers must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Drivers.Common.AxisEventAggregator.FireEachNonBlocking``1(System.EventHandler{``0},System.Object,``0)")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Event handlers must not throw to prevent breaking subscribers",
    Scope = "member",
    Target = "~M:ZakYip.Singulation.Drivers.Leadshine.LeadshineHelpers.FireEachNonBlocking``1(System.EventHandler{``0},System.Object,``0)")]

// Health monitoring must continue despite errors
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Health monitor must be resilient and continue monitoring",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Drivers.Health")]

// Leadshine DLL interop may throw various exceptions - must be caught for safety
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "DLL interop requires catching all exceptions for safety",
    Scope = "type",
    Target = "~T:ZakYip.Singulation.Drivers.Leadshine.LeadshineLtdmcBusAdapter")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "DLL interop requires catching all exceptions for safety",
    Scope = "type",
    Target = "~T:ZakYip.Singulation.Drivers.Leadshine.LeadshineLtdmcAxisDrive")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "DLL interop requires catching all exceptions for safety",
    Scope = "type",
    Target = "~T:ZakYip.Singulation.Drivers.Leadshine.LeadshineCabinetIoModule")]
