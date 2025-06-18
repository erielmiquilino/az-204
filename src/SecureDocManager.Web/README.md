# SecureDocManager - Frontend

## 📋 Descrição

Frontend do sistema de gestão de documentos corporativos desenvolvido com React, TypeScript e Material-UI. Este projeto faz parte do estudo para certificação Azure AZ-204, focando em implementação de segurança com Microsoft Entra ID (Azure AD).

## 🚀 Tecnologias Utilizadas

- **React 19** com TypeScript
- **Vite** para build e desenvolvimento
- **Material-UI (MUI)** para componentes de UI
- **MSAL React** para autenticação com Azure AD
- **React Router** para navegação
- **Axios** para chamadas HTTP
- **React Hot Toast** para notificações
- **Date-fns** para manipulação de datas

## 📦 Instalação

1. Clone o repositório
2. Navegue até a pasta do projeto:

   ```bash
   cd src/SecureDocManager.Web
   ```

3. Instale as dependências:

   ```bash
   npm install
   ```

4. Configure as variáveis de ambiente:
   - Crie um arquivo `.env` na raiz do projeto
   - Adicione as seguintes configurações:

   ```env
   # Azure AD Configuration
   VITE_AZURE_CLIENT_ID=your-spa-client-id-here
   VITE_AZURE_TENANT_ID=your-tenant-id-here
   VITE_REDIRECT_URI=http://localhost:3000
   
   # API Configuration
   VITE_API_BASE_URL=https://localhost:7000/api
   ```

## ⚙️ Configuração do Azure AD

1. No portal Azure, registre uma aplicação SPA
2. Configure as URLs de redirecionamento:
   - `http://localhost:3000` (desenvolvimento)
   - URL de produção quando disponível
3. Configure as permissões necessárias:
   - User.Read
   - api://securedocmanager-api/Documents.Read

## 🏃‍♂️ Executando o Projeto

### Desenvolvimento

```bash
npm run dev
```

### Build para produção

```bash
npm run build
```

### Preview da build

```bash
npm run preview
```

## 🔧 Estrutura do Projeto

```text
src/
├── components/       # Componentes reutilizáveis
├── config/          # Configurações (MSAL, API, etc)
├── contexts/        # Contextos React (Auth, etc)
├── hooks/           # Custom hooks
├── pages/           # Páginas da aplicação
├── routes/          # Configuração de rotas
├── services/        # Serviços de API
├── types/           # Tipos TypeScript
└── utils/           # Utilitários
```

## 🔐 Funcionalidades de Segurança

- **Autenticação SSO** com Microsoft Entra ID
- **Autorização baseada em roles** (Admin, Manager, Employee)
- **Tokens de acesso** gerenciados automaticamente
- **Refresh tokens** para sessões longas
- **Proteção de rotas** baseada em permissões

## 📱 Páginas Implementadas

- **Dashboard**: Visão geral do sistema
- **Documentos**: Listagem e gerenciamento de documentos
- **Upload**: Upload de novos documentos
- **Usuários**: Gerenciamento de usuários (Admin)
- **Auditoria**: Logs de atividades (Admin/Manager)
- **Configurações**: Configurações do sistema (Admin)
- **Perfil**: Informações do usuário logado

## 🎨 Design e UX

- Interface moderna e responsiva
- Tema customizado com cores do Azure
- Navegação intuitiva com drawer lateral
- Feedback visual para todas as ações
- Suporte a temas claro/escuro (futuro)

## 🚧 Próximas Implementações

- [ ] Upload de documentos com drag-and-drop
- [ ] Visualização de documentos inline
- [ ] Busca avançada com filtros
- [ ] Download em lote
- [ ] Assinatura digital de documentos
- [ ] Integração com Microsoft Graph
- [ ] Dashboard com gráficos interativos
- [ ] Notificações em tempo real

## 📚 Documentação

Para mais informações sobre o projeto completo, consulte a documentação principal em `/docs/az204-security-project.md`

## 🤝 Contribuição

Este é um projeto de estudos para certificação AZ-204. Sugestões e melhorias são bem-vindas!
