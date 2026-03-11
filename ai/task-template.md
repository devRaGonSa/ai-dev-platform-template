# TASK-XXX

## Goal
Clear description of the objective.

## Context
Explain where in the project the change happens.

## Steps

1. Step one
2. Step two
3. Step three

## Files to Read First

List the most relevant files that should be inspected before implementing the task.

Examples:

- Controllers/GuestController.cs
- Services/ConfirmationService.cs
- Data/ApplicationDbContext.cs
- Models/Guest.cs

Rules:

- The agent should read these files before making any change.
- Prefer reading existing services, controllers and models related to the feature.
- Keep the list small (3-6 files).

## Expected Files to Modify

List the files that are expected to change during this task.

Examples:

- Controllers/GuestController.cs
- Services/ConfirmationService.cs
- Data/ApplicationDbContext.cs
- Views/Guest/Confirm.cshtml

Rules:

- Prefer modifying only the files listed here.
- If additional files are required, explain why in the commit message.
- Do not modify unrelated files.

## Constraints

- Follow ASP.NET Core MVC architecture
- Do not modify unrelated files
- Keep the change minimal
- Prefer small commits

## Validation

Before completing the task ensure:

- dotnet build succeeds
- dotnet test succeeds
- no new warnings introduced

## Change Budget

- Prefer modifying fewer than 5 files.
- Prefer changes under 200 lines of code.
- Split the work into additional tasks if limits are exceeded.
