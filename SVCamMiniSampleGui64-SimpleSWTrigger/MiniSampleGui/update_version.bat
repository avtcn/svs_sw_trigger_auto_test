@echo off
setlocal
cd /d "%~dp0"

for /f %%i in ('git rev-parse --short HEAD 2^>nul') do set GIT_REV=%%i

if "%GIT_REV%"=="" (
    if not exist version_git.h (
        echo #pragma once > version_git.h
        echo #ifndef GIT_COMMIT_ID >> version_git.h
        echo #define GIT_COMMIT_ID "unknown" >> version_git.h
        echo #endif >> version_git.h
    )
    exit /b 0
)

(
echo #pragma once
echo #ifndef GIT_COMMIT_ID
echo #define GIT_COMMIT_ID "%GIT_REV%"
echo #endif
) > version_git.h.tmp

if exist version_git.h (
    fc version_git.h.tmp version_git.h >nul 2>&1
    if errorlevel 1 (
        move /y version_git.h.tmp version_git.h >nul
    ) else (
        del version_git.h.tmp >nul
    )
) else (
    move /y version_git.h.tmp version_git.h >nul
)

exit /b 0
