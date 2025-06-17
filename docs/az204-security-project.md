# Projeto AZ-204: Sistema de Gestão de Documentos Corporativos

## 📋 Visão Geral do Projeto

**Nome:** SecureDocManager
**Objetivo:** Sistema web para gestão segura de documentos corporativos com diferentes níveis de acesso

### Funcionalidades Principais:
- Upload e download de documentos
- Controle de acesso baseado em funções (Admin, Manager, Employee)
- Assinatura digital de documentos
- Auditoria de acessos
- Integração com Microsoft Graph para dados do usuário
- Configurações seguras da aplicação

## 🎯 Tecnologias que Serão Exploradas

### ✅ Autenticação e Autorização
- **Microsoft Entra ID** para autenticação de usuários
- **Plataforma Microsoft Identity** para SSO
- **Claims-based authorization** com diferentes roles

### ✅ Armazenamento Seguro
- **Azure Key Vault** para chaves, segredos e certificados
- **App Configuration** para configurações da aplicação
- **Managed Identity** para acesso seguro aos recursos

### ✅ Controle de Acesso
- **Shared Access Signatures (SAS)** para documentos
- **Microsoft Graph API** para informações do usuário

### ✅ Arquitetura
- **Frontend:** React.js com MSAL.js
- **Backend:** ASP.NET Core Web API
- **Storage:** Azure Blob Storage
- **Database:** Azure SQL Database

---

## 🚀 Roteiro de Implementação

### **Fase 1: Configuração da Infraestrutura Azure**

#### 1.1 Criação dos Recursos Base
```bash
# Criar Resource Group
az group create --name rg-securedocmanager --location brazilsouth

# Criar Storage Account
az storage account create \
    --name stsecuredocmanager \
    --resource-group rg-securedocmanager \
    --location brazilsouth \
    --sku Standard_LRS

# Criar Azure SQL Database
az sql server create \
    --name sql-securedocmanager \
    --resource-group rg-securedocmanager \
    --location brazilsouth \
    --admin-user sqladmin \
    --admin-password "SuaSenhaSegura123!"

az sql db create \
    --resource-group rg-securedocmanager \
    --server sql-securedocmanager \
    --name db-documents \
    --service-objective Basic
```

#### 1.2 Configuração do Azure Key Vault
```bash
# Criar Key Vault
az keyvault create \
    --name kv-securedocmanager \
    --resource-group rg-securedocmanager \
    --location brazilsouth \
    --enabled-for-deployment true \
    --enabled-for-template-deployment true

# Adicionar segredos iniciais
az keyvault secret set \
    --vault-name kv-securedocmanager \
    --name "DatabaseConnectionString" \
    --value "Server=sql-securedocmanager.database.windows.net;Database=db-documents;..."

az keyvault secret set \
    --vault-name kv-securedocmanager \
    --name "StorageConnectionString" \
    --value "DefaultEndpointsProtocol=https;AccountName=stsecuredocmanager;..."
```

#### 1.3 Configuração do App Configuration
```bash
# Criar App Configuration
az appconfig create \
    --name appconfig-securedocmanager \
    --resource-group rg-securedocmanager \
    --location brazilsouth \
    --sku standard

# Adicionar configurações
az appconfig kv set \
    --name appconfig-securedocmanager \
    --key "Documents:MaxFileSizeMB" \
    --value "10"

az appconfig kv set \
    --name appconfig-securedocmanager \
    --key "Documents:AllowedExtensions" \
    --value "pdf,docx,xlsx,pptx"
```

---

### **Fase 2: Configuração do Microsoft Entra ID**

#### 2.1 Registro da Aplicação Web API
```bash
# Criar app registration para API
az ad app create \
    --display-name "SecureDocManager-API" \
    --identifier-uris "api://securedocmanager-api" \
    --app-roles '[
        {
            "allowedMemberTypes": ["User"],
            "description": "Administrator access",
            "displayName": "Admin",
            "id": "$(uuidgen)",
            "isEnabled": true,
            "value": "Admin"
        },
        {
            "allowedMemberTypes": ["User"],
            "description": "Manager access",
            "displayName": "Manager",
            "id": "$(uuidgen)",
            "isEnabled": true,
            "value": "Manager"
        },
        {
            "allowedMemberTypes": ["User"],
            "description": "Employee access",
            "displayName": "Employee",
            "id": "$(uuidgen)",
            "isEnabled": true,
            "value": "Employee"
        }
    ]'
```

#### 2.2 Registro da Aplicação Frontend
```bash
# Criar app registration para SPA
az ad app create \
    --display-name "SecureDocManager-SPA" \
    --spa-redirect-uris "http://localhost:3000" \
    --required-resource-accesses '[
        {
            "resourceAppId": "[API-APP-ID]",
            "resourceAccess": [
                {
                    "id": "[SCOPE-ID]",
                    "type": "Scope"
                }
            ]
        },
        {
            "resourceAppId": "00000003-0000-0000-c000-000000000000",
            "resourceAccess": [
                {
                    "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d",
                    "type": "Scope"
                }
            ]
        }
    ]'
```

#### 2.3 Configuração de Permissões Microsoft Graph
- User.Read (para perfil básico)
- User.ReadBasic.All (para listar usuários da organização)
- Directory.Read.All (para informações do diretório)

---

### **Fase 3: Implementação do Backend (ASP.NET Core)**

#### 3.1 Estrutura do Projeto
```
SecureDocManager.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── DocumentsController.cs
│   └── UsersController.cs
├── Services/
│   ├── IDocumentService.cs
│   ├── DocumentService.cs
│   ├── IKeyVaultService.cs
│   ├── KeyVaultService.cs
│   ├── IGraphService.cs
│   └── GraphService.cs
├── Models/
│   ├── Document.cs
│   ├── User.cs
│   └── DTOs/
├── Data/
│   └── ApplicationDbContext.cs
└── Program.cs
```

#### 3.2 Configuração Principal (Program.cs)
```csharp
// Configurar Managed Identity
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddKeyVaultClient(new Uri("https://kv-securedocmanager.vault.azure.net/"));
    clientBuilder.AddConfigurationClient(new Uri("https://appconfig-securedocmanager.azconfig.io"));
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

// Configurar Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Configurar Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
});
```

#### 3.3 Implementação do Key Vault Service
```csharp
public class KeyVaultService : IKeyVaultService
{
    private readonly SecretClient _secretClient;
    
    public KeyVaultService(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }
    
    public async Task<string> GetSecretAsync(string secretName)
    {
        var secret = await _secretClient.GetSecretAsync(secretName);
        return secret.Value.Value;
    }
    
    public async Task<string> GetConnectionStringAsync()
    {
        return await GetSecretAsync("DatabaseConnectionString");
    }
}
```

#### 3.4 Implementação SAS Tokens para Blob Storage
```csharp
public class DocumentService : IDocumentService
{
    public async Task<string> GenerateDownloadUrlAsync(string documentId, string userRole)
    {
        var blobClient = GetBlobClient(documentId);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "documents",
            BlobName = documentId,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        
        // Diferentes permissões baseadas no role
        if (userRole == "Admin" || userRole == "Manager")
        {
            sasBuilder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write);
        }
        else
        {
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
        }
        
        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
```

#### 3.5 Integração com Microsoft Graph
```csharp
public class GraphService : IGraphService
{
    private readonly GraphServiceClient _graphClient;
    
    public async Task<User> GetUserProfileAsync(string userId)
    {
        return await _graphClient.Users[userId]
            .Request()
            .Select(u => new { u.DisplayName, u.Mail, u.Department, u.JobTitle })
            .GetAsync();
    }
    
    public async Task<IEnumerable<User>> GetUsersInDepartmentAsync(string department)
    {
        return await _graphClient.Users
            .Request()
            .Filter($"department eq '{department}'")
            .GetAsync();
    }
}
```

---

### **Fase 4: Implementação do Frontend (React + MSAL)**

#### 4.1 Configuração MSAL
```javascript
// src/authConfig.js
export const msalConfig = {
    auth: {
        clientId: "YOUR-SPA-CLIENT-ID",
        authority: "https://login.microsoftonline.com/YOUR-TENANT-ID",
        redirectUri: "http://localhost:3000"
    },
    cache: {
        cacheLocation: "sessionStorage",
        storeAuthStateInCookie: false,
    }
};

export const loginRequest = {
    scopes: ["api://securedocmanager-api/access_as_user", "User.Read"]
};
```

#### 4.2 Componente de Autenticação
```javascript
// src/components/AuthWrapper.js
import { useMsal } from "@azure/msal-react";

const AuthWrapper = ({ children }) => {
    const { instance, accounts } = useMsal();
    
    const handleLogin = () => {
        instance.loginPopup(loginRequest);
    };
    
    if (accounts.length === 0) {
        return (
            <div>
                <button onClick={handleLogin}>Login com Microsoft</button>
            </div>
        );
    }
    
    return children;
};
```

#### 4.3 Hook para Chamadas de API
```javascript
// src/hooks/useApiCall.js
export const useApiCall = () => {
    const { instance, accounts } = useMsal();
    
    const callApi = async (url, options = {}) => {
        const request = {
            scopes: ["api://securedocmanager-api/access_as_user"],
            account: accounts[0]
        };
        
        const response = await instance.acquireTokenSilent(request);
        
        return fetch(url, {
            ...options,
            headers: {
                ...options.headers,
                'Authorization': `Bearer ${response.accessToken}`
            }
        });
    };
    
    return { callApi };
};
```

---

### **Fase 5: Implementação de Managed Identity**

#### 5.1 Configuração no App Service
```bash
# Habilitar System Assigned Managed Identity
az webapp identity assign \
    --name app-securedocmanager \
    --resource-group rg-securedocmanager

# Dar permissões ao Key Vault
az keyvault set-policy \
    --name kv-securedocmanager \
    --object-id [MANAGED-IDENTITY-OBJECT-ID] \
    --secret-permissions get list

# Dar permissões ao App Configuration
az role assignment create \
    --assignee [MANAGED-IDENTITY-OBJECT-ID] \
    --role "App Configuration Data Reader" \
    --scope /subscriptions/[SUB-ID]/resourceGroups/rg-securedocmanager/providers/Microsoft.AppConfiguration/configurationStores/appconfig-securedocmanager
```

#### 5.2 Código para Usar Managed Identity
```csharp
// Configuração no Program.cs
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri("https://appconfig-securedocmanager.azconfig.io"), 
                   new DefaultAzureCredential())
           .UseFeatureFlags();
});

// Configuração do Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri("https://kv-securedocmanager.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

### **Fase 6: Funcionalidades Avançadas**

#### 6.1 Assinatura Digital de Documentos
```csharp
public class DocumentSigningService
{
    private readonly KeyVaultService _keyVaultService;
    
    public async Task<byte[]> SignDocumentAsync(byte[] document, string certificateName)
    {
        var certificate = await _keyVaultService.GetCertificateAsync(certificateName);
        
        // Implementar assinatura digital usando o certificado do Key Vault
        using var rsa = certificate.GetRSAPrivateKey();
        var signature = rsa.SignData(document, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return signature;
    }
}
```

#### 6.2 Auditoria de Acessos
```csharp
public class AuditService
{
    public async Task LogAccessAsync(string userId, string documentId, string action)
    {
        var auditLog = new AuditLog
        {
            UserId = userId,
            DocumentId = documentId,
            Action = action,
            Timestamp = DateTime.UtcNow,
            IpAddress = GetClientIpAddress()
        };
        
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }
}
```

---

## 🧪 Cenários de Teste

### Teste 1: Autenticação e Autorização
- [ ] Login com diferentes tipos de usuário
- [ ] Acesso a recursos baseado em roles
- [ ] Renovação automática de tokens

### Teste 2: Gestão de Documentos
- [ ] Upload com validação de tipo de arquivo
- [ ] Download com SAS token temporário
- [ ] Acesso negado para usuários não autorizados

### Teste 3: Integração Microsoft Graph
- [ ] Busca de informações do usuário
- [ ] Listagem de colegas do mesmo departamento

### Teste 4: Segurança
- [ ] Secrets sendo lidos do Key Vault
- [ ] Configurações sendo lidas do App Configuration
- [ ] Managed Identity funcionando sem credenciais hardcoded

---

## 📚 Estrutura do Repositório GitHub

```
SecureDocManager/
├── README.md
├── docs/
│   ├── setup-guide.md
│   ├── architecture.md
│   └── api-documentation.md
├── infrastructure/
│   ├── azure-resources.bicep
│   └── deployment-scripts/
├── src/
│   ├── SecureDocManager.API/
│   ├── SecureDocManager.Web/
│   └── SecureDocManager.Tests/
├── .github/
│   └── workflows/
│       ├── ci-cd.yml
│       └── infrastructure.yml
└── docker/
    ├── Dockerfile.api
    └── Dockerfile.web
```

---

## 🎓 Pontos de Aprendizado para AZ-204

### ✅ Implementar autenticação e autorização de usuário
- **Aplicado:** Sistema completo de login com Microsoft Entra ID
- **Conceitos:** JWT tokens, Claims, Roles, Policies

### ✅ Microsoft Identity Platform
- **Aplicado:** MSAL.js no frontend, Microsoft.Identity.Web no backend
- **Conceitos:** OAuth 2.0, OpenID Connect, Token lifecycle

### ✅ Shared Access Signatures
- **Aplicado:** URLs temporárias para download de documentos
- **Conceitos:** Time-based access, Permission levels, Blob SAS

### ✅ Microsoft Graph
- **Aplicado:** Busca de perfil do usuário e informações organizacionais
- **Conceitos:** Graph SDK, Scoped permissions, Batch requests

### ✅ App Configuration & Key Vault
- **Aplicado:** Configurações dinâmicas e secrets seguros
- **Conceitos:** Configuration management, Secret rotation, Feature flags

### ✅ Managed Identity
- **Aplicado:** Acesso sem credenciais a recursos Azure
- **Conceitos:** System vs User assigned, Role assignments, DefaultAzureCredential

---

## 🚀 Próximos Passos

1. **Semana 1-2:** Configurar infraestrutura Azure e Entra ID
2. **Semana 3-4:** Implementar backend com autenticação
3. **Semana 5:** Desenvolver frontend com MSAL
4. **Semana 6:** Integrar Key Vault e App Configuration
5. **Semana 7:** Implementar Microsoft Graph
6. **Semana 8:** Testes, documentação e deploy

Este projeto te dará experiência hands-on com todos os tópicos de segurança da AZ-204, criando um portfólio sólido para sua certificação!