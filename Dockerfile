# 1. Derleme Aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala ve restore et
COPY ["src/GuvenleAlSat.API/GuvenleAlSat.API.csproj", "src/GuvenleAlSat.API/"]
COPY ["src/GuvenleAlSat.Business/GuvenleAlSat.Business.csproj", "src/GuvenleAlSat.Business/"]
COPY ["src/GuvenleAlSat.DataAccess/GuvenleAlSat.DataAccess.csproj", "src/GuvenleAlSat.DataAccess/"]
COPY ["src/GuvenleAlSat.Core/GuvenleAlSat.Core.csproj", "src/GuvenleAlSat.Core/"]

RUN dotnet restore "src/GuvenleAlSat.API/GuvenleAlSat.API.csproj"

# Kaynak kodları kopyala ve derle
COPY . .
WORKDIR "/src/src/GuvenleAlSat.API"
RUN dotnet publish "GuvenleAlSat.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Çalışma (Runtime) Aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Linux container'larda SIGSEGV (Status 139) çökmesini engelleyen kritik bayraklar:
ENV DOTNET_EnableWriteXorExecute=0
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "GuvenleAlSat.API.dll"]