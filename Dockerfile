FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Proje dosyalarını kopyala ve restore et
COPY ["src/GuvenleAlSat.API/GuvenleAlSat.API.csproj", "src/GuvenleAlSat.API/"]
COPY ["src/GuvenleAlSat.Business/GuvenleAlSat.Business.csproj", "src/GuvenleAlSat.Business/"]
COPY ["src/GuvenleAlSat.DataAccess/GuvenleAlSat.DataAccess.csproj", "src/GuvenleAlSat.DataAccess/"]
COPY ["src/GuvenleAlSat.Core/GuvenleAlSat.Core.csproj", "src/GuvenleAlSat.Core/"]

RUN dotnet restore "src/GuvenleAlSat.API/GuvenleAlSat.API.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY . .
WORKDIR "/src/src/GuvenleAlSat.API"
RUN dotnet publish "GuvenleAlSat.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime imajı
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "GuvenleAlSat.API.dll"]