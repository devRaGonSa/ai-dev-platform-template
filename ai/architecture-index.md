# Architecture Index

This file provides a quick overview of the repository architecture.

AI agents should read this file before scanning the repository.

---

# Application Type

ASP.NET Core MVC (.NET 8)

Main project:

FormularioBoda.Web

Test project:

FormularioBoda.Web.Tests

---

# Core Layers

Controllers

FormularioBoda.Web/Controllers

Services

FormularioBoda.Web/Services

Models

FormularioBoda.Web/Models

Data

FormularioBoda.Web/Data

Views

FormularioBoda.Web/Views

Static assets

FormularioBoda.Web/wwwroot

---

# Dependency Injection

Configured in:

FormularioBoda.Web/Program.cs

Common registrations follow:

builder.Services.AddScoped
builder.Services.AddSingleton
builder.Services.AddTransient

---

# Database

Entity Framework Core

DbContext:

ApplicationDbContext

Migrations location:

FormularioBoda.Web/Data/Migrations

---

# Multi-tenancy

Entities include:

WeddingAccountId

Ensure tenant filtering when querying data.

---

# Storage

MinIO is used for media storage.

Relevant services are located in:

FormularioBoda.Web/Services

---

# Tests

Unit and integration tests are located in:

FormularioBoda.Web.Tests

Integration tests are marked with:

[Trait("Category", "Integration")]
