$ErrorActionPreference = 'Stop'
$backendDirectory = Split-Path -Parent $PSScriptRoot
$certificateDirectory = Join-Path $backendDirectory 'certs'
New-Item -ItemType Directory -Path $certificateDirectory -Force | Out-Null
$certificateAcl = [Security.AccessControl.DirectorySecurity]::new()
$certificateAcl.SetAccessRuleProtection($true, $false)
$inheritance = [Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit'
foreach ($identity in @([Security.Principal.WindowsIdentity]::GetCurrent().User, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
    $certificateAcl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($identity, 'FullControl', $inheritance, 'None', 'Allow'))
}
Set-Acl -LiteralPath $certificateDirectory -AclObject $certificateAcl
$certificatePath = Join-Path $certificateDirectory 'localhost.pfx'
$passwordPath = Join-Path $certificateDirectory 'password.txt'
$publicCertificatePath = Join-Path $certificateDirectory 'localhost.pem'
$environmentPath = Join-Path $backendDirectory '.env'
if (!(Test-Path -LiteralPath $passwordPath)) {
    $certificateBytes = [byte[]]::new(32)
    $randomGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $randomGenerator.GetBytes($certificateBytes) } finally { $randomGenerator.Dispose() }
    $certificatePassword = [Convert]::ToBase64String($certificateBytes)
    [IO.File]::WriteAllText($passwordPath, $certificatePassword)
}
$certificatePassword = [IO.File]::ReadAllText($passwordPath).Trim()
if ([string]::IsNullOrWhiteSpace($certificatePassword)) { throw 'The local HTTPS certificate password is empty.' }
dotnet dev-certs https --trust --quiet
if ($LASTEXITCODE -ne 0) { throw 'Could not trust the local development certificate.' }
dotnet dev-certs https --export-path $certificatePath --password $certificatePassword --quiet
if ($LASTEXITCODE -ne 0) { throw 'Could not export the HTTPS certificate.' }
dotnet dev-certs https --export-path $publicCertificatePath --format PEM --quiet
if ($LASTEXITCODE -ne 0) { throw 'Could not export the public certificate.' }
if (Test-Path -LiteralPath $environmentPath) {
    $environmentText = [IO.File]::ReadAllText($environmentPath)
    $entry = 'HTTPS_CERT_PASSWORD=' + $certificatePassword
    if ($environmentText -match '(?m)^HTTPS_CERT_PASSWORD=.*$') {
        $environmentText = [regex]::Replace($environmentText, '(?m)^HTTPS_CERT_PASSWORD=.*$', $entry)
    } else { $environmentText = $environmentText.TrimEnd() + [Environment]::NewLine + $entry + [Environment]::NewLine }
    [IO.File]::WriteAllText($environmentPath, $environmentText)
}
Write-Output 'Local HTTPS certificate is ready. Certificate files and passwords are excluded from Git.'
