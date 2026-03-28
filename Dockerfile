# ASP.NET Core — .NET 10 (matches ThesisWebApp.csproj TargetFramework)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["ThesisWebApp/ThesisWebApp.csproj", "ThesisWebApp/"]
RUN dotnet restore "ThesisWebApp/ThesisWebApp.csproj"

COPY ThesisWebApp/ ThesisWebApp/
WORKDIR /src/ThesisWebApp
RUN dotnet publish "ThesisWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

# Local: 8080. Render.com sets PORT — listen on that when present.
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
CMD ["/bin/sh", "-c", "exec dotnet ThesisWebApp.dll --urls \"http://0.0.0.0:${PORT:-8080}\""]
