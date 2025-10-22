FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src

COPY ["agrirouter-rest-api/agrirouter-rest-api.csproj", "agrirouter-rest-api/"]
COPY ["agrirouter-sdk-dotnet-standard-impl/agrirouter-sdk-dotnet-standard-impl.csproj", "agrirouter-sdk-dotnet-standard-impl/"]
COPY ["agrirouter-sdk-dotnet-standard-api/agrirouter-sdk-dotnet-standard-api.csproj", "agrirouter-sdk-dotnet-standard-api/"]

RUN dotnet restore "agrirouter-rest-api/agrirouter-rest-api.csproj"

COPY . .

WORKDIR "/src/agrirouter-rest-api"
RUN dotnet build "agrirouter-rest-api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "agrirouter-rest-api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS final
WORKDIR /app
EXPOSE 5000

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "agrirouter-rest-api.dll"]
