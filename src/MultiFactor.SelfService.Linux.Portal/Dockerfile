#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt-get update && apt-get install -y libldap-2.4-2
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MultiFactor.SelfService.Linux.Portal/MultiFactor.SelfService.Linux.Portal.csproj", "MultiFactor.SelfService.Linux.Portal/"]
RUN dotnet restore "MultiFactor.SelfService.Linux.Portal/MultiFactor.SelfService.Linux.Portal.csproj"
COPY . .
WORKDIR "/src/MultiFactor.SelfService.Linux.Portal"
RUN dotnet build "MultiFactor.SelfService.Linux.Portal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MultiFactor.SelfService.Linux.Portal.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MultiFactor.SelfService.Linux.Portal.dll"]