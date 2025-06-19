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
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new DefaultAzureCredential();
    var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
    builder.Services.AddSingleton(secretClient);

    // Buscar connection strings do Key Vault
    try
    {
        // SQL Database Connection String
        var sqlConnectionSecret = secretClient.GetSecret("SqlDatabaseConnectionString");
        builder.Configuration["ConnectionStrings:SqlDatabase"] = sqlConnectionSecret.Value.Value;
        
        // Cosmos DB Connection String
        var cosmosConnectionSecret = secretClient.GetSecret("CosmosDBConnectionString");
        builder.Configuration["ConnectionStrings:CosmosDB"] = cosmosConnectionSecret.Value.Value;
        
        // Storage Connection String
        var storageConnectionSecret = secretClient.GetSecret("StorageConnectionString");
        builder.Configuration["ConnectionStrings:Storage"] = storageConnectionSecret.Value.Value;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao buscar secrets do Key Vault: {ex.Message}");
        // Em desenvolvimento, usar valores locais se Key Vault não estiver disponível
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("Usando connection strings locais para desenvolvimento.");
        }
    }
}
else
{
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

// Registrar serviços da aplicação
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
// Temporariamente comentado até resolver o problema do Microsoft Graph
// builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<ICosmosService, CosmosService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDocumentSigningService, DocumentSigningService>();

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

// Aplicar migrações automaticamente em desenvolvimento
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            if (dbContext != null)
            {
                dbContext.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao aplicar migrações: {ex.Message}");
        }
    }
}

app.Run();
