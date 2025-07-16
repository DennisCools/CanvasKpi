#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# create map in container /sourcecode
WORKDIR /sourcecode
RUN echo "Debugging files at $(date):" && ls -R

COPY ["./src/CanvasIdentity/CanvasIdentity.csproj", "src/CanvasIdentity/"]
COPY ["./src/CompetenceProfilingDomain/CompetenceProfilingDomain.csproj", "src/CompetenceProfilingDomain/"]
COPY ["./src/CompetenceProfilingInfrastructure/CompetenceProfilingInfrastructure.csproj", "src/CompetenceProfilingInfrastructure/"]
RUN echo "Debugging files at $(date):" && ls -R
COPY ["./src/CanvasKpiLti/CanvasKpiLti.csproj", "src/CanvasKpiLti/"]

RUN echo "Debugging files at $(date):" && ls -R
# run dotnet restore in container map /sourcecode
RUN dotnet restore "src/CanvasKpiLti/CanvasKpiLti.csproj"
# copy locale map CanvasKpi to container map sourcecode
# /sourcecode
#       +src
#           +CanvasKpiLti
#           +CanvasIdentity
#           +...
COPY . .

WORKDIR "/sourcecode/src/CanvasKpiLti"
RUN dotnet build "CanvasKpiLti.csproj" -c Release -o /app/build
# make an new images(publish) from image build (dotnet sdk images)
FROM build AS publish
RUN dotnet publish "CanvasKpiLti.csproj" -c Release -o /app/publish

# make an new images(final) from image base (dotnet runtime images) 
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasKpiLti.dll"]
# ENTRYPOINT ["tail", "-f", "/dev/null"]
