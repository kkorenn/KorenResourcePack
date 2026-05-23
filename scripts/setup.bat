@echo off
rem Dependency check + NuGet prime for KorenResourcePack (Windows).
rem Run once before scripts\koren_build.sh (or `dotnet build -p:Install=true`).
setlocal EnableDelayedExpansion

pushd "%~dp0.."

set MISSING=0
set UNITY_VERSION=6000.3.10f1
if exist "unity\current\ProjectSettings\ProjectVersion.txt" (
  for /f "tokens=2" %%v in ('findstr /b "m_EditorVersion:" unity\current\ProjectSettings\ProjectVersion.txt') do set UNITY_VERSION=%%v
)

echo == Required ==
where dotnet >nul 2>&1
if %ERRORLEVEL%==0 (
  for /f "delims=" %%v in ('dotnet --version') do echo   [ok]      dotnet ^(%%v^)
) else (
  echo   [missing] dotnet SDK 6.0+ - install: https://dotnet.microsoft.com/download   ^(or: winget install Microsoft.DotNet.SDK.8^)
  set MISSING=1
)

echo == Optional (only needed to rebuild AssetBundles) ==
where python >nul 2>&1
if %ERRORLEVEL%==0 (
  for /f "delims=" %%v in ('python --version 2^>^&1') do echo   [ok]      %%v
) else (
  echo   [warn]    python - only needed if Unity font assets have .otf files. Install: winget install Python.Python.3
)

set UNITY_HUB=%PROGRAMFILES%\Unity\Hub\Editor
if exist "%UNITY_HUB%\%UNITY_VERSION%\Editor\Unity.exe" (
  echo   [ok]      Unity %UNITY_VERSION%
) else if exist "%UNITY_HUB%" (
  echo   [warn]    Unity %UNITY_VERSION% not installed ^(other versions may be present^). Use SKIP_BUNDLE=1 or install via Unity Hub.
) else (
  echo   [warn]    Unity %UNITY_VERSION% - only needed to rebuild AssetBundles. Install via Unity Hub: https://unity.com/download   Otherwise set SKIP_BUNDLE=1.
)

echo == Game / UMM ==
set "GAME=%PROGRAMFILES(X86)%\Steam\steamapps\common\A Dance of Fire and Ice"
if not exist "%GAME%" set "GAME=%PROGRAMFILES%\Steam\steamapps\common\A Dance of Fire and Ice"
if exist "%GAME%" (
  echo   [ok]      ADOFAI install: %GAME%
) else (
  echo   [missing] ADOFAI install not found. Pass -p:Game="C:\path\to\game" to dotnet build.
  set MISSING=1
)

set "MANAGED=%GAME%\A Dance of Fire and Ice_Data\Managed"
if exist "%MANAGED%\UnityModManager\UnityModManager.dll" (
  echo   [ok]      UnityModManager installed
) else (
  echo   [warn]    UnityModManager.dll not found in %MANAGED%\UnityModManager - install UMM into ADOFAI: https://www.nexusmods.com/site/mods/21
)

echo.
if "%MISSING%"=="1" (
  echo Some required deps missing. Install them, then re-run scripts\setup.bat.
  popd
  exit /b 1
)

echo == Prime NuGet packages ==
dotnet restore
if errorlevel 1 (
  popd
  exit /b 1
)

echo.
echo Setup complete. Build:
echo   dotnet build -c Release
echo.
echo   dotnet build -c Release -p:Legacy=true   ^(pre-3.1.0 / r141 ADOFAI^)
echo.
echo   dotnet build -c Release -p:SkipBundle=true   ^(skip Unity AssetBundle rebuild^)
echo.
echo   dotnet build -c Release -p:SkipBundle=true -p:Legacy=true   ^(legacy + skip bundle^)
echo.
echo.  dotnet build -c Release -p:Install=true ^(to install directly to ur mods folder)
popd
endlocal
