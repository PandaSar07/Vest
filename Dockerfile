# Vest — multi-stage: dashboard (Node) + ASP.NET Core 8 (Render uses Docker for .NET)
FROM node:20-bookworm-slim AS dashboard
WORKDIR /src/frontend/dashboard
COPY frontend/dashboard/package.json frontend/dashboard/package-lock.json ./
RUN npm ci
COPY frontend/dashboard/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
COPY --from=dashboard /src/wwwroot/dashboard ./wwwroot/dashboard
RUN dotnet publish Vest.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Render sets PORT; Program.cs binds to it. Default 8080 matches ASP.NET container images.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Vest.dll"]
