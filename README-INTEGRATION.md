# Guia de Integração - SecureDocManager

Este documento descreve como configurar e executar o sistema SecureDocManager com integração completa entre frontend e backend, utilizando Azure KeyVault e Azure App Configuration.

## Arquitetura

- **Backend**: ASP.NET Core Web API
- **Frontend**: React com TypeScript e Material-UI
- **Autenticação**: Microsoft Entra ID (Azure AD)
- **Configuração**: Azure App Configuration
- **Secrets**: Azure Key Vault
- **Banco de Dados**: SQL Server + Cosmos DB
- **Storage**: Azure Blob Storage

## Configuração do Azure

### 1. Azure Key Vault

O Key Vault armazena todas as connection strings sensíveis:

```bash
# Criar Key Vault
az keyvault create --name kv-securedocmanager --resource-group rg-securedocmanager --location eastus

# Adicionar secrets
az keyvault secret set --vault-name kv-securedocmanager --name SqlDatabaseConnectionString --value "Server=..."
az keyvault secret set --vault-name kv-securedocmanager --name CosmosDBConnectionString --value "AccountEndpoint=..."
az keyvault secret set --vault-name kv-securedocmanager --name StorageConnectionString --value "DefaultEndpointsProtocol=..."
```

### 2. Azure App Configuration

O App Configuration armazena todas as configurações da aplicação:

```bash
# Criar App Configuration
az appconfig create --name appconfig-securedocmanager --resource-group rg-securedocmanager --location eastus

# Adicionar configurações
az appconfig kv set --name appconfig-securedocmanager --key AzureAd:TenantId --value "your-tenant-id"
az appconfig kv set --name appconfig-securedocmanager --key AzureAd:ClientId --value "your-api-client-id"
az appconfig kv set --name appconfig-securedocmanager --key AzureAd:Domain --value "your-domain.onmicrosoft.com"
```

### 3. Managed Identity

Habilitar Managed Identity para a aplicação acessar KeyVault e App Configuration:

```bash
# No App Service
az webapp identity assign --name app-securedocmanager --resource-group rg-securedocmanager

# Dar permissões no Key Vault
az keyvault set-policy --name kv-securedocmanager --object-id <managed-identity-id> --secret-permissions get list

# Dar permissões no App Configuration
az role assignment create --assignee <managed-identity-id> --role "App Configuration Data Reader" --scope <app-config-resource-id>
```

## Configuração do Backend

### 1. Variáveis de Ambiente

Para desenvolvimento local, configure:

```bash
# Windows PowerShell
$env:ConnectionStrings__AppConfig = "Endpoint=https://appconfig-securedocmanager.azconfig.io;Id=...;Secret=..."

# Linux/Mac
export ConnectionStrings__AppConfig="Endpoint=https://appconfig-securedocmanager.azconfig.io;Id=...;Secret=..."
```

### 2. Executar o Backend

```bash
cd src/SecureDocManager.API
dotnet restore
dotnet run
```

O backend estará disponível em `https://localhost:7000`

## Configuração do Frontend

### 1. Criar arquivo .env

Crie um arquivo `.env` na pasta `src/SecureDocManager.Web` baseado no `env.example`:

```env
VITE_AZURE_CLIENT_ID=your-spa-client-id
VITE_AZURE_TENANT_ID=your-tenant-id
VITE_REDIRECT_URI=http://localhost:5173
VITE_API_BASE_URL=https://localhost:7000/api
```

### 2. Instalar dependências e executar

```bash
cd src/SecureDocManager.Web
npm install
npm run dev
```

O frontend estará disponível em `http://localhost:5173`

## Configuração do Azure AD

### 1. Registro da API

1. Criar um novo registro de aplicativo para a API
2. Expor uma API e adicionar o scope `Documents.Read`
3. Configurar roles: Admin, Manager, Employee

### 2. Registro do SPA

1. Criar um novo registro de aplicativo para o SPA
2. Configurar como Single Page Application
3. Adicionar redirect URIs: `http://localhost:5173`, `https://your-app.azurewebsites.net`
4. Adicionar permissões da API: `api://securedocmanager-api/Documents.Read`

## Funcionalidades Integradas

### 1. Documentos

- Listar documentos com filtros
- Upload de documentos com tags e níveis de acesso
- Download de documentos
- Assinatura digital
- Exclusão de documentos

### 2. Usuários

- Listar usuários do Azure AD
- Editar informações de usuários
- Gerenciar roles

### 3. Auditoria

- Logs de todas as ações
- Filtros por usuário, documento e data
- Rastreamento de IP

## Desenvolvimento Local

Para desenvolvimento local sem Azure:

1. Use o emulador do Cosmos DB
2. Use LocalDB para SQL Server
3. Use Azurite para Storage
4. Configure as connection strings em `appsettings.Development.json`

## Troubleshooting

### Erro de CORS

Verifique se a URL do frontend está configurada no CORS do backend em `Program.cs`

### Erro de autenticação

1. Verifique se os IDs do tenant e client estão corretos
2. Confirme que as permissões foram concedidas no Azure AD
3. Verifique se o token está sendo enviado nas requisições

### Connection strings não encontradas

1. Verifique se a variável de ambiente `ConnectionStrings__AppConfig` está definida
2. Confirme que o Managed Identity tem acesso ao Key Vault
3. Verifique os nomes dos secrets no Key Vault
