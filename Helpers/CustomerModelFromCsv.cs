using CsvHelper.Configuration.Attributes;

namespace ExcelAndElasticSearch.API.Helpers;
public class CustomerModelFromCsv
{
    [Index(0)]
    public string Id { get; set; }

    [Index(1)]
    public string? Name { get; set; }

    [Index(2)]
    public string? Address { get; set; }

    [Index(3)]
    public string? Email { get; set; }

    [Index(4)]
    public string? Notes { get; set; }
}
