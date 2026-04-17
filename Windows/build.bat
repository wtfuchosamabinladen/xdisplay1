@echo off
echo ============================================
echo   XDisplay - Building Windows Server
echo ============================================
echo.

REM Try modern dotnet CLI first
where dotnet >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo Using dotnet CLI...
    dotnet build XDisplay.csproj -c Release -o bin\Release
    IF %ERRORLEVEL% EQU 0 (
        echo.
        echo Build successful!
        echo Run: bin\Release\XDisplay.exe
        goto end
    )
)

REM Fall back to built-in .NET Framework CSC compiler
echo Trying built-in .NET Framework compiler...
SET CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
IF NOT EXIST "%CSC%" SET CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe

IF NOT EXIST "%CSC%" (
    echo ERROR: .NET Framework compiler not found.
    echo Please install Visual Studio or .NET SDK from:
    echo https://dotnet.microsoft.com/download
    goto end
)

"%CSC%" /target:winexe /out:XDisplay.exe ScreenServer.cs ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Net.dll

IF %ERRORLEVEL% EQU 0 (
    echo.
    echo Build successful! Run XDisplay.exe
) ELSE (
    echo Build failed. Try opening XDisplay.csproj in Visual Studio instead.
)

:end
echo.
pause
