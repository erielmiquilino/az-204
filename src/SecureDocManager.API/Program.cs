using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Identity.Web;
using SecureDocManager.API.Data;
using SecureDocManager.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Azure App Configuration
builder.Configuration.AddAzureAppConfiguration(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("AppConfig");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.Connect(connectionString)
               .UseFeatureFlags();
    }
    else
    {
        // Para desenvolvimento local, usar appsettings.json
        Console.WriteLine("App Configuration não configurado. Usando configurações locais.");
    }
});

// Configurar Key Vault
var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
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
        clientBuilder.AddSecretClient(new Uri(keyVaultUrl));
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
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Configurar Microsoft Graph
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd")
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

// Configurar Authorization com políticas baseadas em roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager", "Admin"));
    options.AddPolicy("AllEmployees", policy => policy.RequireRole("Employee", "Manager", "Admin"));
});

// Configurar Entity Framework Core com SQL Database
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlDatabase") 
    ?? builder.Configuration["DatabaseConnectionString"];

if (!string.IsNullOrEmpty(sqlConnectionString))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlConnectionString));
}

// Configurar Cosmos DB
var cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosDB") 
    ?? builder.Configuration["CosmosDBConnectionString"];

if (!string.IsNullOrEmpty(cosmosConnectionString))
{
    builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
    {
        return new CosmosClient(cosmosConnectionString);
    });
}

// Registrar serviços da aplicação
builder.Services.AddScoped<IKeyVaultService, KeyVaultService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<ICosmosService, CosmosService>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder => builder
            .WithOrigins("http://localhost:3000") // React app URL
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

app.Run();
