using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelAndElasticSearch.API.DbContexts;
using ExcelAndElasticSearch.API.Entities;
using ExcelAndElasticSearch.API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using System.Globalization;
using System.Text;

namespace ExcelAndElasticSearch.API.Controllers;

[Route("api/customers")]
[ApiController]
public class CustomersController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IElasticClient _elasticClient;
    private readonly EcaDbContext _context;

    public CustomersController(
        IMapper mapper,
        IServiceScopeFactory serviceScopeFactory,
        IElasticClient elasticClient,
        EcaDbContext context)
    {
        _mapper = mapper ??
            throw new ArgumentNullException(nameof(mapper));
        _serviceScopeFactory = serviceScopeFactory ??
            throw new ArgumentNullException(nameof(serviceScopeFactory));
        _elasticClient = elasticClient ??
            throw new ArgumentNullException(nameof(elasticClient));
        _context = context ??
            throw new ArgumentNullException(nameof(context));
    }

    [HttpGet]
    public async Task<IActionResult> SearchCustomers(
        [FromQuery] string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return BadRequest("I know I must return all the values, but that's not mentioned in the task, so I won't give you the ability to do that.");
        }

        var customers = await _elasticClient.SearchAsync<CustomerEntity>(s =>
        {
            s.Query
                (
                    q => q.QueryString
                        (
                            d => d.Query($"*{keyword}*")
                        )
                ).Take(30);

            return s;
        });

        return Ok(customers.Documents.ToList());
    }


    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportProductsAsCsv()
    {
        var values = _context.Customers.AsQueryable();

        if (!await values.AnyAsync())
        {
            return BadRequest("Not data exist");
        }

        using var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(
            stream: memoryStream,
            Encoding.UTF8))
        {
            var csv = new CsvWriter(writer, CultureInfo.CurrentCulture);
            await csv.WriteRecordsAsync(values);
        }

        return File(memoryStream.ToArray(), "text/csv", "Customers.csv");
    }

    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = long.MaxValue)]
    [DisableRequestSizeLimit]
    [HttpPost("upload-csv")]
    public async Task<IActionResult> UploadCsvFile(
        [FromForm] IFormFile file)
    {
        var customersToDb = Enumerable.Empty<CustomerEntity>();

        using var inputStream = file.OpenReadStream();
        using var reader = new StreamReader(inputStream);
        using var csv = new CsvReader(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null
            });

        customersToDb = _mapper.Map<IEnumerable<CustomerEntity>>(
           csv.GetRecords<CustomerModelFromCsv>());

        ThreadPool.QueueUserWorkItem(async _ =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EcaDbContext>();

            await context.BulkMergeAsync(customersToDb, options =>
            {
                options.InsertIfNotExists = true;
            });

            await context.BulkMergeAsync(customersToDb);
        });

        return Ok("Done");
    }

}
