Set-Location "$PSScriptRoot\mobile\FraudGuard-AI"

Write-Host "Building signed release APK..." -ForegroundColor Cyan

dotnet build FraudGuardAI.csproj `
    "-t:PackageForAndroid" `
    "-f:net8.0-android" `
    "-c:Release" `
    "-p:AndroidSigningKeyStore=..\..\fraudguard.keystore" `
    "-p:AndroidSigningKeyAlias=fraudguard" `
    "-p:AndroidSigningKeyPass=fraudguard123" `
    "-p:AndroidSigningStorePass=fraudguard123"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build succeeded!" -ForegroundColor Green
    $apkFiles = Get-ChildItem -Path "bin\Release\net8.0-android" -Filter "*.apk" -Recurse
    foreach ($apk in $apkFiles) {
        $size = [math]::Round($apk.Length / 1MB, 1)
        Write-Host "APK: $($apk.FullName) ($size MB)" -ForegroundColor Yellow
    }
} else {
    Write-Host "Build FAILED (exit code $LASTEXITCODE)" -ForegroundColor Red
}
