FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY EventosVivos.sln ./
COPY src/EventosVivos.Api/EventosVivos.Api.csproj src/EventosVivos.Api/
COPY src/EventosVivos.Application/EventosVivos.Application.csproj src/EventosVivos.Application/
COPY src/EventosVivos.Domain/EventosVivos.Domain.csproj src/EventosVivos.Domain/
COPY src/EventosVivos.Infrastructure/EventosVivos.Infrastructure.csproj src/EventosVivos.Infrastructure/

RUN dotnet restore EventosVivos.sln

COPY src/ src/

RUN dotnet publish src/EventosVivos.Api/EventosVivos.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EventosVivos.Api.dll"]
