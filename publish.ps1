<#
.SYNOPSIS
    TypedQuery Package Release Script
    
.DESCRIPTION
    Increments version, builds, packs, and optionally publishes NuGet packages.
    
.PARAMETER Version
    The new version number (e.g., "1.1.0"). If not specified, increments patch version.
    
.PARAMETER BumpType
    Type of version bump: "major", "minor", or "patch" (default: "patch")
    
.PARAMETER NuGetApiKey
    NuGet.org API key for publishing. If not provided, packages are created but not published.
    
.PARAMETER SkipTests
    Skip running tests before packing.
    
.PARAMETER SkipBuild  
    Skip building (useful if you just built).

.EXAMPLE
    .\publish.ps1 -BumpType patch
    # Increments patch version (1.0.0 -> 1.0.1), builds, tests, and packs
    
.EXAMPLE
    .\publish.ps1 -Version "2.0.0" -NuGetApiKey "your-api-key"
    # Sets version to 2.0.0 and publishes to NuGet.org
    
.EXAMPLE
    .\publish.ps1 -BumpType minor -SkipTests
    # Increments minor version (1.0.0 -> 1.1.0), skips tests
#>

param(
    [string]$Version,
    [ValidateSet("major", "minor", "patch")]
    [string]$BumpType = "patch",
    [string]$NuGetApiKey,
    [switch]$SkipTests,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Project files to update
$ProjectFiles = @(
    "src\TypedQuery.Abstractions\TypedQuery.Abstractions.csproj",
    "src\TypedQuery\TypedQuery.csproj",
    "src\TypedQuery.EntityFrameworkCore\TypedQuery.EntityFrameworkCore.csproj"
)

$OutputDir = "artifacts"

function Get-CurrentVersion {
    $csproj = Get-Content $ProjectFiles[0] -Raw
    if ($csproj -match '<Version>(\d+\.\d+\.\d+)</Version>') {
        return $matches[1]
    }
    return "1.0.0"
}

function Get-NextVersion {
    param([string]$Current, [string]$BumpType)
    
    $parts = $Current.Split('.')
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($BumpType) {
        "major" { $major++; $minor = 0; $patch = 0 }
        "minor" { $minor++; $patch = 0 }
        "patch" { $patch++ }
    }
    
    return "$major.$minor.$patch"
}

function Update-Version {
    param([string]$NewVersion)
    
    foreach ($file in $ProjectFiles) {
        Write-Host "  Updating $file" -ForegroundColor Gray
        $content = Get-Content $file -Raw
        $content = $content -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$NewVersion</Version>"
        Set-Content $file $content -NoNewline
    }
}

# ===========================================
# Main Script
# ===========================================

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  TypedQuery Package Release Script" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Determine version
$currentVersion = Get-CurrentVersion
Write-Host "Current version: $currentVersion" -ForegroundColor Yellow

if ($Version) {
    $newVersion = $Version
} else {
    $newVersion = Get-NextVersion -Current $currentVersion -BumpType $BumpType
}

Write-Host "New version: $newVersion" -ForegroundColor Green
Write-Host ""

# Confirm
$confirm = Read-Host "Proceed with version $newVersion? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Aborted." -ForegroundColor Red
    exit 1
}

# Step 1: Update version in all project files
Write-Host "`n[1/5] Updating version in project files..." -ForegroundColor Cyan
Update-Version -NewVersion $newVersion
Write-Host "  Done!" -ForegroundColor Green

# Step 2: Clean
Write-Host "`n[2/5] Cleaning..." -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null
dotnet clean -c Release --verbosity quiet
Write-Host "  Done!" -ForegroundColor Green

# Step 3: Build
if (-not $SkipBuild) {
    Write-Host "`n[3/5] Building..." -ForegroundColor Cyan
    dotnet build -c Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Done!" -ForegroundColor Green
} else {
    Write-Host "`n[3/5] Skipping build..." -ForegroundColor Yellow
}

# Step 4: Run tests
if (-not $SkipTests) {
    Write-Host "`n[4/5] Running tests..." -ForegroundColor Cyan
    dotnet run --project src\TypedQuery.Tests\TypedQuery.Tests.csproj --no-build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Done!" -ForegroundColor Green
} else {
    Write-Host "`n[4/5] Skipping tests..." -ForegroundColor Yellow
}

# Step 5: Pack
Write-Host "`n[5/5] Packing NuGet packages..." -ForegroundColor Cyan
foreach ($file in $ProjectFiles) {
    Write-Host "  Packing $file" -ForegroundColor Gray
    dotnet pack $file -c Release --no-build -o $OutputDir --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Pack failed for $file!" -ForegroundColor Red
        exit 1
    }
}
Write-Host "  Done!" -ForegroundColor Green

# List packages
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Packages created in '$OutputDir':" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Get-ChildItem $OutputDir -Filter "*.nupkg" | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor White
}

# Step 6: Publish (if API key provided)
if ($NuGetApiKey) {
    Write-Host "`n[6/6] Publishing to NuGet.org..." -ForegroundColor Cyan
    
    $packages = Get-ChildItem $OutputDir -Filter "*.nupkg" | Where-Object { $_.Name -notlike "*.symbols.*" }
    
    foreach ($pkg in $packages) {
        Write-Host "  Publishing $($pkg.Name)..." -ForegroundColor Gray
        dotnet nuget push $pkg.FullName --api-key $NuGetApiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  Failed to publish $($pkg.Name)!" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "  Done!" -ForegroundColor Green
    
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "  Successfully published v$newVersion!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
} else {
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "  Packages ready for publishing!" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To publish manually, run:" -ForegroundColor White
    Write-Host "  dotnet nuget push artifacts\*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Or re-run this script with -NuGetApiKey parameter." -ForegroundColor White
}

Write-Host ""
