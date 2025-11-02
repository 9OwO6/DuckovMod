@echo off
echo Building BetterThrowingSystem Mod...

REM Set your game path here
set DUCKOV_PATH=D:\SteamLibrary\steamapps\common\Escape from Duckov

REM Set MSBuild property for DuckovPath and build in Release mode
dotnet build BetterThrowingSystem.csproj -c Release /p:DuckovPath="%DUCKOV_PATH%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful!
    echo DLL location: bin\Release\netstandard2.1\BetterThrowingSystem.dll
    echo.
    echo Copying DLL to mod folder...
    copy /Y bin\Release\netstandard2.1\BetterThrowingSystem.dll .
    if %ERRORLEVEL% EQU 0 (
        echo DLL copied to mod folder.
        echo.
        echo NOTE: Run prepare_for_upload.bat to prepare files for Steam upload.
    ) else (
        echo Warning: Failed to copy DLL. Please copy manually.
    )
) else (
    echo.
    echo Build failed! Please check the error messages above.
)

pause

