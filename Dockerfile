# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Erp.Api.csproj ./
RUN dotnet restore ./Erp.Api.csproj

COPY . ./
RUN dotnet publish ./Erp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Erp.Api.dll"]
