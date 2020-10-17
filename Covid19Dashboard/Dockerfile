#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Covid19Dashboard/Covid19Dashboard.csproj", "Covid19Dashboard/"]
RUN dotnet restore "Covid19Dashboard/Covid19Dashboard.csproj"
COPY . .
WORKDIR "/src/Covid19Dashboard"
RUN dotnet build "Covid19Dashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Covid19Dashboard.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Covid19Dashboard.dll"]