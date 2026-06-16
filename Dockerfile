FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-arm64v8 AS build
WORKDIR /src
COPY server/RecBack.Server/RecBack.Server.csproj .
RUN dotnet restore
COPY server/RecBack.Server/ .
RUN dotnet publish -c Release -o /app -r linux-arm64 --self-contained false

FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim-arm64v8
WORKDIR /app
COPY --from=build /app .
EXPOSE 9999 2018 20182 20161
VOLUME ["/app/data"]
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT ["dotnet", "/app/RecBack.Server.dll"]
