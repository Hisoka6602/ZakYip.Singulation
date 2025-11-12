// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Infrastructure services must be resilient - no exception should crash the system
// Services are designed to log errors and continue operation

// Background workers must catch all exceptions to continue running
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Background workers must be resilient and continue running despite errors",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Infrastructure.Workers")]

// Runtime isolation code must catch all exceptions for safety
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Runtime isolation code must prevent any exception from escaping",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Infrastructure.Runtime")]

// Cabinet pipeline must be resilient
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Cabinet pipeline must handle all errors gracefully to prevent system crashes",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Infrastructure.Cabinet")]
