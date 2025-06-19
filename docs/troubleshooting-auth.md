# Guia de Troubleshooting - Autenticação e Microsoft Graph

## Erro: "Failed to fetch user profile from Graph"

Este erro ocorre quando a aplicação não consegue buscar o perfil do usuário do Microsoft Graph após o login.

### Erro Específico: "Invalid audience"

Se você vê o erro:

```text
Access token validation failure. Invalid audience.
```

Isso significa que o token foi emitido para o audience errado. O Microsoft Graph requer um token específico.

#### Solução Implementada

O código foi atualizado para:

1. Requisitar um token específico para o Microsoft Graph com o scope `User.Read`
2. Implementar tratamento de erro com limite de tentativas (máximo 3)
3. Redirecionar para uma página de erro após falhas repetidas
4. Prevenir loops infinitos de erro

### Causas Comuns e Soluções

#### 1. Arquivo .env não configurado

Crie um arquivo `.env` na pasta `src/SecureDocManager.Web/` com as seguintes variáveis:

```env
# Azure AD Configuration
VITE_AZURE_CLIENT_ID=seu-client-id-do-spa
VITE_AZURE_TENANT_ID=seu-tenant-id
VITE_REDIRECT_URI=http://localhost:5173
VITE_API_SCOPE_URI=api://seu-api-client-id/Documents.Read

# API Configuration
VITE_API_BASE_URL=https://localhost:7000/api
```

#### 2. Permissões do Microsoft Graph não configuradas

No Azure Portal, configure as seguintes permissões para sua aplicação SPA:

1. Acesse o Azure Portal → Azure Active Directory → App registrations
2. Selecione sua aplicação SPA
3. Vá em "API permissions"
4. Adicione as seguintes permissões:
   - Microsoft Graph → Delegated permissions → User.Read
   - Sua API personalizada → Documents.Read

5. Conceda consentimento de administrador (Grant admin consent)

#### 3. Configuração incorreta do CORS

Verifique se a API backend permite requisições do frontend:

1. No arquivo `appsettings.json` da API, verifique a configuração de CORS
2. Certifique-se de que `http://localhost:5173` está na lista de origens permitidas

#### 4. Token expirado ou inválido

Execute os seguintes passos:

1. Limpe o cache do navegador
2. Faça logout e login novamente
3. Verifique no console do navegador se o token está sendo adquirido corretamente

### Logs de Debug

Com as alterações feitas no `AuthProvider.tsx`, você verá no console:

- "Token acquired successfully" - se o token foi obtido
- "Calling Graph API: <https://graph.microsoft.com/v1.0/me>" - endpoint sendo chamado
- "Graph API response status: XXX" - código de status HTTP
- Detalhes do erro se houver falha

### Códigos de Erro Comuns

- **401 Unauthorized**: Token inválido ou sem permissões
- **403 Forbidden**: Aplicação não tem permissão para acessar o recurso
- **404 Not Found**: Usuário não encontrado (raro)

### Verificação Rápida

Execute este comando PowerShell para testar se suas credenciais estão corretas:

```powershell
# Substitua pelos seus valores
$clientId = "seu-client-id"
$tenantId = "seu-tenant-id"

# Teste de login
Start-Process "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/authorize?client_id=$clientId&response_type=code&redirect_uri=http://localhost:5173&scope=openid%20profile%20User.Read"
```

Se o login funcionar, mas o erro persistir, o problema está nas permissões ou configuração da aplicação.

---

## Correção de Autenticação - Backend API

### Problemas Comuns

1. **Erro 500 em /documents**: Incompatibilidade de versões do Microsoft.Graph
2. **Erro 403 em outras rotas**: Configuração incorreta de autenticação

### Configuração do Azure AD para a API

1. **App Registration da API**:
   - Verifique o Application (client) ID
   - Verifique o Tenant ID
   - **IMPORTANTE**: Crie um Client Secret se necessário

2. **Expose an API**:
   - Application ID URI: `api://seu-api-client-id`
   - Scopes expostos: `api://seu-api-client-id/Documents.Read`

3. **API Permissions**:
   - Microsoft Graph: User.Read (Delegated)
   - Conceda admin consent

### Configuração do Backend

Atualize o `appsettings.Development.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "seu-dominio.com",
    "TenantId": "seu-tenant-id",
    "ClientId": "seu-api-client-id",
    "ClientSecret": "seu-client-secret",
    "Audience": "api://seu-api-client-id"
  },
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read"
  }
}
```

### Reiniciar Aplicações

Após alterar as configurações:

1. **Backend (API)**:

   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   dotnet run
   ```

2. **Frontend (React)**:

   ```bash
   npm install
   npm run dev
   ```

### Verificação Final

1. Faça login no frontend
2. Abra o console do navegador (F12)
3. Verifique se há mensagens de erro
4. Teste chamadas à API

Se ainda houver erros 403, verifique no console da API se o token está sendo validado corretamente.
