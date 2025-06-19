# SecureDocManager 🔐

Sistema de gestão segura de documentos corporativos desenvolvido como projeto de estudos para a certificação **Microsoft Azure AZ-204**.

## 📋 Sobre o Projeto

O **SecureDocManager** é uma aplicação web completa que demonstra a implementação prática de conceitos de segurança do Azure. Este projeto foi criado especificamente para consolidar o conhecimento necessário para a seção "Implementar a segurança do Azure" da certificação AZ-204, que representa 15-20% do exame.

### Por que este projeto foi criado?

- **Aprendizado prático**: Aplicar conceitos de segurança do Azure em um cenário real
- **Preparação para AZ-204**: Cobrir todos os tópicos de segurança exigidos no exame
- **Portfólio técnico**: Demonstrar competência em desenvolvimento com Azure
- **Integração de tecnologias**: Ver como diferentes serviços do Azure trabalham juntos

### ⚠️ Importante: Uso de IA no Desenvolvimento

Este projeto foi desenvolvido com **assistência massiva de Inteligência Artificial** (Claude Sonnet/opus e Gemini Pro). O objetivo principal é **didático e educacional**, focando especificamente no aprendizado das integrações com serviços Azure.

**Características do projeto:**

- 🤖 **Geração de código por IA**: Aproximadamente 80-90% do código foi gerado com assistência de IA
- 📚 **Foco didático**: Prioriza clareza e compreensão sobre otimização extrema
- 🎯 **Objetivo educacional**: Demonstrar integrações Azure de forma prática e funcional
- 🔧 **Código funcional**: Totalmente operacional, mas não necessariamente production-ready

**O que este projeto NÃO é:**

- ❌ Exemplo de código enterprise otimizado
- ❌ Referência de melhores práticas de arquitetura
- ❌ Solução pronta para produção sem revisões

**O que este projeto É:**

- ✅ Ambiente de aprendizado prático para Azure AZ-204
- ✅ Demonstração funcional de integrações Azure
- ✅ Base para experimentação e estudo
- ✅ Exemplo de como IA pode acelerar o desenvolvimento para fins educacionais

## 🚀 Funcionalidades

### Para Usuários

- **Autenticação SSO** com Microsoft Entra ID (Azure AD)
- **Upload/Download** seguro de documentos
- **Visualização** de documentos com controle de acesso
- **Assinatura digital** de documentos importantes
- **Histórico de auditoria** de todas as ações

### Níveis de Acesso

- **Admin**: Acesso total ao sistema, gestão de usuários e configurações
- **Manager**: Gestão de documentos do departamento, visualização de logs
- **Employee**: Upload e visualização de documentos permitidos

## 🛠️ Tecnologias Utilizadas

### Frontend

- **React 19** com TypeScript
- **Material-UI** para interface moderna
- **MSAL React** para autenticação Azure AD
- **Vite** para build otimizado

### Backend

- **ASP.NET Core 9** Web API
- **Entity Framework Core** com SQL Server
- **Azure SDK** para integração com serviços

### Serviços Azure

- **Microsoft Entra ID**: Autenticação e autorização
- **Azure Key Vault**: Armazenamento seguro de secrets e certificados
- **Azure Blob Storage**: Armazenamento de documentos
- **Azure SQL Database**: Dados relacionais
- **Azure Cosmos DB**: Busca rápida e logs
- **Microsoft Graph**: Informações de usuários e organização

## 🏗️ Arquitetura

```text
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   React SPA     │────▶│  ASP.NET API    │────▶│  Azure Services │
│   (Frontend)    │     │   (Backend)     │     │  (Storage/Auth) │
└─────────────────┘     └─────────────────┘     └─────────────────┘
        │                        │                        │
        │                        │                        │
        ▼                        ▼                        ▼
   [MSAL.js]              [EF Core/ADO.NET]         [Key Vault]
   [Material-UI]          [Azure SDK]               [Blob Storage]
   [React Router]         [MS Graph SDK]            [SQL Database]
                                                    [Cosmos DB]
```

## 🔒 Implementações de Segurança

### Autenticação e Autorização

- OAuth 2.0 / OpenID Connect via Microsoft Entra ID
- JWT tokens com claims e roles
- Políticas de autorização baseadas em roles

### Armazenamento Seguro

- **Azure Key Vault** para connection strings e certificados
- **Managed Identity** para acesso sem credenciais
- **SAS tokens** para acesso temporário a blobs

### Auditoria e Compliance

- Log completo de todas as ações dos usuários
- Rastreamento de acesso a documentos
- Histórico de assinaturas digitais

## 📦 Estrutura do Projeto

```text
az-204/
├── README.md                    # Este arquivo
├── az-204.sln                  # Solution .NET
├── docs/                       # Documentação adicional
│   ├── az204-security-project.md      # Documentação técnica detalhada do projeto
│   ├── desenvolvimento-local.md       # Guia completo de setup local
│   ├── troubleshooting-auth.md       # Resolução de problemas de autenticação
│   ├── cosmos-db-reference.md        # Referência e estrutura do Cosmos DB
│   ├── azure-storage-troubleshooting.md  # Troubleshooting do Azure Storage
│   └── digital-signatures-azure-keyvault.md  # Implementação de assinaturas digitais
├── src/
│   ├── SecureDocManager.API/   # Backend ASP.NET Core
│   └── SecureDocManager.Web/   # Frontend React
└── README-INTEGRATION.md       # Instruções de integração dos componentes
```

## 🚀 Como Executar

### Pré-requisitos

- .NET 9.0 SDK
- Node.js 18+
- Azure Subscription (para recursos cloud)
- Visual Studio 2022 ou VS Code

### Setup Local

1. **Clone o repositório**

```bash
git clone https://github.com/seu-usuario/az-204.git
cd az-204
```

2. **Configure as variáveis de ambiente**

Backend (`src/SecureDocManager.API/appsettings.Development.json`):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "seu-dominio.com",
    "TenantId": "seu-tenant-id",
    "ClientId": "seu-client-id"
  }
}
```

Frontend (`src/SecureDocManager.Web/.env`):

```env
VITE_AZURE_CLIENT_ID=seu-spa-client-id
VITE_AZURE_TENANT_ID=seu-tenant-id
VITE_REDIRECT_URI=http://localhost:5173
VITE_API_BASE_URL=https://localhost:7000/api
```

3. **Execute o Backend**

```bash
cd src/SecureDocManager.API
dotnet restore
dotnet ef database update
dotnet run
```

4. **Execute o Frontend**

```bash
cd src/SecureDocManager.Web
npm install
npm run dev
```

## 📚 Conceitos AZ-204 Demonstrados

✅ **Implementar autenticação e autorização**

- Microsoft Identity Platform
- OAuth 2.0 e OpenID Connect
- Controle de acesso baseado em roles (RBAC)

✅ **Proteger dados com Azure Key Vault**

- Armazenamento de secrets e certificados
- Managed Identity para acesso seguro
- Rotação de secrets

✅ **Implementar Managed Identities**

- System-assigned identity
- Acesso a recursos sem credenciais
- Integração com Azure Services

✅ **Shared Access Signatures (SAS)**

- Acesso temporário a blobs
- Diferentes níveis de permissão
- URLs seguras com expiração

✅ **Microsoft Graph Integration**

- Obter informações de usuários
- Listar membros da organização
- Permissões delegadas

## 🎯 Aprendizados e Resultados

Este projeto me permitiu:

- Compreender profundamente a integração entre serviços Azure
- Implementar padrões de segurança enterprise-ready
- Ganhar experiência prática com tecnologias cobradas no AZ-204
- Criar um portfólio demonstrável de competências Azure

## 🤝 Contribuições

Este é um projeto de estudos, mas sugestões e melhorias são bem-vindas! Sinta-se à vontade para:

- Abrir issues com dúvidas ou sugestões
- Fazer fork para seus próprios estudos
- Compartilhar experiências de aprendizado

## 📄 Licença

Este projeto é open source e está disponível sob a licença MIT.

---

**Desenvolvido por:** Eriel Miquilino com muito Vibe Coding
**Objetivo:** Preparação para certificação Microsoft Azure AZ-204  
**Status:** ✅ Completo e funcional
