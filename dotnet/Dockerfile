FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Stockroom.csproj ./
RUN dotnet restore Stockroom.csproj

COPY . ./
RUN dotnet publish Stockroom.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p /data && chown $APP_UID:$APP_UID /data
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DATABASE=/data/inventory.db
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Stockroom.dll"]