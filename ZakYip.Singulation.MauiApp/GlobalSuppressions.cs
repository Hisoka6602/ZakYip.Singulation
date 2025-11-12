// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
using System.Diagnostics.CodeAnalysis;

// MAUI UI code often needs to catch all exceptions to prevent app crashes
// and provide user-friendly error messages
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "MAUI ViewModels need to catch all exceptions to prevent UI crashes",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.MauiApp.ViewModels")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "MAUI Services need to catch all exceptions to prevent app crashes",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.MauiApp.Services")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "MAUI Helper methods need to catch all exceptions",
    Scope = "namespaceanddescendants",
    Target = "~N:ZakYip.Singulation.MauiApp.Helpers")]
