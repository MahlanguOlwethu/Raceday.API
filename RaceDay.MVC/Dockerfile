# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RaceDay.MVC/RaceDay.MVC.csproj", "RaceDay.MVC/"]
COPY ["RaceDay.API/RaceDay.API.csproj", "RaceDay.API/"]
RUN dotnet restore "RaceDay.MVC/RaceDay.MVC.csproj"
COPY . .
WORKDIR "/src/RaceDay.MVC"
RUN dotnet build "RaceDay.MVC.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RaceDay.MVC.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]