# Script para criar e aplicar migrations
Write-Host "Criando migrations para SecureDocManager API..." -ForegroundColor Green

# Parar a API se estiver rodando
Write-Host "Por favor, pare a API (Ctrl+C) antes de continuar..." -ForegroundColor Yellow
Read-Host "Pressione Enter quando a API estiver parada"

# Criar migration inicial
Write-Host "Criando migration inicial..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate

# Aplicar migrations
Write-Host "Aplicando migrations ao banco de dados..." -ForegroundColor Cyan
dotnet ef database update

Write-Host "Migrations criadas e aplicadas com sucesso!" -ForegroundColor Green
Write-Host "Agora você pode executar 'dotnet run' para iniciar a API." -ForegroundColor Yellow 