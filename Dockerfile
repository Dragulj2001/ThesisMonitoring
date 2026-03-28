# Build faza
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiramo .csproj fajl direktno (jer je on u root-u na GitHub-u)
COPY ["ThesisWebApp.csproj", "./"]
RUN dotnet restore "ThesisWebApp.csproj"

# Kopiramo sav ostali kod
COPY . .

# Publish aplikacije
RUN dotnet publish "ThesisWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime faza (Finalna slika)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render podešavanja
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ThesisWebApp.dll"]