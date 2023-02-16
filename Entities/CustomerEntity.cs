namespace ExcelAndElasticSearch.API.Entities;
public class CustomerEntity
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
}
