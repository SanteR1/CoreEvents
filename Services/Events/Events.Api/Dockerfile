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
COPY ["Services/Events/Events.Api/Events.Api.csproj", "Services/Events/Events.Api/"]
COPY ["Services/Events/Events.Application/Events.Application.csproj", "Services/Events/Events.Application/"]
COPY ["Services/Events/Events.Domain/Events.Domain.csproj", "Services/Events/Events.Domain/"]
COPY ["Shared/CoreEvents.Shared.Contracts/CoreEvents.Shared.Contracts.csproj", "Shared/CoreEvents.Shared.Contracts/"]
COPY ["Services/Events/Events.Infrastructure/Events.Infrastructure.csproj", "Services/Events/Events.Infrastructure/"]
RUN dotnet restore "./Services/Events/Events.Api/Events.Api.csproj"
COPY . .
WORKDIR "/src/Services/Events/Events.Api"
RUN dotnet build "./Events.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Этот этап используется для публикации проекта службы, который будет скопирован на последний этап
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Events.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Этот этап используется в рабочей среде или при запуске из VS в обычном режиме (по умолчанию, когда конфигурация отладки не используется)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Events.Api.dll"]