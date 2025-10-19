$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path "$root/.."
$bat = Join-Path $solutionRoot "ZakYip.Singulation.Host/unstall.bat"

if (Test-Path $bat) {
    Write-Host "[uninstall] Invoking legacy unstall.bat" -ForegroundColor Yellow
    & $bat
}
else {
    Write-Host "[uninstall] Legacy batch script not found, skipping." -ForegroundColor DarkYellow
}
