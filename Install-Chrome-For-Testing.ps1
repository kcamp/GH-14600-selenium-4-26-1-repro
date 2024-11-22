# hurry up Invoke-WebRequest
$ProgressPreference = 'SilentlyContinue'

# Use the 131.0.6778.85 version
$ChromeVersion = "131.0.6778.85"
$DownloadUrl = "https://storage.googleapis.com/chrome-for-testing-public/$ChromeVersion/win64/chrome-win64.zip"
$TargetPath = ".\chrome-win64-$ChromeVersion.zip"
$InstallationPath = Join-Path "." "ChromeForTesting"

if(-not (Test-Path $InstallationPath))
{
	New-Item $InstallationPath -ItemType Directory | Out-Null
}

if(-not (Test-Path $InstallationPath)) 
{
	Write-Host "Downloading $TargetPath from $DownloadUrl"
	Invoke-WebRequest $DownloadUrl -OutFile $TargetPath
}
else 
{
	Write-Host "$TargetPath already exists.  Continuing with existing file."
}

Write-Host "Unzipping $TargetPath to $InstallationPath"
Expand-Archive $TargetPath $InstallationPath -Force