FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["Auth/AuthServer/AuthServer.csproj", "Auth/AuthServer/"]
COPY ["Infrastructure/Infrastructure/Infrastructure.csproj", "Infrastructure/Infrastructure/"]
RUN dotnet restore "Auth/AuthServer/AuthServer.csproj"
COPY . .
WORKDIR "/src/Auth/AuthServer"
RUN dotnet build "AuthServer.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AuthServer.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AuthServer.dll"]