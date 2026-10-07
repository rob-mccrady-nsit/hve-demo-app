---
title: Stockroom for .NET
description: Build and run the ASP.NET Core Stockroom application
---

# Stockroom for .NET

This is the ASP.NET Core reimplementation of the Stockroom inventory and sales workshop app. The Python application and workshop materials at the repository root remain unchanged.

## Requirements

* .NET 10 SDK

## Run locally

From this directory:

```powershell
dotnet restore Stockroom.sln
dotnet run --project Stockroom.csproj --urls http://localhost:5080
```

Open <http://localhost:5080>. By default the app creates `inventory.db` in this directory. Set the `DATABASE` environment variable to use another database path.

## Test

```powershell
dotnet test Stockroom.sln
```

Tests use temporary SQLite files and do not modify the running app's database.

## Run with Docker Compose

From this directory:

```powershell
docker compose up --build
```

Open <http://localhost:5080>. Data persists in the `stockroom-data` volume. Set `APP_PORT` to change the host port.