FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Titan.Core/Titan.Core.csproj Titan.Core/
COPY Titan.Engine/Titan.Engine.csproj Titan.Engine/
COPY Titan.Gateway/Titan.Gateway.csproj Titan.Gateway/
RUN dotnet restore Titan.Gateway/Titan.Gateway.csproj

COPY Titan.Core/ Titan.Core/
COPY Titan.Engine/ Titan.Engine/
COPY Titan.Gateway/ Titan.Gateway/
RUN dotnet publish Titan.Gateway/Titan.Gateway.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Titan.Gateway.dll"]
