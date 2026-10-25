#!/bin/sh
# Runs the format check, tests (on .NET 8 and .NET 9) and dotnet pack in the official .NET SDK image.
# Usage: ./run-docker.sh
# It uses the linux/amd64 image by default. On newer Apple chips the arm64 .NET 9 tools crash at random
# ("Illegal instruction", exit code 132), so emulation is slower but steady. To try native arm64 anyway:
#   DOCKER_PLATFORM=linux/arm64 ./run-docker.sh
# DOTNET_EnableArm64Sve=0 makes those crashes rarer on arm64. It does nothing on Intel.
set -e
docker run --rm -v "$PWD":/src -w /src \
  -v privatus-dotnet-nuget:/root/.nuget/packages \
  -e DOTNET_CLI_TELEMETRY_OPTOUT=1 -e DOTNET_NOLOGO=1 \
  -e DOTNET_EnableArm64Sve=0 \
  --platform "${DOCKER_PLATFORM:-linux/amd64}" \
  mcr.microsoft.com/dotnet/sdk:9.0 sh -c '
  set -e
  # The SDK 9 image only has the .NET 9 runtime. Add the ASP.NET Core 8 runtime for the net8.0 tests.
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --runtime aspnetcore --install-dir /usr/share/dotnet >/dev/null
  dotnet restore Privatus.Analytics.AspNetCore.sln
  # Layout and code style check (.editorconfig). Style rules are also errors in the build (EnforceCodeStyleInBuild).
  dotnet format Privatus.Analytics.AspNetCore.sln --verify-no-changes --no-restore
  dotnet build Privatus.Analytics.AspNetCore.sln -c Release --no-restore
  dotnet test Privatus.Analytics.AspNetCore.sln -c Release --no-build
  dotnet pack src/Privatus.Analytics.AspNetCore/Privatus.Analytics.AspNetCore.csproj -c Release --no-build -o artifacts
  ls -l artifacts
'
