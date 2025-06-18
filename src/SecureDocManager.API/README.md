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
│   ├── DocumentsController.cs
│   └── UsersController.cs
├── Services/           # Serviços de negócio
│   ├── DocumentService.cs
│   ├── KeyVaultService.cs
│   ├── GraphService.cs
│   └── CosmosService.cs
├── Models/             # Modelos de dados
│   ├── Document.cs
│   ├── User.cs
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
- Assinatura digital de documentos

### Integração com Azure

- Azure Key Vault para secrets
- Azure Blob Storage para arquivos
- Azure Cosmos DB para busca rápida
- Azure SQL Database para dados relacionais
- Microsoft Graph para informações de usuários

### Auditoria

- Log completo de todas as ações
- Histórico de acesso a documentos
- Rastreamento de alterações

## 🔒 Segurança

- Autenticação OAuth 2.0/OpenID Connect
- Managed Identity para acesso aos recursos Azure
- Criptografia em trânsito e em repouso
- Controle de acesso granular
- Auditoria completa

## 📝 Endpoints Principais

### Auth

- `GET /api/auth/me` - Informações do usuário atual
- `GET /api/auth/roles` - Roles do usuário
- `GET /api/auth/permissions` - Permissões do usuário

### Documents

- `POST /api/documents/upload` - Upload de documento
- `GET /api/documents/{id}` - Obter documento
- `GET /api/documents/department/{departmentId}` - Listar documentos do departamento
- `POST /api/documents/sign` - Assinar documento
- `DELETE /api/documents/{id}` - Deletar documento

### Users

- `GET /api/users` - Listar usuários
- `GET /api/users/{id}` - Obter usuário
- `PUT /api/users/{id}/role` - Atualizar role do usuário
- `GET /api/users/audit-logs` - Logs de auditoria

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

## 📚 Documentação Adicional

- [Azure AD Authentication](https://docs.microsoft.com/azure/active-directory/develop/)
- [Azure Key Vault](https://docs.microsoft.com/azure/key-vault/)
- [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/)
- [Microsoft Graph](https://docs.microsoft.com/graph/)
