# Cache breaker para forzar la lectura del nuevo Program.cs - Parcial 2026
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY . ./

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:10000

EXPOSE 10000

# Elimina el comando 'dotnet ef database update'
ENTRYPOINT ["dotnet", "PlataformaCreditos.dll"]