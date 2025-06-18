# SecureDocManager API

API Backend para o sistema de gestão segura de documentos corporativos com integração Azure.

## 📋 Pré-requisitos

- .NET 9.0 SDK
- Azure Subscription (para recursos em produção)
- Azure Storage Emulator ou Azurite (para desenvolvimento local)
- SQL Server ou LocalDB
- Azure Cosmos DB Emulator (opcional para desenvolvimento)

## 🛠️ Configuração

### 1. Configuração do Azure AD

1. Registre sua aplicação no Azure AD
2. Configure as permissões do Microsoft Graph:
   - User.Read
   - User.ReadBasic.All
   - Directory.Read.All

3. Atualize o `appsettings.json` com:
   - TenantId
   - ClientId
   - Domain

### 2. Configuração Local

Para desenvolvimento local, o projeto está configurado para usar:

- LocalDB para SQL Server
- Azure Storage Emulator
- Cosmos DB Emulator (opcional)

### 3. Configuração dos Secrets (Desenvolvimento)

```bash
dotnet user-secrets init
dotnet user-secrets set "AzureAd:ClientSecret" "your-client-secret"
```

## 🚀 Executando o Projeto

### Desenvolvimento Local

#### 1. Instalar dependências

```bash
dotnet restore
```

#### 2. Criar o banco de dados

```bash
dotnet ef database update
```

#### 3. Executar a aplicação

```bash
dotnet run
```

A API estará disponível em:

- <https://localhost:5001>
- <http://localhost:5000>

### Swagger/OpenAPI

Acesse a documentação da API em: <https://localhost:5001/swagger>

## 📁 Estrutura do Projeto

```text
SecureDocManager.API/
├── Controllers/         # Controladores da API
│   ├── AuthController.cs
│   ├── AuditController.cs      # ✅ Fase 6 - Auditoria
│   ├── DocumentsController.cs
│   └── UsersController.cs
├── Services/           # Serviços de negócio
│   ├── DocumentService.cs
│   ├── KeyVaultService.cs
│   ├── GraphService.cs
│   ├── CosmosService.cs
│   ├── AuditService.cs         # ✅ Fase 6 - Auditoria
│   ├── DocumentSigningService.cs # ✅ Fase 6 - Assinatura Digital
│   └── Interfaces/
├── Models/             # Modelos de dados
│   ├── Document.cs
│   ├── User.cs
│   ├── AuditLog.cs            # ✅ Fase 6 - Logs de Auditoria
│   ├── CosmosDocument.cs
│   └── DTOs/
├── Data/               # Contextos de dados
│   ├── ApplicationDbContext.cs
│   └── CosmosDbContext.cs
└── Program.cs          # Configuração principal
```

## 🔑 Principais Funcionalidades

### Autenticação e Autorização

- Integração com Microsoft Entra ID
- Roles: Admin, Manager, Employee
- Políticas de autorização baseadas em roles

### Gestão de Documentos

- Upload/Download seguro de documentos
- Controle de acesso baseado em departamento e role
- Geração de URLs temporárias com SAS tokens
- **✅ Assinatura digital de documentos** (Fase 6)
- **✅ Verificação de assinaturas digitais** (Fase 6)

### Integração com Azure

- Azure Key Vault para secrets e operações criptográficas
- Azure Blob Storage para arquivos
- Azure Cosmos DB para busca rápida
- Azure SQL Database para dados relacionais
- Microsoft Graph para informações de usuários

### **✅ Auditoria Completa** (Fase 6)

- **✅ Log automático de todas as ações**
- **✅ Histórico detalhado de acesso a documentos**
- **✅ Rastreamento de assinaturas digitais**
- **✅ Controle de atividades por usuário**

## 🔒 Segurança

- Autenticação OAuth 2.0/OpenID Connect
- Managed Identity para acesso aos recursos Azure
- Criptografia em trânsito e em repouso
- Controle de acesso granular
- **✅ Auditoria completa com logs detalhados** (Fase 6)
- **✅ Assinatura digital usando Azure Key Vault** (Fase 6)

## 📝 Endpoints Principais

### Auth

- `GET /api/auth/me` - Informações do usuário atual
- `GET /api/auth/roles` - Roles do usuário
- `GET /api/auth/permissions` - Permissões do usuário

### Documents

- `POST /api/documents/upload` - Upload de documento
- `GET /api/documents/{id}` - Obter documento
- `GET /api/documents/department/{departmentId}` - Listar documentos do departamento
- **✅ `POST /api/documents/sign`** - Assinar documento digitalmente (Fase 6)
- **✅ `POST /api/documents/{id}/verify-signature`** - Verificar assinatura (Fase 6)
- `DELETE /api/documents/{id}` - Deletar documento

### **✅ Audit** (Fase 6)

- **✅ `GET /api/audit/logs`** - Obter logs de auditoria (Admin/Manager)
- **✅ `GET /api/audit/my-activity`** - Obter atividade do usuário atual
- **✅ `GET /api/audit/document/{id}/history`** - Obter histórico de um documento

### Users

- `GET /api/users` - Listar usuários
- `GET /api/users/{id}` - Obter usuário
- `PUT /api/users/{id}/role` - Atualizar role do usuário
- `GET /api/users/audit-logs` - Logs de auditoria

## 🎯 **Fase 6 - Funcionalidades Avançadas** ✅

### 📋 Resumo da Implementação

A Fase 6 foi implementada com sucesso, adicionando **Assinatura Digital de Documentos** e **Auditoria de Acessos** ao sistema.

### 🆕 Novos Componentes Criados

#### 1. Modelos

- **`Models/AuditLog.cs`** - Entidade para logs de auditoria

#### 2. Serviços de Auditoria

- **`Services/IAuditService.cs`** - Interface do serviço de auditoria
- **`Services/AuditService.cs`** - Implementação do serviço de auditoria

#### 3. Serviços de Assinatura Digital

- **`Services/IDocumentSigningService.cs`** - Interface do serviço de assinatura digital
- **`Services/DocumentSigningService.cs`** - Implementação do serviço de assinatura digital

#### 4. Controladores

- **`Controllers/AuditController.cs`** - Controlador para funcionalidades de auditoria

### 🔧 Componentes Atualizados

#### 1. Configurações

- **`SecureDocManager.API.csproj`** - Adicionado pacote `Azure.Security.KeyVault.Keys v4.7.0`
- **`Program.cs`** - Registrados novos serviços de auditoria e assinatura digital

#### 2. Controladores Existentes

- **`Controllers/DocumentsController.cs`**:
  - Integração com `IAuditService` para logs automáticos
  - Novo endpoint `/sign` aprimorado com assinatura digital
  - Novo endpoint `/{id}/verify-signature` para verificação de assinaturas
  - Logs de auditoria em todas as operações (upload, download, view, delete)

#### 3. Serviços Existentes

- **`Services/IDocumentService.cs`** - Adicionados métodos:
  - `GetUserRoleAsync(string userId)`
  - `DownloadDocumentAsync(int documentId)`
- **`Services/DocumentService.cs`** - Implementados novos métodos para suporte à assinatura digital

### ✅ Sistema de Auditoria Completo

#### Logs Automáticos

- **Upload de documentos** - Registra quando um documento é enviado
- **Visualização de documentos** - Registra quando um documento é acessado
- **Assinatura de documentos** - Registra quando um documento é assinado digitalmente
- **Exclusão de documentos** - Registra quando um documento é deletado
- **Verificação de assinaturas** - Registra quando uma assinatura é verificada

### ✅ Assinatura Digital de Documentos

#### Funcionalidades de Assinatura

- **Assinatura usando Azure Key Vault** - Utiliza chaves criptográficas do Key Vault
- **Verificação de assinaturas** - Valida assinaturas usando certificados
- **Controle de permissões** - Apenas Admin e Manager podem assinar documentos
- **URLs temporárias** - Gera URLs seguras para documentos assinados

### 🔐 Integração com Azure Key Vault

#### Pacotes Azure Utilizados

- **Azure.Security.KeyVault.Keys** - Para operações criptográficas
- **Azure.Security.KeyVault.Certificates** - Para gestão de certificados
- **Azure.Security.KeyVault.Secrets** - Para secrets seguros

#### Funcionalidades de Segurança

- **Managed Identity** - Autenticação sem credenciais hardcoded
- **Assinatura RSA-256** - Algoritmo criptográfico robusto
- **Certificados X.509** - Padrão industrial para assinaturas digitais

### 📊 Estrutura de Dados

#### AuditLog

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public int? DocumentId { get; set; }
    public string Action { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
}
```

#### DocumentSignature

```csharp
public class DocumentSignature
{
    public int DocumentId { get; set; }
    public byte[] Signature { get; set; }
    public string SignedBy { get; set; }
    public DateTime SignedAt { get; set; }
    public string CertificateThumbprint { get; set; }
    public bool IsValid { get; set; }
}
```

## 🎓 Objetivos de Aprendizado AZ-204 Atingidos

### ✅ Implementar Autenticação e Autorização

- Controle baseado em roles para assinatura digital
- Auditoria de acesso com informações do usuário

### ✅ Azure Key Vault - Funcionalidades Avançadas

- **Chaves criptográficas** para assinatura digital
- **Certificados** para verificação de assinaturas
- **Operações criptográficas** usando CryptographyClient

### ✅ Gestão de Configurações Seguras

- Configurações dinâmicas via Key Vault
- Managed Identity para acesso seguro

### ✅ Monitoramento e Auditoria

- Sistema completo de logs de auditoria
- Rastreamento de ações dos usuários
- Histórico detalhado de documentos

## 🐛 Troubleshooting

### Erro de conexão com Azure AD

- Verifique as configurações do TenantId e ClientId
- Confirme que o redirect URI está configurado no Azure AD

### Erro de conexão com Storage

- Para desenvolvimento, certifique-se que o Azure Storage Emulator está rodando
- Execute: `AzureStorageEmulator.exe start`

### Erro de conexão com SQL Server

- Verifique se o LocalDB está instalado
- Execute: `sqllocaldb info` para verificar instâncias disponíveis

### Erro de conexão com Azure Key Vault

- Verifique se a Managed Identity está configurada corretamente
- Confirme que as permissões no Key Vault estão configuradas

## 🚀 Próximos Passos

Para completar a implementação em produção:

1. **Certificados de Produção**: Configurar certificados reais no Azure Key Vault
2. **Persistência de Assinaturas**: Implementar tabela para armazenar assinaturas no banco
3. **Validação Avançada**: Adicionar validação de integridade de documentos
4. **Dashboard de Auditoria**: Interface visual para análise de logs
5. **Alertas de Segurança**: Notificações para atividades suspeitas

## 📚 Documentação Adicional

- [Azure AD Authentication](https://docs.microsoft.com/azure/active-directory/develop/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/)
- [Microsoft Graph](https://docs.microsoft.com/graph/)

---

## ✅ Status do Projeto

**FASE 6 CONCLUÍDA COM SUCESSO** ✅

- ✅ Auditoria de Acessos implementada
- ✅ Assinatura Digital implementada  
- ✅ Integração com Azure Key Vault
- ✅ Controladores atualizados
- ✅ Testes de compilação aprovados
- ✅ Documentação completa

*Implementação realizada seguindo rigorosamente os padrões arquiteturais do projeto e as melhores práticas de segurança do Azure.*
