FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MetamaskExperiments/MetamaskExperiments.csproj", "MetamaskExperiments/"]
RUN dotnet restore "MetamaskExperiments/MetamaskExperiments.csproj"
COPY . .
WORKDIR "/src/MetamaskExperiments"
RUN dotnet build "MetamaskExperiments.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MetamaskExperiments.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MetamaskExperiments.dll"]