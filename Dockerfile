# 1. Derleme (Build) Aşaması - .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Proje dosyalarını kopyala
COPY ["src/GuvenleAlSat.API/GuvenleAlSat.API.csproj", "src/GuvenleAlSat.API/"]
COPY ["src/GuvenleAlSat.Business/GuvenleAlSat.Business.csproj", "src/GuvenleAlSat.Business/"]
COPY ["src/GuvenleAlSat.DataAccess/GuvenleAlSat.DataAccess.csproj", "src/GuvenleAlSat.DataAccess/"]
COPY ["src/GuvenleAlSat.Core/GuvenleAlSat.Core.csproj", "src/GuvenleAlSat.Core/"]

# Bağımlılıkları geri yükle
RUN dotnet restore "src/GuvenleAlSat.API/GuvenleAlSat.API.csproj"

# Tüm kodları kopyala ve Release olarak derle
COPY . .
WORKDIR "/src/src/GuvenleAlSat.API"
RUN dotnet publish "GuvenleAlSat.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Çalıştırma (Runtime) Aşaması - .NET 10 ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

# Linux Container (Render Free Tier) Status 139 segfault önleme bayrakları:
ENV DOTNET_EnableWriteXorExecute=0
ENV DOTNET_gcServer=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GuvenleAlSat.API.dll"]