FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5263

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["LaundrySignalR.csproj", "./"]
RUN dotnet restore "LaundrySignalR.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "LaundrySignalR.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "LaundrySignalR.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false $TARGETARCH

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LaundrySignalR.dll"]
