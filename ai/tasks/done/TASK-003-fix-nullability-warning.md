# TASK-003

Goal:
Fix nullable reference warning in GuestService to keep build clean.

Steps:

1. Inspect GuestService warning CS8601 location
2. Apply a safe nullability fix without changing behavior
3. Run dotnet test to verify no regressions and warning removal

Constraints:

- Keep behavior unchanged
- Make the smallest possible source code change