#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
ENV TZ=Europe/Berlin
WORKDIR /app
EXPOSE 80
# Enable this to copy the json data files to the build
# COPY ["Covid19Dashboard/data/", "/app/data/"]

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Covid19Dashboard/Covid19Dashboard/Covid19Dashboard.csproj", "Covid19Dashboard/Covid19Dashboard/"]
RUN dotnet restore "Covid19Dashboard/Covid19Dashboard/Covid19Dashboard.csproj"
COPY . .
WORKDIR "/src/Covid19Dashboard/Covid19Dashboard/"
RUN dotnet build "Covid19Dashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Covid19Dashboard.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Covid19Dashboard.dll"]