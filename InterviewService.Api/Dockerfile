FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InterviewService/Directory.Packages.props", "InterviewService/"]
COPY ["InterviewService/NuGet.config", "InterviewService/"]
COPY ["InterviewService/InterviewService.Api/InterviewService.Api.csproj", "InterviewService/InterviewService.Api/"]
COPY ["InterviewService/InterviewGrpcContracts/InterviewGrpcContracts.csproj", "InterviewService/InterviewGrpcContracts/"]
COPY ["InterviewService/InterviewService.Application/InterviewService.Application.csproj", "InterviewService/InterviewService.Application/"]
COPY ["InterviewService/InterviewService.Core/InterviewService.Core.csproj", "InterviewService/InterviewService.Core/"]
COPY ["InterviewService/InterviewService.Infrastructure/InterviewService.Infrastructure.csproj", "InterviewService/InterviewService.Infrastructure/"]
COPY ["AIServices/AIServices/bin/Debug/", "AIServices/AIServices/bin/Debug/"]
RUN dotnet restore "InterviewService/InterviewService.Api/InterviewService.Api.csproj" --configfile "InterviewService/NuGet.config"
COPY . .
WORKDIR "/src/InterviewService/InterviewService.Api"
RUN dotnet build "InterviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InterviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InterviewService.Api.dll"]

