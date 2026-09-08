@echo off
setlocal

pushd "%~dp0\.." >nul
python "%~dp0count_code_stats.py" %*
set EXIT_CODE=%ERRORLEVEL%
popd >nul

echo.
pause
exit /b %EXIT_CODE%
