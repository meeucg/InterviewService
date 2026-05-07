FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["InterviewService.Api/InterviewService.Api.csproj", "InterviewService.Api/"]
COPY ["InterviewGrpcContracts/InterviewGrpcContracts.csproj", "InterviewGrpcContracts/"]
COPY ["InterviewService.Application/InterviewService.Application.csproj", "InterviewService.Application/"]
COPY ["InterviewService.Core/InterviewService.Core.csproj", "InterviewService.Core/"]
COPY ["InterviewService.Infrastructure/InterviewService.Infrastructure.csproj", "InterviewService.Infrastructure/"]
RUN dotnet restore "InterviewService.Api/InterviewService.Api.csproj"
COPY . .
WORKDIR "/src/InterviewService.Api"
RUN dotnet build "InterviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InterviewService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InterviewService.Api.dll"]

