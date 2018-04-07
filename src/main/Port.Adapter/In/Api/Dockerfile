FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY ei8-Cortex-Graph-Master.sln ./
COPY main/Port.Adapter/In/Api/In.Api.csproj main/Port.Adapter/In/Api/
COPY main/Application/Application.csproj main/Application/
COPY main/Domain.Model/Domain.Model.csproj main/Domain.Model/
COPY main/Port.Adapter/IO/Persistence/ArangoDB/ArangoDB.csproj main/Port.Adapter/IO/Persistence/ArangoDB/
COPY main/Port.Adapter/IO/Process/Events/GetEventStore/GetEventStore.csproj main/Port.Adapter/IO/Process/Events/GetEventStore/
COPY main/Port.Adapter/IO/Process/Events/Events.csproj main/Port.Adapter/IO/Process/Events/
COPY main/Port.Adapter/Common/Common.csproj main/Port.Adapter/Common/
COPY main/Port.Adapter/IO/Process/Events/Standard/Standard.csproj main/Port.Adapter/IO/Process/Events/Standard/
RUN dotnet restore -nowarn:msb3202,nu1503
COPY . .
WORKDIR /src/main/Port.Adapter/In/Api
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "In.Api.dll"]
