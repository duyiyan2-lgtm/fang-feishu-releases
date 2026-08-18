FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/FangFeishu.Backend.sln backend/
COPY backend/src/FangFeishu.Api/FangFeishu.Api.csproj backend/src/FangFeishu.Api/
COPY backend/tests/FangFeishu.Api.Tests/FangFeishu.Api.Tests.csproj backend/tests/FangFeishu.Api.Tests/
RUN dotnet restore backend/FangFeishu.Backend.sln

COPY backend/ backend/
RUN dotnet publish backend/src/FangFeishu.Api/FangFeishu.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FangFeishu.Api.dll"]
