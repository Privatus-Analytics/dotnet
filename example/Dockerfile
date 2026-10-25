# Conformance example app (ASP.NET Core Razor Pages). Build with the package
# folder as the context:
#   docker build -f example/Dockerfile .
# The build runs on linux/amd64: on newer Apple chips the arm64 .NET SDK tools crash at random
# ("Illegal instruction"). The output is plain .NET code, so the app itself runs on any chip.
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:9.0 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_EnableArm64Sve=0
WORKDIR /src
# The package from this repo's own source (a ProjectReference), not from NuGet.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/ src/
COPY example/ example/
RUN dotnet publish example/Example.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
ENV DOTNET_EnableArm64Sve=0 ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "Example.dll"]
