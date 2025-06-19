# Migração para CosmosDB - SecureDocManager

## Por que migrar para CosmosDB?

1. **Simplicidade**: Uma única tecnologia de banco de dados
2. **Escalabilidade Global**: CosmosDB é distribuído globalmente por padrão
3. **Performance**: Latência garantida < 10ms
4. **Certificação AZ-204**: CosmosDB é uma tecnologia chave no exame

## Análise de Entidades

### Entidades Atuais no SQL Server

- **Users**: Informações dos usuários
- **Documents**: Metadados dos documentos
- **AuditLogs**: Logs de auditoria

### Design para CosmosDB

#### Container: users

```json
{
  "id": "user-guid",
  "partitionKey": "user-guid",
  "type": "user",
  "displayName": "Nome do Usuário",
  "email": "user@example.com",
  "department": "IT",
  "jobTitle": "Developer",
  "role": "Employee",
  "isActive": true,
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2024-01-01T00:00:00Z"
}
```

#### Container: documents

```json
{
  "id": "doc-guid",
  "partitionKey": "department-id",
  "type": "document",
  "fileName": "document.pdf",
  "fileExtension": ".pdf",
  "fileSize": 1024000,
  "blobStorageUrl": "https://...",
  "departmentId": "IT",
  "uploadedByUserId": "user-guid",
  "uploadedByUserName": "Nome do Usuário",
  "uploadedDate": "2024-01-01T00:00:00Z",
  "isDeleted": false,
  "tags": ["confidential", "project-x"]
}
```

#### Container: audit

```json
{
  "id": "audit-guid",
  "partitionKey": "2024-01", // Particionar por mês
  "type": "audit",
  "userId": "user-guid",
  "userName": "Nome do Usuário",
  "documentId": "doc-guid",
  "action": "DocumentViewed",
  "timestamp": "2024-01-01T00:00:00Z",
  "ipAddress": "192.168.1.1",
  "details": {}
}
```

## Vantagens da Estrutura Proposta

1. **Particionamento Eficiente**:
   - Users: Por userId (consultas individuais)
   - Documents: Por departmentId (consultas departamentais)
   - Audit: Por mês (consultas temporais)

2. **Consultas Otimizadas**:
   - Buscar documentos por departamento
   - Logs de auditoria por período
   - Perfil de usuário direto

3. **Desnormalização Controlada**:
   - Nome do usuário no documento (evita JOINs)
   - Informações essenciais duplicadas para performance

## Próximos Passos

1. Deseja prosseguir com a migração completa para CosmosDB?
2. Ou prefere manter SQL Server e apenas criar as migrations?

A migração completa levará cerca de 30 minutos de refatoração.
