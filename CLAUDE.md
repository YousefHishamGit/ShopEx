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

Backend unit tests: `dotnet test ShopEx.Tests` (xUnit + NSubstitute; pure money calculations and service rules with mocked repositories, no database). `ShopEx.postman_collection.json` and `ShopEx/ShopEx.http` hold sample API requests. The `*.log` files in the root are stray build output.

## Architecture

Clean-architecture solution with four projects: `ShopEx` (controllers, `Program.cs` DI wiring) → `ShopEx.Application` (DTOs, service/repository interfaces, service implementations) → `ShopEx.Domain` (entities/enums); `ShopEx.Infrastructure` implements the repositories and EF contexts. DI registrations are all manual in `ShopEx/Program.cs`; add new repositories/services there.

**Two databases, two DbContexts** (connection strings in `appsettings.json`, SQL Server on `.` with Windows auth):
- `RMSContext` → `RMSHQ`: the pre-existing RMS database. Entities live in `ShopEx.Domain/RMSEntity` and `RMSEnum`; repos/services/interfaces are under `RMS` folders. Treat it as externally owned (no migrations here).
- `ShopContext` → `ShopEXdb`: the app's own database. Entities in `ShopEx.Domain/Entity`, EF configurations and **migrations** in `ShopEx.Infrastructure/Persistence/ShopDb`. `ShopDbSeeder.SeedAsync` runs at every startup.

**Errors**: `ShopEx/Middleware/GlobalExceptionHandler.cs` (registered first in the pipeline) turns unhandled exceptions into `{ message }` JSON: `ArgumentException`/`InvalidOperationException` → 400, `KeyNotFoundException` → 404 (only when thrown from `ShopEx.*` code, so EF/framework messages never leak), `DbUpdateConcurrencyException`/unique-index violations → 409, anything else → 500 with a generic message (stack trace in `detail` in Development only). Don't add `catch (Exception ex) => ex.Message` in controllers; throw these exception types from services instead.

**Concurrency**: read-validate-write sections run inside `IUnitOfWork.ExecuteInTransactionAsync` and first call `IUnitOfWork.AcquireLockAsync(resource)` (SQL Server `sp_getapplock`, transaction-owned, throws `InvalidOperationException` if not granted), then re-read the record. Used for payments/settlements/cancel (`SupplierInvoice:{id}`, `Consignment:{id}`, `MaintenanceTicket:{id}`), balance closings (`Balance:{store}:{type}`) and document numbering (`MaintenanceTicket:Number`, `Consignment:Number`, `SupplierReturn:Number`). `StockBalance` and `Device` also have a `RowVersion`.

**Lists & paging**: big lists are paged in SQL with a 100-row cap: supplier invoices, suppliers, supplier statement, stock (`Stock/balances`, warehouse totals only when `includeTotals=true`), maintenance (`paged`), devices, expenses, customer statements, consignments (`Consignments/paged`, dealer-wide stats in the same response) and the balance ledger (`Balances/ledger?page=`). Item pickers load a warehouse with one call to `Stock/balances/all`. RMS runs at compatibility level 120, so `Contains()` over id lists becomes an inline `IN (...)`: keep lists chunked (see `ItemRepository`).

ShopEx entities reference RMS records (suppliers, items, stores) by ID only, so there are no cross-database FKs; data from both sides is combined in the Application services. Repositories for the ShopEx side share an `IUnitOfWork`.

**Frontend**: standalone components in `src/app/pages/<feature>`, routes in `app.routes.ts`, one service per backend area in `src/app/services`, and all TS interfaces in `src/app/models/models.ts`. `ApiService` hardcodes `baseUrl = 'https://localhost:7122/api'` (check other services for the same constant when changing the API address). The API enables a permissive `AllowAll` CORS policy. Backend controller names map to API routes used by the services (e.g. `RMSSuppliers`, `RMSItems`, `Stores`, `SupplierInvoices`).

## Auth & roles

JWT bearer auth with two roles: `Admin` (HQ, everything) and `Employee` (branch). Users live in `ShopEx` DB (`Users` table); passwords are stored as plain text **by the owner's explicit request** (compare is constant-time; the only places to change for hashing are `AuthService.PasswordMatches` and the password assignments in `UserService`).

- **Secure by default**: `Program.cs` sets a `FallbackPolicy` requiring role `Admin`, so any controller/action without `[Authorize]` is admin-only. Only `AuthController.Login` is `[AllowAnonymous]`. Employee-allowed actions are marked explicitly with `[Authorize(Roles = AppRoles.AdminOrEmployee)]` at *action* level (a class-level `[Authorize]` would AND with it) — currently: maintenance list/paged/get/create/deliver, balances `grouped`/`ledger`/`close-store` plus the Employee-only `charge-own` (branch tops up its own balance; `BalanceService.ChargeOwnBranchAsync` creates only the `Balance` charge, see below), and the Employee-only branch expense endpoints `Expenses/branch` (list/create) + `Expenses/branch/categories` (store, creator and date come from the token/server), and `Devices` list/stores/create/update/sell (phones bought for resale by HQ or a branch; an Employee sees only unsold (`InStock`) devices from all branches/HQ and may sell any of them (best price wins; the selling branch is stored in `SoldStoreId` from the token), but creates/edits only their own branch's unsold devices — store comes from the token on create; sold devices, sale price and profit are visible to Admin only; `cancel-sale` and `DELETE` are Admin only; `StoreId`/`SoldStoreId = null` means HQ).
- **Device purchases and balance charges are NOT expenses**: like supplier invoices they are purchases (stock / balance for resale), so `DeviceService` and `BalanceService.ChargeOwnBranchAsync` create no `Expense` (device cost only feeds device profit; charges show their profit at closing). `Expense.DeviceId`/`BalanceId` are legacy columns kept unused (`IsAutomatic` guards on manual update/delete remain harmlessly); `ShopDbSeeder` deletes any leftover expenses linked to them (and the empty "شراء أجهزة"/"شحن رصيد" categories) at startup. Only manually entered expenses exist now. Dashboard branch rows (`BranchDailyDto`) include devices, maintenance revenue, balances and expenses per branch plus an HQ row; consignments are not tied to a branch so they only appear in the HQ row/TOTAL.
- **Token claims** (`ShopEx/Security/JwtTokenService.cs`): `sub`, `name`, `role`, `storeId` (employee's branch), `stamp` (`User.SecurityStamp`). Controllers must take the branch from `User.StoreId()` (never from the request) for employees. `OnTokenValidated` re-checks the user is active and the stamp matches (cached 60s, invalidated on password change/disable/delete).
- **Config**: `Jwt:Key` (32+ chars) comes from user-secrets / env var `Jwt__Key`; in Development a random key is generated if missing. First admin is seeded when the Users table is empty: `InitialAdmin:Username`/`InitialAdmin:Password`, else a random password is logged once. CORS origins: `Cors:AllowedOrigins`.
- **Subscription / license (developer-controlled)**: a third role `Dev` lives in its **own table** `DevUsers` (never in `Users`, so no client screen/API can list it; password is PBKDF2-hashed via `IPasswordHasher`). `Licenses` is a single-row table (`Id = 1`): `IsEnabled` on/off + `ExpiresAt` (UTC). The license is valid only if enabled and not expired. `LicenseMiddleware` (after `UseAuthentication`) returns 403 `{ message, code: "LICENSE_EXPIRED" }` for any authenticated non-Dev API call when invalid, and `AuthService.LoginAsync` rejects Admin/Employee logins. Dev endpoints: `DevController` (`POST api/Dev/login` anonymous; `GET api/Dev/license`, `PUT api/Dev/license/enabled`, `POST api/Dev/license/extend {months}` are `Dev` only). `Dev` tokens validate against `DevUsers` in `OnTokenValidated` and cannot reach admin endpoints (fallback policy requires `Admin`). Config: `InitialDev:Username`/`InitialDev:Password` seed the first dev account when `DevUsers` is empty (no default account exists; remove the password from config after the first run); `License:InitialGraceDays` (default 30) sets the first license's expiry when the row is created.
- **Frontend**: `core/auth.interceptor.ts` (adds the token to API calls only, handles 401/403, signs out on `LICENSE_EXPIRED`), `core/auth.guard.ts` (`adminGuard`, `employeeGuard`, `devGuard`, `guestGuard`), `services/auth.service.ts`. The developer UI is hidden (not linked anywhere): `/dev/login` (reuses `LoginComponent` with `data: { dev: true }`) and `/dev/license` (`pages/dev/license`). Employees can only reach `/branch/*`; everything else is under the admin-guarded parent route in `app.routes.ts`.
