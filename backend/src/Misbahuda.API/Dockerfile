FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Misbahuda.API/Misbahuda.API.csproj", "src/Misbahuda.API/"]
COPY ["src/Misbahuda.Application/Misbahuda.Application.csproj", "src/Misbahuda.Application/"]
COPY ["src/Misbahuda.Domain/Misbahuda.Domain.csproj", "src/Misbahuda.Domain/"]
COPY ["src/Misbahuda.Infrastructure/Misbahuda.Infrastructure.csproj", "src/Misbahuda.Infrastructure/"]

RUN dotnet restore "src/Misbahuda.API/Misbahuda.API.csproj"

COPY . .

RUN dotnet build "src/Misbahuda.API/Misbahuda.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Misbahuda.API/Misbahuda.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Misbahuda.API.dll"]
