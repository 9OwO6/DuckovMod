@echo off
echo Building BetterThrowingSystem Mod...

REM Set your game path here
set DUCKOV_PATH=D:\SteamLibrary\steamapps\common\Escape from Duckov

REM Build the project with Release configuration and DuckovPath parameter
dotnet build BetterThrowingSystem.csproj -c Release /p:DuckovPath="%DUCKOV_PATH%"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Copying DLL...
    
    REM Copy the DLL to the mod folder
    copy /Y bin\Release\netstandard2.1\BetterThrowingSystem.dll .\ 2>nul
    
    if %ERRORLEVEL% EQU 0 (
        echo DLL copied successfully!
    ) else (
        echo Warning: Failed to copy DLL. Please copy manually from bin\Release\netstandard2.1\
    )
    
    echo.
    echo Build and copy done!
) else (
    echo.
    echo Build failed! Please check the error messages above.
    exit /b 1
)

pause

