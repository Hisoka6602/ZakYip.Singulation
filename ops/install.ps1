param(
    [string]$PublishDir = "publish/host"
)

Write-Host "[install] Publishing ZakYip.Singulation.Host to $PublishDir" -ForegroundColor Cyan
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path "$root/.."
$project = Join-Path $solutionRoot "ZakYip.Singulation.Host/ZakYip.Singulation.Host.csproj"
$publishPath = Join-Path $solutionRoot $PublishDir

if (-not (Test-Path $project)) {
    throw "Host project not found at $project"
}

if (-not (Test-Path $publishPath)) {
    Write-Host "[install] Creating $publishPath" -ForegroundColor DarkCyan
    New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
}

& dotnet publish $project -c Release -o $publishPath

$bat = Join-Path $solutionRoot "ZakYip.Singulation.Host/install.bat"
if (Test-Path $bat) {
    Write-Host "[install] Invoking legacy install.bat" -ForegroundColor Yellow
    & $bat
}
