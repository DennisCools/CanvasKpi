tag v0.0.3

# Canvas Kpi Tool Ovp Open
Dotnetcore MVC webapp met ms sql database.


## Tools
https://json2csharp.com/



## Canvas Lti Link Menu
### Create menulink in course
curl http://localhost:3000/api/v1/accounts/1/external_tools \
-X POST -H 'Authorization: Bearer canvas-docker' \
-F 'name=CanvasLtiApiDemoLocal' \
-F 'consumer_key=some_key' \
-F "shared_secret=some_secret" \
-F "url=https://localhost:8001/LtiLaunch/LtiCourseLoginAndOauth2" \
-F "privacy_level=public" \
-F "custom_fields[domain]=\$Canvas.api.domain" \
-F "custom_fields[Canvas_course_name]=\$Canvas.course.name" \
-F "course_navigation[text]=CanvasLtiApiDemoLocal" \
-F "course_navigation[default]=true" \
-F "course_navigation[enabled]=true" 


## docker
docker pull mcr.microsoft.com/mssql/server
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=yourStrong(!)Password" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

"CanvasKpiLtiContext": "Server=127.0.0.1,1433;Database=kpiDb;User ID=sa;Password=MyPass@word;TrustServerCertificate=True;MultiSubnetFailover=True;MultipleActiveResultSets=true"

on mac
docker run -e "ACCEPT_EULA=1" -e "MSSQL_SA_PASSWORD=MyPass@word" -e "MSSQL_PID=Developer" -e "MSSQL_USER=SA" -p 1433:1433 -d --name=sql mcr.microsoft.com/azure-sql-edge
https://learn.microsoft.com/en-us/sql/azure-data-studio/download-azure-data-studio?view=sql-server-ver16
create first table with azure-data-studio if using rider database tool.

om mac M4 pro
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@word" -e "MSSQL_PID=Evaluation" -p 1433:1433  --name sqlpreview --hostname sqlpreview -d mcr.microsoft.com/mssql/server:latest


# database  
https://learn.microsoft.com/en-us/ef/core/cli/dotnet

dotnet tool install --global dotnet-ef  / dotnet tool update --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design

dotnet ef migrations add Outcome
dotnet ef database update

cd <<ProjectMap>>/src/
dotnet ef migrations add <<MigrationName>>> --project CompetenceProfilingInfrastructure -s CanvasKpiLti -c DatabaseContext --verbose
dotnet ef database update --project CompetenceProfilingInfrastructure -s CanvasKpiLti -c DatabaseContext --verbose

dotnet ef migrations add --project src/CompetenceProfilingInfrastructure/CompetenceProfilingInfrastructure.csproj --startup-project src/CanvasKpiLti/CanvasKpiLti.csproj --context CompetenceProfilingInfrastructure.Data.DatabaseContext --configuration Debug --verbose WebCacheTable --output-dir Migrations

# Caching
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-7.0
// https://dejanstojanovic.net/aspnet/2018/may/using-idistributedcache-in-net-core-just-got-a-lot-easier/
dotnet sql-cache create "Server=127.0.0.1,1433;Database=kpiCpDb;User ID=sa;Password=MyPass@word;TrustServerCertificate=True;MultiSubnetFailover=True;MultipleActiveResultSets=true" dbo WebCache

dotnet sql-cache create "Server=(localdb)\MSSQLLocalDB;Database=kpiDb;TrustServerCertificate=True;MultiSubnetFailover=True;MultipleActiveResultSets=true" dbo WebCache

## uml


![Class Diagram](http://www.plantuml.com/plantuml/proxy?src=https://raw.githubusercontent.com/DennisCools/CanvasKpi/main/uml.puml)

info
https://plantuml.com
https://github.com/Zingam/UML-in-Markdown/blob/master/README.md
https://blog.jetbrains.com/dotnet/2020/10/06/create-uml-diagrams-using-plantuml/

## https in Docker
https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-9.0#windows-using-linux-containers

