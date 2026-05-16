#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
#copio il progetto
WORKDIR /src
COPY ["backend/api/JanaApi.csproj", "."]
RUN dotnet restore "./JanaApi.csproj" 
COPY "backend/api" .
WORKDIR "/src/."
RUN dotnet build "./JanaApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./JanaApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM node:22-alpine AS node-builder
WORKDIR /frontend
COPY ["frontend/", "/frontend"]
RUN npm install
RUN npm run build


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#frontend files
COPY --from=node-builder /frontend/dist ./wwwroot/
#COPY "favicon.ico" ./wwwroot/favicon.ico
ENTRYPOINT ["dotnet", "JanaApi.dll"]