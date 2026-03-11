# Repository Context

## Project Overview

FormularioBoda is an ASP.NET Core MVC (`net8.0`) application for managing wedding guest confirmations and related workflows in a multi-tenant model (by `WeddingAccount`).

The project follows a layered architecture centered on:

- Controllers
- Services
- Models / ViewModels
- Data (EF Core)
- Views

The application currently uses:

- ASP.NET Core MVC + ASP.NET Core Identity
- Entity Framework Core
- SQL Server (main runtime database)
- MinIO (media object storage)
- xUnit for tests (with SQLite support in tests)

---

# Architecture

## Controllers

Controllers handle HTTP requests and delegate business logic to services.

Rules:

- Controllers should remain thin.
- Do not implement business logic inside controllers.
- Always call services for domain operations.

---

## Services

Services contain the main business logic of the application.

Rules:

- Services should not depend on Views.
- Prefer dependency injection.
- Business rules should live here.
- Tenant-aware behavior (`WeddingAccountId`) should be enforced here when applicable.

---

## Data Layer

The Data layer contains:

- `DbContext`
- EF Core configuration
- Migrations

Rules:

- Data access must go through `DbContext`.
- Avoid raw SQL unless necessary.
- Keep migration changes explicit and minimal.

---

## Models

Models represent domain entities and database structures.

Rules:

- Models should remain simple.
- Avoid placing business logic here unless it is domain-specific.
- Use ViewModels for UI-specific shaping.

---

## Views

Views contain Razor templates for UI rendering.

Rules:

- Views should not contain business logic.
- Use ViewModels when necessary.
- Keep tenant/public routing conventions consistent (for example `/w/{publicCode}`).

---

# Testing

Tests are written using xUnit (`FormularioBoda.Web.Tests`).

Rules:

- Add tests when modifying services.
- Prefer unit tests for business logic.
- Use SQLite or in-memory approaches for EF-related tests when appropriate.
- Preserve or improve current coverage of critical confirmation flows.

---

# Coding Guidelines

- Follow existing naming conventions.
- Prefer small focused changes.
- Do not modify unrelated files.
- Keep tasks under ~200 lines of change when feasible.
- Prefer modifying fewer than 5 files per task.

---

# AI Workflow

`Feature -> Planner -> Tasks -> Worker -> Tests -> Commit -> Push`

The worker agent processes tasks from:

`ai/tasks/pending`

Each task must follow:

`ai/task-template.md`
