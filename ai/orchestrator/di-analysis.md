# Dependency Injection Analysis

Analyze the ASP.NET Core dependency injection configuration to understand service relationships.

## Objectives

Identify how controllers, services and data access components are connected.

## Steps

1. Inspect Program.cs or Startup.cs.
2. Locate service registrations such as:

builder.Services.AddScoped
builder.Services.AddSingleton
builder.Services.AddTransient

3. Map the relationship:

Interface -> Implementation

Example:

IConfirmationService -> ConfirmationService

4. Identify which controllers depend on those services.

Example:

GuestController -> IConfirmationService

5. Determine downstream dependencies such as DbContext or external services.

## Output

Produce a dependency map such as:

Controller:
- GuestController

Service:
- IConfirmationService -> ConfirmationService

Data:
- ApplicationDbContext

This information should guide which components are modified in a task.
