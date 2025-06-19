# Solução de Problemas com Azure Storage

## Problema de Conectividade

O erro `System.Net.Http.HttpIOException: The response ended prematurely` indica problemas de conectividade com o Azure Storage. Este documento apresenta soluções para resolver esses problemas.

## Soluções Implementadas

### 1. Melhorias no DocumentService

- **Retry Policy Personalizado**: Implementado retry manual com backoff exponencial
- **Timeout Reduzido**: Configurado para falhar mais rapidamente em caso de problemas
- **Validação de Parâmetros**: Adicionada validação antes de tentar upload
- **Logging Detalhado**: Melhor rastreamento de erros

### 2. Configuração para Desenvolvimento Local

#### Opção A: Azure Storage Emulator (Azurite)

1. **Instalar Azurite**:

   ```bash
   npm install -g azurite
   ```

2. **Iniciar o Azurite**:

   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. **String de Conexão (já configurada no appsettings.Development.json)**:

   ```json
   "StorageConnectionString": "UseDevelopmentStorage=true"
   ```

#### Opção B: Azure Storage Account Real

1. **Criar Storage Account no Azure Portal**
2. **Obter a Connection String**
3. **Configurar no Key Vault ou appsettings**

### 3. Verificação de Conectividade

#### Teste Manual do Storage

```csharp
// No Program.cs ou em um controller de teste
app.MapGet("/test-storage", async (IDocumentService documentService) =>
{
    try
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("teste"));
        var doc = await documentService.UploadDocumentAsync(stream, "teste.txt", "user123", "dept456");
        return Results.Ok($"Upload bem-sucedido: {doc.Id}");
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Erro: {ex.Message}");
    }
});
```

### 4. Configurações de Rede

#### Proxy e Firewall

Se estiver atrás de um proxy corporativo:

1. **Desabilitar Proxy para Storage Emulator**:

   ```csharp
   // No Program.cs
   builder.Services.AddHttpClient("AzureStorage", client =>
   {
       client.Timeout = TimeSpan.FromMinutes(10);
   })
   .ConfigurePrimaryHttpMessageHandler(() =>
   {
       return new HttpClientHandler()
       {
           UseProxy = false // Desabilitar proxy
       };
   });
   ```

2. **Configurar Exceções no Firewall**:
   - Azurite: Portas 10000, 10001, 10002
   - Azure Storage: HTTPS (443)

### 5. Logs e Diagnóstico

#### Logs Detalhados

O DocumentService agora gera logs detalhados:

```text
info: Iniciando upload do documento arquivo.pdf para o usuário user123
warn: Tentativa 1 de 3 falhou ao obter container 'documents'
info: Container 'documents' criado com sucesso
info: Upload concluído com sucesso para arquivo.pdf na tentativa 1
info: Documento 123 salvo no banco de dados
```

#### Verificar Logs no Azure

Se usando Azure Storage real:

1. Habilitar logging no Storage Account
2. Verificar métricas no Azure Monitor

### 6. Alternativas de Fallback

#### Storage Local (Para Desenvolvimento)

```csharp
// Implementar um FileSystemDocumentService para desenvolvimento
public class FileSystemDocumentService : IDocumentService
{
    // Salvar arquivos localmente no sistema de arquivos
}
```

### 7. Monitoramento em Produção

#### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddAzureBlobStorage(connectionString, "documents");
```

#### Métricas Customizadas

```csharp
// Instrumentar o DocumentService com métricas
services.AddSingleton<IMetrics, Metrics>();
```

## Comandos Úteis

### Reiniciar Azurite

```bash
# Parar processos existentes
taskkill /f /im node.exe

# Iniciar novamente
azurite --silent --location c:\azurite
```

### Verificar Conectividade

```powershell
# Testar conectividade HTTP
Invoke-WebRequest -Uri "http://127.0.0.1:10000" -Method GET
```

### Limpar Cache

```bash
# Limpar container local
azurite --silent --location c:\azurite --blobHost 127.0.0.1 --blobPort 10000
```

## Troubleshooting Específico

### Erro: "Container not found"

- Verificar se o Azurite está rodando
- Verificar string de conexão
- Criar container manualmente via Storage Explorer

### Erro: "Authentication failed"

- Verificar credenciais no Key Vault
- Validar Managed Identity em produção

### Erro: "Network timeout"

- Aumentar timeout no HttpClient
- Verificar conectividade de rede
- Considerar usar retry policy mais agressivo
