# Eagle 🦅

Eagle is a web application for managing a shoe shop's day-to-day operations — inventory, sales, and staff — built with a classic 3-tier architecture on ASP.NET Core MVC.

## Features

- **Inventory Management** — track shoe stock, sizes, categories, and restocking
- **Sales Tracking** — record and review transactions and daily/monthly sales
- **Staff Management** — manage employee records and roles

## Tech Stack

- **Presentation Layer (PL)** — ASP.NET Core MVC
- **Business Layer (BL)** — application/domain logic and services
- **Data Access Layer (DAL)** — EF Core
- **Database** — PostgreSQL

## Architecture

```
Eagle/
├── Eagle.PL/     # MVC Controllers, Views, ViewModels
├── Eagle.BL/     # Business logic, services, interfaces
└── Eagle.DAL/    # EF Core DbContext, entities, repositories, migrations
```

Each layer only depends on the one below it, keeping concerns separated and the codebase easier to maintain and test.

## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 or later)
- [PostgreSQL](https://www.postgresql.org/download/)

## Project Status

🚧 In active development.

## License

TBD