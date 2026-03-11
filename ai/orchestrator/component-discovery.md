# Component Discovery

Before implementing a task, analyze the repository to identify the most relevant components.

## Goals

Automatically determine which parts of the codebase are related to the feature.

## Steps

1. Identify related Controllers
2. Identify related Services
3. Identify related Models
4. Identify DbContext or EF Core configuration
5. Identify existing tests related to the feature

## Search heuristics

Look for:

- class names matching the feature domain
- services injected into controllers
- EF Core DbSet references
- namespaces related to the feature

Example:

Feature: Guest confirmation reminders

Relevant components might be:

Controllers:
- GuestController.cs

Services:
- ConfirmationService.cs

Models:
- Guest.cs
- Confirmation.cs

Data:
- ApplicationDbContext.cs

Tests:
- ConfirmationServiceTests.cs

## Output

Provide a list of files to inspect before implementation.
