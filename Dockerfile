# API de TitanGlobal para Render. La cadena de conexión llega por la variable
# de entorno ConnectionStrings__DefaultConnection (nunca en la imagen).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/TitanGlobal.Api/TitanGlobal.Api.csproj src/TitanGlobal.Api/
RUN dotnet restore src/TitanGlobal.Api/TitanGlobal.Api.csproj
COPY src/ src/
RUN dotnet publish src/TitanGlobal.Api/TitanGlobal.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "TitanGlobal.Api.dll"]
