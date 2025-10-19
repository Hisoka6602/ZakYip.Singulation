$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path "$root/.."

Write-Host "[selfcheck] dotnet build" -ForegroundColor Cyan
& dotnet build (Join-Path $solutionRoot "ZakYip.Singulation.sln")
