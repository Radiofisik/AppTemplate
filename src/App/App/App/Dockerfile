FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["App/App/App.csproj", "App/App/"]
COPY ["App/Api/Api.csproj", "App/Api/"]
COPY ["Infrastructure/Infrastructure/Infrastructure.csproj", "Infrastructure/Infrastructure/"]
COPY ["App/Services/Services.csproj", "App/Services/"]
COPY ["App/Events/Events.csproj", "App/Events/"]
COPY ["App/Dtos/Dtos.csproj", "App/Dtos/"]
COPY ["Infrastructure/Interceptor/Interceptor.csproj", "Infrastructure/Interceptor/"]
COPY ["App/Handlers/Handlers.csproj", "App/Handlers/"]
RUN dotnet restore "App/App/App.csproj"
COPY . .
WORKDIR "/src/App/App"
RUN dotnet build "App.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "App.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "App.dll"]