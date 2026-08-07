# См. статью по ссылке https://aka.ms/customizecontainer, чтобы узнать как настроить контейнер отладки и как Visual Studio использует этот Dockerfile для создания образов для ускорения отладки.

# Этот этап используется при запуске из VS в быстром режиме (по умолчанию для конфигурации отладки)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Этот этап используется для сборки проекта службы
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Users/Users.Api/Users.Api.csproj", "Services/Users/Users.Api/"]
COPY ["Services/Users/Users.Application/Users.Application.csproj", "Services/Users/Users.Application/"]
COPY ["Services/Users/Users.Domain/Users.Domain.csproj", "Services/Users/Users.Domain/"]
COPY ["Shared/CoreEvents.Shared.Contracts/CoreEvents.Shared.Contracts.csproj", "Shared/CoreEvents.Shared.Contracts/"]
COPY ["Services/Users/Users.Infrastructure/Users.Infrastructure.csproj", "Services/Users/Users.Infrastructure/"]
RUN dotnet restore "./Services/Users/Users.Api/Users.Api.csproj"
COPY . .
WORKDIR "/src/Services/Users/Users.Api"
RUN dotnet build "./Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Users.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Users.Api.dll"]