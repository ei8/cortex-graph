FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY ei8-Cortex-Graph-Master.sln ./
COPY main/Port.Adapter/Out/Api/Out.Api.csproj main/Port.Adapter/Out/Api/
COPY main/Application/Application.csproj main/Application/
COPY main/Domain.Model/Domain.Model.csproj main/Domain.Model/
COPY main/Port.Adapter/IO/Persistence/ArangoDB/ArangoDB.csproj main/Port.Adapter/IO/Persistence/ArangoDB/
COPY main/Port.Adapter/Common/Common.csproj main/Port.Adapter/Common/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/main/Port.Adapter/Out/Api
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ei8.Cortex.Graph.Port.Adapter.Out.Api.dll"]
