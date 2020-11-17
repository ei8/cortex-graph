FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
COPY *.sln ./
COPY . .
WORKDIR /src/main/Port.Adapter/In/Api
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app /property:PublishWithAspNetCoreTargetManifest=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ei8.Cortex.Graph.Port.Adapter.In.Api.dll"]
