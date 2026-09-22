@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

echo Building WinSelectionColor.exe...
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /win32manifest:app.manifest /out:WinSelectionColor.exe src\Program.cs src\ColorRegistry.cs src\WallpaperHelper.cs src\EyedropperForm.cs src\MainForm.cs

if errorlevel 1 (
    echo [ERROR] Compilation failed!
) else (
    echo [SUCCESS] WinSelectionColor.exe built successfully!
)