# Configuração para Desenvolvimento Local com Key Vault

## 🎯 Objetivo

Este guia explica como configurar o ambiente de desenvolvimento local para acessar o Azure Key Vault real, buscando as connection strings diretamente do Azure.

## 📋 Pré-requisitos

1. **Azure CLI instalado**
2. **Permissões no Key Vault** (conforme configurado no projeto AZ-204)
3. **Visual Studio ou VS Code** (para autenticação automática)

## 🔧 Configuração Passo a Passo

### 1. Instalar Azure CLI

```bash
# Windows (via winget)
winget install Microsoft.AzureCli

# Ou baixar do site oficial
# https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
```

### 2. Fazer Login no Azure CLI

```bash
# Login interativo
az login

# Verificar se logou corretamente
az account show

# Definir a subscription correta (se necessário)
az account set --subscription "2e894cd1-92d7-4c3a-8282-abd1b455b834"
```

### 3. Verificar Permissões no Key Vault

```bash
# Testar acesso ao Key Vault
az keyvault secret list --vault-name kv-securedocmanager

# Testar leitura de um secret específico
az keyvault secret show --vault-name kv-securedocmanager --name StorageConnectionString
```

### 4. Configurar Visual Studio (Opcional)

Se estiver usando Visual Studio:

1. Vá em **Tools** → **Options**
2. Navegue para **Azure Service Authentication**
3. Selecione a conta correta
4. Teste a conexão

### 5. Verificar Configuração da Aplicação

O `appsettings.Development.json` já está configurado com:

```json
{
  "KeyVault": {
    "Url": "https://kv-securedocmanager.vault.azure.net/"
  }
}
```

## 🚀 Como Funciona

### DefaultAzureCredential Chain

A aplicação usa `DefaultAzureCredential` que tenta as seguintes autenticações em ordem:

1. **Environment Variables** - Se `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, etc. estiverem definidas
2. **Managed Identity** - Para execução no Azure
3. **Visual Studio** - Se logado no VS
4. **Azure CLI** - Se `az login` foi executado
5. **VS Code** - Se logado no VS Code
6. **Interactive Browser** - Como último recurso (apenas em dev)

### Logs Detalhados

A aplicação agora exibe logs detalhados durante a inicialização:

```
Key Vault URL: https://kv-securedocmanager.vault.azure.net/
Tentando conectar ao Key Vault...
Buscando SqlDatabaseConnectionString...
✓ SqlDatabaseConnectionString obtida com sucesso
Buscando CosmosDBConnectionString...
✓ CosmosDBConnectionString obtida com sucesso
Buscando StorageConnectionString...
✓ StorageConnectionString obtida com sucesso
🎉 Todas as connection strings foram obtidas do Key Vault com sucesso!
```

## 🛠️ Troubleshooting

### Erro: "Forbidden" ou "Access Denied"

**Problema**: Sem permissões no Key Vault
**Solução**: 
```bash
# Verificar se seu usuário tem as permissões corretas
az keyvault show --name kv-securedocmanager --query "properties.accessPolicies"

# Se necessário, adicionar permissões (como administrador)
az keyvault set-policy --name kv-securedocmanager \
    --upn SEU-EMAIL@ndd.com.br \
    --secret-permissions get list
```

### Erro: "Authentication Failed"

**Problema**: Azure CLI não está logado ou expirou
**Solução**:
```bash
# Fazer login novamente
az login --tenant 717144d2-6d9f-42a1-b56d-42afc3753ec3

# Verificar se está logado
az account show
```

### Erro: "Key Vault não encontrado"

**Problema**: URL do Key Vault incorreta ou recurso não existe
**Solução**:
```bash
# Verificar se o Key Vault existe
az keyvault show --name kv-securedocmanager

# Listar todos os Key Vaults na subscription
az keyvault list --query "[].{Name:name, ResourceGroup:resourceGroup}"
```

### Fallback para Configurações Locais

Se o Key Vault não estiver acessível, a aplicação automaticamente usa as configurações locais do `appsettings.Development.json`:

```
❌ Erro ao buscar secrets do Key Vault: [erro detalhado]
⚠️ Usando connection strings locais como fallback...
```

## 📊 Vantagens desta Configuração

### ✅ Para o Projeto de Estudos AZ-204

1. **Experiência Real**: Usa os serviços Azure reais
2. **Autenticação Prática**: Explora diferentes métodos de autenticação
3. **Segurança**: Não expõe credentials no código
4. **Resiliência**: Fallback automático para desenvolvimento

### ✅ Para Desenvolvimento

1. **Sem Configuração Manual**: Connection strings vêm automaticamente do Azure
2. **Ambiente Consistente**: Mesmo comportamento entre dev/prod
3. **Fácil Debugging**: Logs detalhados de todo o processo
4. **Flexibilidade**: Funciona online e offline (com fallback)

## 🔄 Workflow de Desenvolvimento

1. **Primeira vez**:
   ```bash
   az login
   # A aplicação automaticamente busca do Key Vault
   ```

2. **Desenvolvimento normal**:
   ```bash
   dotnet run
   # Conexões automáticas com Azure services
   ```

3. **Offline/Problemas**:
   ```
   # Aplicação automaticamente usa fallback local
   # Continua funcionando normalmente
   ```

## 🎯 Testando a Configuração

Execute os endpoints de teste para verificar se tudo está funcionando:

```bash
# Testar conexão com Key Vault
curl http://localhost:5235/api/test/storage-connection

# Testar upload real para Azure Storage
curl -X POST http://localhost:5235/api/test/storage-upload

# Verificar diagnósticos de rede
curl http://localhost:5235/api/test/network-diagnostics
```

## 📝 Próximos Passos

1. ✅ **Configure o Azure CLI** (`az login`)
2. ✅ **Execute a aplicação** (`dotnet run`)
3. ✅ **Verifique os logs** para conexão com Key Vault
4. ✅ **Teste o upload** usando os endpoints de teste
5. ✅ **Desenvolva normalmente** com Azure services reais

Esta configuração proporciona uma experiência de desenvolvimento autêntica usando os recursos Azure reais, perfeita para o aprendizado prático da certificação AZ-204! 🚀 