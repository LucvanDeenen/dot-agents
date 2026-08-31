# API image. Built cross-arch: the SDK stage runs on the build host's native
# architecture and cross-compiles for the target (fast arm64 builds on amd64
# CI runners, no QEMU for the compiler).
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY . .
# -a implies the linux-$TARGETARCH runtime identifier; restore is included so
# RID-specific assets resolve correctly.
RUN dotnet publish api/AgentPlatform.Api.csproj -c Release -a $TARGETARCH -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Talks to the host Docker engine through the mounted socket to spawn
# agent-runner containers — hence no USER downgrade here.
EXPOSE 8080
ENTRYPOINT ["dotnet", "AgentPlatform.Api.dll"]
