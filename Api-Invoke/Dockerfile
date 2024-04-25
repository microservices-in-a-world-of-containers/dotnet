#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Api-Invoke/Api-Invoke.csproj", "Api-Invoke/"]
RUN dotnet restore "Api-Invoke/Api-Invoke.csproj"
COPY . .
WORKDIR "/src/Api-Invoke"
RUN dotnet build "Api-Invoke.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Api-Invoke.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api-Invoke.dll"]