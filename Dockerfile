FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY server/RecBack.Server/RecBack.Server.csproj .
RUN dotnet restore -r linux-arm64
COPY server/RecBack.Server/ .
RUN dotnet publish -c Release -o /app -r linux-arm64 --self-contained false --no-restore

FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 9999 2018 20182 20161
VOLUME ["/app/data"]
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT ["dotnet", "/app/RecBack.Server.dll"]