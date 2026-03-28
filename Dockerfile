# Build faza
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Kopiramo .csproj direktno (jer je on sada u root-u na GitHub-u)
COPY ["ThesisWebApp.csproj", "./"]
RUN dotnet restore "ThesisWebApp.csproj"

# Kopiramo sav ostali kod
COPY . .

# Publish aplikacije
RUN dotnet publish "ThesisWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Finalna faza (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render koristi PORT varijablu, ASP.NET treba da sluša na 0.0.0.0
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ThesisWebApp.dll"]