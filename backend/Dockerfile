FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["backend/RealEstateAI.Api.csproj", "backend/"]
RUN dotnet restore "backend/RealEstateAI.Api.csproj"

COPY backend/ backend/
RUN dotnet publish "backend/RealEstateAI.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "RealEstateAI.Api.dll"]
