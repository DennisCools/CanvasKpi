#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/CanvasIdentity/CanvasIdentity.csproj", "src/CanvasIdentity/"]
COPY ["src/CompetenceProfilingDomain/CompetenceProfilingDomain.csproj", "src/CompetenceProfilingDomain/"]
COPY ["src/CompetenceProfilingInfrastructure/CompetenceProfilingInfrastructure.csproj", "src/CompetenceProfilingInfrastructure/"]
COPY ["src/CanvasKpiLti/CanvasKpiLti.csproj", "src/CanvasKpiLti/"]
RUN dotnet restore "src/CanvasKpiLti/CanvasKpiLti.csproj"
COPY . .

WORKDIR "/src/src/CanvasKpiLti"
RUN dotnet build "CanvasKpiLti.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CanvasKpiLti.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasKpiLti.dll"]