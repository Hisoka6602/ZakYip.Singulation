// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// Test helpers and test framework code legitimately catch all exceptions
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Test helpers need to catch all exceptions for safe test execution",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.Tests.TestHelpers")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Test framework code needs to catch all exceptions",
    Scope = "type",
    Target = "~T:ZakYip.Singulation.Tests.MiniTestFramework")]
