# Inventory Stock Management System

## Project Overview

This project is a simple inventory stock management system with:

- item master management
- store master management
- stock receive and issue transactions
- current stock lookup by store and item
- stock movement and transaction detail reports

The solution includes a .NET backend API, a React frontend, and PostgreSQL persistence.

## Technologies and Frameworks

- Backend: ASP.NET Core 8 Web API
- ORM: Entity Framework Core 8
- Database: PostgreSQL
- Frontend: React 18 + TypeScript
- Frontend build tool: Vite
- HTTP client: Axios
- Reporting: RDLC via `ReportViewerCore.NETCore`
- API documentation: Swagger / OpenAPI

## Architecture Overview

- `backend-api/`
  ASP.NET Core Web API with services, controllers, EF Core model, migrations, and RDLC report generation.
- `frontend/`
  React single-page application that calls the backend API.
- `database/`
  PostgreSQL setup script for a fresh database.

The main backend layers are:

- Controllers: HTTP endpoints only
- Services: business rules and stock logic
- Data: EF Core `DbContext`, entity configuration, migrations, and development seed data
- DTOs: API contracts for requests, responses, and reports

## Database Setup

Two setup paths are available:

1. Fresh database from SQL script
2. EF Core migrations

For a ready-to-use setup with schema and sample data, use:

- [database/postgresql-script.sql](database/postgresql-script.sql)

The script creates the current EF Core schema and seeds:

- sample stores
- sample items
- sample stock transactions
- matching stock balances

## PostgreSQL Configuration

The default backend connection string is in [backend-api/appsettings.json](backend-api/appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=inventory_db;Username=postgres;Password=CHANGE_ME"
}
```

Update it for your local PostgreSQL instance before running the backend. For local development, prefer overriding it with an environment-specific file or environment variable rather than committing a real password.

## EF Core Migration Instructions

Use migrations if you want the database created directly from the EF Core model.

From the repository root:

```bash
dotnet ef database update --project backend-api --startup-project backend-api
```

Notes:

- In `Development`, the backend also runs `Database.MigrateAsync()` on startup.
- Development seeding adds master data for stores and items only.
- The SQL setup script includes additional sample transaction history and stock balances.

## Backend Run Instructions

From the repository root:

```bash
dotnet restore
dotnet run --project backend-api --launch-profile http
```

Default development URLs:

- API: `http://localhost:5231`
- Swagger: `http://localhost:5231/swagger`

## Frontend Run Instructions

From `frontend/`:

```bash
npm install
npm run dev
```

Default frontend URL:

- `http://localhost:5173`

In development, Vite proxies `/api` to `http://localhost:5231`.

For a deployed frontend, set `VITE_API_BASE_URL` to the backend base URL if needed.

## Sample Login or Configuration

There is no authentication or login flow implemented in the current application.

The main required configuration is:

- PostgreSQL connection string in `backend-api/appsettings.json`
- optional `VITE_API_BASE_URL` for non-proxy frontend deployments

## API Overview

Main endpoints:

- `GET/POST/PUT/PATCH/DELETE /api/items`
- Items with transaction history or nonzero stock must be deactivated rather than deleted.
- `GET/POST/PUT/PATCH /api/stores`
- `GET /api/stockbalances?storeId={id}&itemId={id}`
- `GET /api/stocktransactions`
- `GET /api/stocktransactions/{id}`
- `POST /api/stocktransactions/receive`
- `POST /api/stocktransactions/issue`
- `PUT /api/stocktransactions/{id}`
- `DELETE /api/stocktransactions/{id}`
- `GET /api/stockreports/movement`
- `GET /api/stockreports/movement/export`
- `GET /api/stockreports/transaction-details`
- `GET /api/stockreports/transaction-details/export`

Swagger provides the full request and response details.

## Stock Calculation Rules

- `Receipt` increases stock.
- `Issue` decreases stock.
- `OpeningBalance` is treated as a positive opening movement in reports and stock adjustments.
- Negative stock is blocked by backend validation.
- Report values are calculated from transaction history, not only from current `StockBalances`.

## Receive Behavior

- A receive transaction creates a stock transaction header and detail rows.
- The transaction is saved inside a database transaction.
- Each detail increases the related `StockBalances` quantity.

## Issue Behavior

- The frontend validates available stock before submit for issue entry.
- The backend validates requested quantity before saving and again while applying stock changes.
- Each issue detail decreases the related `StockBalances` quantity.
- If stock is insufficient, the request is rejected.

## Transaction Update Behavior

- Updating a transaction supports:
  - new detail rows
  - modified detail rows
  - deleted detail rows
- Stock is adjusted by the net difference between old and new quantities.
- Existing detail item identity cannot be changed during update.
- Deleting a transaction reverses its stock effect.

## Report Behavior

Two reports are implemented:

1. Stock Movement Report
   Shows opening, receive, issue, return, and closing by item and store.
2. Transaction Detail Report
   Shows transaction-level opening, receive, issue, and closing quantities.

Current report rules:

- calculations come from transaction history
- opening-balance entries inside the selected period are included in the Opening column
- `Return` is currently reported as zero
- export and print are supported through backend RDLC rendering

## Assumptions

- PostgreSQL is available locally or in a reachable environment.
- Stock transactions are created through the application service layer so `StockBalances` stay synchronized.
- Development uses the `http` launch profile unless changed locally.

## Limitations

- No authentication or user/role management is implemented.
- No dedicated UI flow exists for `Transfer`, `Adjustment`, or `Return` transactions.
- `Return` is not implemented as a transaction type in business processing, so report return quantity remains zero.
- Development auto-seeding does not populate sample transaction history; use the SQL script if you want a fuller sample dataset.

## Verification

Run `dotnet test` from the repository root and `npm run build` from `frontend/`.
Stop an existing API process before rebuilding its executable, or use
`dotnet test -p:UseAppHost=false -p:OutputPath=bin/verification/`.

The frontend development proxy expects the API on port 5231; start it with
`dotnet run --project backend-api --launch-profile http`.
A separately running older API on another port does not use the current source automatically.
For separate-origin deployments, configure the reverse proxy/CORS and set
`VITE_API_BASE_URL` to the full API prefix (for example `https://example.com/api`).

The SQL script is for an empty database and records the matching EF migration.
Do not run its sample inserts against an existing populated database.
Transaction edits and deletions serialize by transaction ID; stock rows are updated
atomically and multi-item changes use a consistent item order. Header remarks can
be cleared by submitting null. Detail dates mirror the transaction header date.
