using ExcelAndElasticSearch.API.DbContexts;
using Nest;

namespace ExcelAndElasticSearch.API.Services;

public class CustomerService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly IElasticClient _elasticClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        IConfiguration config,
        IElasticClient elasticClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CustomerService> logger)
    {
        _config = config ??
            throw new ArgumentNullException(nameof(config));
        _elasticClient = elasticClient ??
            throw new ArgumentNullException(nameof(elasticClient));
        _serviceScopeFactory = serviceScopeFactory ??
            throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThreadPool.QueueUserWorkItem(async _ =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<EcaDbContext>();

                var customers = context.Customers.AsQueryable();

                var bulkIndexResponse = await _elasticClient.BulkAsync(b => b
                    .Index(_config["ElasticSearch:Index"])
                    .IndexMany(customers, (bi, customer) => bi.Document(customer)), cancellationToken);

                _logger.LogInformation("Customers synced successfully to ElasticSearch");

                if (bulkIndexResponse.Errors)
                {
                    _logger.LogError("There exist and error in sync data");
                    throw new Exception("Something went wrong");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Customer Servive Shutdown");
        return Task.CompletedTask;
    }
}
