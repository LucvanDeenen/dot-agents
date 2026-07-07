FROM mcr.microsoft.com/dotnet/sdk:8.0-noble-aot AS build
WORKDIR /src
COPY AgentPlatform.Api.csproj .
RUN dotnet restore
COPY . .
# building on the Pi itself, so the arm64 RID is picked up automatically —
# add --arch arm64 explicitly if you ever build this cross-platform (e.g. CI on x86)
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled-aot AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["./AgentPlatform.Api"]
