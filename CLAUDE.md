# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ShopEx is a shop back-office system: an ASP.NET Core (.NET 9, SDK pinned in `global.json`) Web API plus an Angular 19 standalone-component SPA in `Frontend/ShopEx`. It tracks supplier invoices/payments/returns, consignments with dealers, maintenance tickets, stock per warehouse, balances and expenses, and reads supplier/item/customer/worksheet data from an existing **RMS** (retail management system) database.

## Commands

Backend (run from repo root `E:\ShopEx`):
```bash
dotnet build ShopEx.sln
dotnet run --project ShopEx            # API on https://localhost:7122 (Swagger in Development)
dotnet ef migrations add <Name> --project ShopEx.Infrastructure --startup-project ShopEx --context ShopContext
dotnet ef database update --project ShopEx.Infrastructure --startup-project ShopEx --context ShopContext
```

Frontend (from `Frontend/ShopEx`):
```bash
npm start                  # ng serve
npm run build
npm test                   # Karma/Jasmine
npx ng test --include='**/foo.component.spec.ts'   # single spec
```

There are no backend test projects. `ShopEx.postman_collection.json` and `ShopEx/ShopEx.http` hold sample API requests. The `*.log` files in the root are stray build output.

## Architecture

Clean-architecture solution with four projects: `ShopEx` (controllers, `Program.cs` DI wiring) → `ShopEx.Application` (DTOs, service/repository interfaces, service implementations) → `ShopEx.Domain` (entities/enums); `ShopEx.Infrastructure` implements the repositories and EF contexts. DI registrations are all manual in `ShopEx/Program.cs`; add new repositories/services there.

**Two databases, two DbContexts** (connection strings in `appsettings.json`, SQL Server on `.` with Windows auth):
- `RMSContext` → `RMSHQ`: the pre-existing RMS database. Entities live in `ShopEx.Domain/RMSEntity` and `RMSEnum`; repos/services/interfaces are under `RMS` folders. Treat it as externally owned (no migrations here).
- `ShopContext` → `ShopEXdb`: the app's own database. Entities in `ShopEx.Domain/Entity`, EF configurations and **migrations** in `ShopEx.Infrastructure/Persistence/ShopDb`. `ShopDbSeeder.SeedAsync` runs at every startup.

ShopEx entities reference RMS records (suppliers, items, stores) by ID only, so there are no cross-database FKs; data from both sides is combined in the Application services. Repositories for the ShopEx side share an `IUnitOfWork`.

**Frontend**: standalone components in `src/app/pages/<feature>`, routes in `app.routes.ts`, one service per backend area in `src/app/services`, and all TS interfaces in `src/app/models/models.ts`. `ApiService` hardcodes `baseUrl = 'https://localhost:7122/api'` (check other services for the same constant when changing the API address). The API enables a permissive `AllowAll` CORS policy. Backend controller names map to API routes used by the services (e.g. `RMSSuppliers`, `RMSItems`, `Stores`, `SupplierInvoices`).
