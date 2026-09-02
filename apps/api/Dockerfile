FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY apps/api/PlanVest.Api.csproj apps/api/
RUN dotnet restore apps/api/PlanVest.Api.csproj

COPY apps/api/ apps/api/
RUN dotnet publish apps/api/PlanVest.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish ./
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlanVest.Api.dll"]
