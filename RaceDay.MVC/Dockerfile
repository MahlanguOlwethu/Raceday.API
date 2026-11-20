# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until the .csproj changes
COPY RaceDay.MVC/RaceDay.MVC.csproj RaceDay.MVC/
RUN dotnet restore RaceDay.MVC/RaceDay.MVC.csproj

COPY RaceDay.MVC/ RaceDay.MVC/
RUN dotnet publish RaceDay.MVC/RaceDay.MVC.csproj -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Run as the non-root user built into the aspnet image
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.MVC.dll"]
