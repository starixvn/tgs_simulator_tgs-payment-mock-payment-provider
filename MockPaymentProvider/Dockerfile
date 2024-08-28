
FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MockPaymentProvider/MockPaymentProvider.csproj", "MockPaymentProvider/"]
RUN dotnet restore "./MockPaymentProvider/MockPaymentProvider.csproj"
COPY . .
WORKDIR "/src/MockPaymentProvider"
RUN dotnet build "./MockPaymentProvider.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MockPaymentProvider.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MockPaymentProvider.dll"]