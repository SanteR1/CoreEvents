# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Переключаемся на root для выдачи прав на запись логов
USER root

# Создаем директорию логов и делаем пользователя app ее владельцем
RUN mkdir -p /var/log/gateway-api \
    && chown -R app:app /var/log/gateway-api

# Возвращаемся к безопасному пользователю для работы приложения
USER app

# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gateway/Gateway.Api/Gateway.Api.csproj", "Gateway/Gateway.Api/"]
RUN dotnet restore "./Gateway/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/Gateway/Gateway.Api"
RUN dotnet build "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
