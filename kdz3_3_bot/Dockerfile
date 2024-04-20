FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["kdz3_3_bot/kdz3_3_bot.csproj", "kdz3_3_bot/"]
COPY ["BotProcessing/BotProcessing.csproj", "BotProcessing/"]
RUN dotnet restore "kdz3_3_bot/kdz3_3_bot.csproj"
COPY . .
WORKDIR "/src/kdz3_3_bot"
RUN dotnet build "kdz3_3_bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "kdz3_3_bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "kdz3_3_bot.dll"]
