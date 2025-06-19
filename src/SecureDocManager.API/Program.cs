using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Identity.Web;
using SecureDocManager.API.Data;
using SecureDocManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Azure App Configuration primeiro para obter as configurações base
var appConfigConnectionString = builder.Configuration.GetConnectionString("AppConfig");
if (!string.IsNullOrEmpty(appConfigConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnectionString)
               .ConfigureKeyVault(kv =>
               {
                   kv.SetCredential(new DefaultAzureCredential());
               })
               .Select(KeyFilter.Any, LabelFilter.Null)
               .Select(KeyFilter.Any, builder.Environment.EnvironmentName)
               .UseFeatureFlags()
               .ConfigureRefresh(refresh =>
               {
                   refresh.Register("Sentinel", refreshAll: true)
                          .SetRefreshInterval(TimeSpan.FromMinutes(5));
               });
    });
}
else
{
    Console.WriteLine("App Configuration não configurado. Usando configurações locais.");
}

// Configurar Key Vault para connection strings
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
Console.WriteLine($"Key Vault URL: {keyVaultUrl}");

if (!string.IsNullOrEmpty(keyVaultUrl))
{
    try
    {
        // Configurar DefaultAzureCredential para desenvolvimento local
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeVisualStudioCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeEnvironmentCredential = false,
            ExcludeInteractiveBrowserCredential = !builder.Environment.IsDevelopment()
        };
        
        var credential = new DefaultAzureCredential(credentialOptions);
        var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        builder.Services.AddSingleton(secretClient);

        Console.WriteLine("Tentando conectar ao Key Vault...");
        
        // Buscar connection strings do Key Vault de forma síncrona durante a inicialização
        try
        {
            // SQL Database Connection String
            Console.WriteLine("Buscando DatabaseConnectionString...");
            var sqlConnectionSecret = secretClient.GetSecret("DatabaseConnectionString");
            builder.Configuration["ConnectionStrings:SqlDatabase"] = sqlConnectionSecret.Value.Value;
            Console.WriteLine("✓ DatabaseConnectionString obtida com sucesso");
            
            // Cosmos DB Connection String
            Console.WriteLine("Buscando CosmosDBConnectionString...");
            var cosmosConnectionSecret = secretClient.GetSecret("CosmosDBConnectionString");
            builder.Configuration["ConnectionStrings:CosmosDB"] = cosmosConnectionSecret.Value.Value;
            Console.WriteLine("✓ CosmosDBConnectionString obtida com sucesso");
            
            // Storage Connection String
            Console.WriteLine("Buscando StorageConnectionString...");
            var storageConnectionSecret = secretClient.GetSecret("StorageConnectionString");
            builder.Configuration["ConnectionStrings:Storage"] = storageConnectionSecret.Value.Value;
            Console.WriteLine("✓ StorageConnectionString obtida com sucesso");
            
            Console.WriteLine("🎉 Todas as connection strings foram obtidas do Key Vault com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao buscar secrets do Key Vault: {ex.Message}");
            Console.WriteLine($"Detalhes: {ex}");
            
            // Em desenvolvimento, tentar usar valores locais como fallback
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine("⚠️ Usando connection strings locais como fallback...");
                // As connection strings locais serão usadas dos appsettings.Development.json
            }
            else
            {
                throw; // Em produção, falhar se não conseguir acessar o Key Vault
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro crítico ao configurar Key Vault: {ex.Message}");
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("⚠️ Continuando em modo de desenvolvimento sem Key Vault...");
            // Registrar um SecretClient nulo para satisfazer a injeção de dependência
            builder.Services.TryAddSingleton<SecretClient>(provider => null!);
        }
        else
        {
            throw;
        }
    }
}
else
{
    Console.WriteLine("⚠️ Key Vault URL não configurada. Usando configurações locais.");
    // Registrar um SecretClient nulo para satisfazer a injeção de dependência em desenvolvimento
    builder.Services.TryAddSingleton<SecretClient>(provider => null!);
}

// Add services to the container.
builder.Services.AddControllers();

// Configurar Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar Managed Identity e Azure Clients
builder.Services.AddAzureClients(clientBuilder =>
{
    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        // O SecretClient já foi registrado, não precisa adicionar aqui de novo
        // clientBuilder.AddSecretClient(new Uri(keyVaultUrl));
    }
    
    var appConfigUrl = builder.Configuration["AppConfiguration:Url"];
    if (!string.IsNullOrEmpty(appConfigUrl))
    {
        clientBuilder.AddConfigurationClient(new Uri(appConfigUrl));
    }
    
    clientBuilder.UseCredential(new DefaultAzureCredential());
});

// Configurar Authentication com Microsoft Entra ID
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration, "AzureAd");

// Configurar Authorization com políticas baseadas em roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("AllEmployees", policy => policy.RequireRole("Employee", "Manager", "Admin"));
});

// Configurar Entity Framework Core com SQL Database
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase");
if (!string.IsNullOrEmpty(sqlConnectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlConnectionString));
}
else if (builder.Environment.IsDevelopment())
{
    // Usar LocalDB em desenvolvimento se não houver connection string
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration["DatabaseConnectionString"]));
}

// Configurar Cosmos DB
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDB");
if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
    {
        return new CosmosClient(cosmosConnectionString);
    });
}
else if (builder.Environment.IsDevelopment())
{
    // Usar emulador local em desenvolvimento
    var localCosmosConnection = builder.Configuration["CosmosDBConnectionString"];
    if (!string.IsNullOrEmpty(localCosmosConnection))
    {
        builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            return new CosmosClient(localCosmosConnection);
        });
    }
}

// Configure HttpClient with timeout and retry policies
builder.Services.AddHttpClient("AzureStorage", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10); // Aumentar timeout para uploads grandes
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler()
    {
        MaxConnectionsPerServer = 10,
        UseProxy = false // Desabilitar proxy se estiver causando problemas
    };
});

// Registrar serviços da aplicação
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IDocumentService>(provider =>
{
    var context = provider.GetRequiredService<ApplicationDbContext>();
    var keyVaultService = provider.GetRequiredService<IKeyVaultService>();
    var cosmosService = provider.GetRequiredService<ICosmosService>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<DocumentService>>();
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AzureStorage");
    
    return new DocumentService(context, keyVaultService, cosmosService, configuration, logger, httpClient);
});
// Temporariamente comentado até resolver o problema do Microsoft Graph
// builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<ICosmosService, CosmosService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDocumentSigningService, DocumentSigningService>();

// Registrar BlobServiceClient
builder.Services.AddSingleton<Azure.Storage.Blobs.BlobServiceClient>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    
    // Tentar obter a connection string do Configuration (já preenchida do Key Vault)
    var storageConnectionString = configuration.GetConnectionString("Storage");
    
    // Se não encontrar, tentar a configuração local
    if (string.IsNullOrEmpty(storageConnectionString))
    {
        storageConnectionString = configuration["StorageConnectionString"];
    }
    
    if (string.IsNullOrEmpty(storageConnectionString))
    {
        throw new InvalidOperationException("Storage connection string não configurada");
    }
    
    return new Azure.Storage.Blobs.BlobServiceClient(storageConnectionString);
});

// Registrar um mock temporário do GraphService
builder.Services.AddScoped<IGraphService>(provider => new MockGraphService());

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins("http://localhost:3000", "http://localhost:5173") // React app URLs
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Aplicar migrações automaticamente ao inicializar
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
        Console.WriteLine("✅ Migrações do banco de dados aplicadas com sucesso.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao aplicar as migrações do banco de dados: {ex.Message}");
        // Em um ambiente de produção, você pode querer logar isso com mais detalhes
        // e talvez impedir a aplicação de iniciar se o banco de dados não estiver acessível/atualizado.
    }
}

app.Run();
