FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Ayoos.sln", "./"]
COPY ["src/Ayoos.Api/Ayoos.Api.csproj", "src/Ayoos.Api/"]
COPY ["src/Ayoos.Application/Ayoos.Application.csproj", "src/Ayoos.Application/"]
COPY ["src/Ayoos.Domain/Ayoos.Domain.csproj", "src/Ayoos.Domain/"]
COPY ["src/Ayoos.Infrastructure/Ayoos.Infrastructure.csproj", "src/Ayoos.Infrastructure/"]
RUN dotnet restore "src/Ayoos.Api/Ayoos.Api.csproj"

COPY . .
WORKDIR /src/src/Ayoos.Api
RUN dotnet publish "Ayoos.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Ayoos.Api.dll"]
