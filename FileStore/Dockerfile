#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileStore/FileStore.csproj", "FileStore/"]
RUN dotnet restore "FileStore/FileStore.csproj"
COPY . .
WORKDIR "/src/FileStore"
RUN dotnet build "FileStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileStore.dll"]