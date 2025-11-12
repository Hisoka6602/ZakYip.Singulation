// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Controllers rely on GlobalExceptionHandlerMiddleware to catch and handle all exceptions
// This provides a centralized error handling strategy with proper HTTP response codes
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", 
    Justification = "Controllers use global exception handling middleware", 
    Scope = "namespaceanddescendants", 
    Target = "~N:ZakYip.Singulation.Host.Controllers")]

// SignalR hubs also rely on the global exception handler
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "SignalR hubs use global exception handling",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Host.SignalR")]
