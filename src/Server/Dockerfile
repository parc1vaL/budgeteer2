FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS restore
WORKDIR /src
COPY ["src/Server/Budgeteer.Server.csproj", "Server/"]
COPY ["src/Blazor/Budgeteer.Blazor.csproj", "Blazor/"]
COPY ["src/Shared/Budgeteer.Shared.csproj", "Shared/"]
RUN dotnet restore "Server/Budgeteer.Server.csproj"

FROM node:20-alpine AS node
WORKDIR /src
COPY ["src/Blazor", "."]
RUN npx tailwindcss -i app.css -o /out/app.css

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY --from=node /out/app.css Blazor/wwwroot/css/app.css
COPY src .

WORKDIR "/src/Server"
RUN dotnet build "Budgeteer.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Budgeteer.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Budgeteer.Server.dll"]
