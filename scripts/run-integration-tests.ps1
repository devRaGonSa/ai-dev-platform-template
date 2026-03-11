Write-Host "Starting integration environment..."

docker compose -f docker-compose.test.yml up -d

Write-Host "Waiting for containers..."
Start-Sleep -Seconds 20

Write-Host "Running EF migrations..."

dotnet ef database update `
  --project FormularioBoda.Web `
  --startup-project FormularioBoda.Web

Write-Host "Running integration tests..."

dotnet test FormularioBoda.sln --filter Category=Integration

Write-Host "Stopping containers..."

docker compose -f docker-compose.test.yml down
