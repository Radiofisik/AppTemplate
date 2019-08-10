FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["Scheduler/Scheduler.App/Scheduler.App.csproj", "Scheduler/Scheduler.App/"]
COPY ["Scheduler/Scheduler.Services/Scheduler.Services.csproj", "Scheduler/Scheduler.Services/"]
COPY ["Infrastructure/Interceptor/Interceptor.csproj", "Infrastructure/Interceptor/"]
COPY ["Infrastructure/Infrastructure/Infrastructure.csproj", "Infrastructure/Infrastructure/"]
COPY ["Scheduler/Scheduler.Commands/Scheduler.Commands.csproj", "Scheduler/Scheduler.Commands/"]
COPY ["Scheduler/Scheduler.Handlers/Scheduler.Handlers.csproj", "Scheduler/Scheduler.Handlers/"]
RUN dotnet restore "Scheduler/Scheduler.App/Scheduler.App.csproj"
COPY . .
WORKDIR "/src/Scheduler/Scheduler.App"
RUN dotnet build "Scheduler.App.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Scheduler.App.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Scheduler.App.dll"]