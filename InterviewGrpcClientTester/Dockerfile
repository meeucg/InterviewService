FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InterviewService/Directory.Packages.props", "InterviewService/"]
COPY ["InterviewService/NuGet.config", "InterviewService/"]
COPY ["InterviewService/InterviewGrpcClientTester/InterviewGrpcClientTester.csproj", "InterviewService/InterviewGrpcClientTester/"]
COPY ["InterviewService/InterviewGrpcContracts/InterviewGrpcContracts.csproj", "InterviewService/InterviewGrpcContracts/"]
COPY ["InterviewService/InterviewService.Core/InterviewService.Core.csproj", "InterviewService/InterviewService.Core/"]
COPY ["InterviewService/InterviewService.Application/InterviewService.Application.csproj", "InterviewService/InterviewService.Application/"]
COPY ["AIServices/AIServices/bin/Debug/", "AIServices/AIServices/bin/Debug/"]
RUN dotnet restore "InterviewService/InterviewGrpcClientTester/InterviewGrpcClientTester.csproj" --configfile "InterviewService/NuGet.config"
COPY . .
WORKDIR "/src/InterviewService/InterviewGrpcClientTester"
RUN dotnet build "InterviewGrpcClientTester.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InterviewGrpcClientTester.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InterviewGrpcClientTester.dll"]
