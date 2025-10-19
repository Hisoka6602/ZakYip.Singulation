$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path "$root/.."
$project = Join-Path $solutionRoot "ZakYip.Singulation.ConsoleDemo/ZakYip.Singulation.ConsoleDemo.csproj"

Write-Host "[dryrun] Running regression scenario" -ForegroundColor Cyan
& dotnet run --project $project -- --regression
