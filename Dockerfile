FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5432

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MyStore/MyStore.csproj", "MyStore/"]
RUN dotnet restore "MyStore/MyStore.csproj"
COPY . .
WORKDIR "/src/MyStore"
RUN dotnet build "MyStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyStore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyStore.dll"]