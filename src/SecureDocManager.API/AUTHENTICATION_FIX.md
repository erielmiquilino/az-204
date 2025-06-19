# Correção de Autenticação - SecureDocManager API

## Problemas Identificados

1. **Erro 500 em /documents**: Incompatibilidade de versões do Microsoft.Graph
2. **Erro 403 em outras rotas**: Configuração incorreta de autenticação

## Passo 1: Configurar o Azure AD

No Azure Portal, verifique as seguintes configurações para sua aplicação API:

1. **App Registration da API**:
   - Application (client) ID: `fbd9c38b-0f2b-4a17-afde-8860d42991da`
   - Tenant ID: `717144d2-6d9f-42a1-b56d-42afc3753ec3`
   - **IMPORTANTE**: Crie um Client Secret e anote o valor

2. **Expose an API**:
   - Application ID URI deve ser: `api://fbd9c38b-0f2b-4a17-afde-8860d42991da`
   - Scopes expostos:
     - `api://fbd9c38b-0f2b-4a17-afde-8860d42991da/Documents.Read`

3. **API Permissions**:
   - Microsoft Graph:
     - User.Read (Delegated)
   - Conceda admin consent

## Passo 2: Atualizar appsettings.Development.json

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "ndd.com.br",
    "TenantId": "717144d2-6d9f-42a1-b56d-42afc3753ec3",
    "ClientId": "fbd9c38b-0f2b-4a17-afde-8860d42991da",
    "ClientSecret": "COLE-SEU-CLIENT-SECRET-AQUI",
    "Audience": "api://fbd9c38b-0f2b-4a17-afde-8860d42991da"
  },
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  }
}
```

## Passo 3: Verificar App Registration do SPA (Frontend)

1. No Azure Portal, verifique a aplicação SPA
2. Em **Authentication**:
   - Redirect URIs: `http://localhost:5173`
   - Tipo: Single-page application
3. Em **API permissions**:
   - Sua API: `api://fbd9c38b-0f2b-4a17-afde-8860d42991da/Documents.Read`
   - Microsoft Graph: `User.Read`

## Passo 4: Atualizar .env do Frontend

```env
VITE_AZURE_CLIENT_ID=seu-spa-client-id
VITE_AZURE_TENANT_ID=717144d2-6d9f-42a1-b56d-42afc3753ec3
VITE_REDIRECT_URI=http://localhost:5173
VITE_API_SCOPE_URI=api://fbd9c38b-0f2b-4a17-afde-8860d42991da/Documents.Read
VITE_API_BASE_URL=https://localhost:7000/api
```

## Passo 5: Reiniciar Aplicações

1. Pare a API e o Frontend
2. Execute na pasta da API:

   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   dotnet run
   ```

3. Execute na pasta do Frontend:

   ```bash
   npm install
   npm run dev
   ```

## Verificação

1. Faça login no frontend
2. Abra o console do navegador (F12)
3. Verifique se há mensagens de erro
4. Tente acessar uma página que faz chamadas à API

Se ainda houver erros 403, verifique no console da API se o token está sendo validado corretamente.
