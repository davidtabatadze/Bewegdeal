# Bewegdeal

A marketplace web application for moving and transport services, connecting customers with service companies.

## Tech Stack

- **Framework:** ASP.NET Core MVC (.NET 10)
- **Database:** SQLite (dev) / MySQL (prod) via EF Core
- **Real-time:** SignalR
- **UI:** Materio Bootstrap HTML Admin Template v3.0.0
- **Auth:** ASP.NET Core cookie authentication with claims

## User Roles

| Role | Description |
|------|-------------|
| Administrator | Platform management, user/chat/invoice oversight |
| Customer | Creates and manages transport requests |
| Company | Browses requests, chats with customers, submits proposals |

## Services

Moving, Removal, Pickup, Transport

## Getting Started

1. **Clone the repo**

2. **Configure `appsettings.Development.json`** — set `Database:Provider` to `sqlite` and `Database:ConnectionString` as needed.

3. **Run**
   ```bash
   dotnet run
   ```
   The app seeds an admin and sample users on first start.

4. **Default accounts** (password: `Admin1234`)
   - `admin@bewegdeal.at` — Administrator
   - `datiko.customer@bewegdeal.at` — Customer
   - `datiko.company@bewegdeal.at` — Company

## Project Structure

```
Controllers/   — MVC controllers (XBaseController base)
Data/          — Entities, repositories, filters, EF Core context
Enums/         — String-constant enums and identity field keys
Models/        — View models and service result types
Services/      — Business logic layer
Tools/         — Utilities: password hashing, file storage, SignalR hub, middleware
Views/         — Razor views and partial layouts
wwwroot/       — Static assets (vendor libs, CSS, JS)
```

## Key Routes

| URL | Description |
|-----|-------------|
| `/` | Public landing page |
| `/Account/Login` | Login |
| `/Account/Register` | Registration (3-step) |
| `/Dashboard` | Role-dispatched dashboard |
| `/Request/List` | Request list |
| `/Invoice/List` | Invoice list |
| `/Settings` | Admin settings |
