using ExcelAndElasticSearch.API.DbContexts;
using ExcelAndElasticSearch.API.Entities;
using ExcelAndElasticSearch.API.Services;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EcaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

//Elastic search
var uri = builder.Configuration["ElasticSearch:Uri"];
var defaultIndex = builder.Configuration["ElasticSearch:Index"];

var settings = new ConnectionSettings(new Uri(uri)).PrettyJson().DefaultIndex(defaultIndex);

//settings.DefaultMappingFor<CustomerEntity>(c => c.Ignore(_ => _.Id));

var client = new ElasticClient(settings);

builder.Services.AddSingleton<IElasticClient>(client);

await client.Indices.CreateAsync(defaultIndex, i => i.Map<CustomerEntity>(x => x.AutoMap()));

builder.Services.AddHostedService<CustomerService>();

////

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<EcaDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
try
{
    await dbContext.Database.MigrateAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occured while applying migrations");
}
app.Run();
