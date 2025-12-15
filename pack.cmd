@echo off
REM TypedQuery Quick Pack Script
REM Usage: pack.cmd [version]
REM Example: pack.cmd 1.1.0

setlocal enabledelayedexpansion

set VERSION=%1
if "%VERSION%"=="" (
    echo Usage: pack.cmd [version]
    echo Example: pack.cmd 1.1.0
    exit /b 1
)

echo.
echo ========================================
echo   TypedQuery Pack Script v%VERSION%
echo ========================================
echo.

REM Clean
echo [1/4] Cleaning...
if exist artifacts rmdir /s /q artifacts
mkdir artifacts
dotnet clean -c Release -v q

REM Build
echo [2/4] Building...
dotnet build -c Release -v m
if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

REM Test
echo [3/4] Testing...
dotnet run --project src\TypedQuery.Tests\TypedQuery.Tests.csproj --no-build -c Release
if errorlevel 1 (
    echo Tests failed!
    exit /b 1
)

REM Pack
echo [4/4] Packing...
dotnet pack src\TypedQuery.Abstractions\TypedQuery.Abstractions.csproj -c Release --no-build -o artifacts -p:Version=%VERSION%
dotnet pack src\TypedQuery\TypedQuery.csproj -c Release --no-build -o artifacts -p:Version=%VERSION%
dotnet pack src\TypedQuery.EntityFrameworkCore\TypedQuery.EntityFrameworkCore.csproj -c Release --no-build -o artifacts -p:Version=%VERSION%

echo.
echo ========================================
echo   Packages created in 'artifacts':
echo ========================================
dir /b artifacts\*.nupkg

echo.
echo To publish:
echo   dotnet nuget push artifacts\*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
echo.
