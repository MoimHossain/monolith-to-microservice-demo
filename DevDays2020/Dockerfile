FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["DevDays2020.csproj", ""]
RUN dotnet restore "./DevDays2020.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "DevDays2020.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DevDays2020.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DevDays2020.dll"]