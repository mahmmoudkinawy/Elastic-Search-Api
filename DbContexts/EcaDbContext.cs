using ExcelAndElasticSearch.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExcelAndElasticSearch.API.DbContexts;
public class EcaDbContext : DbContext
{
    public EcaDbContext(DbContextOptions<EcaDbContext> options) : base(options)
    { }

    public DbSet<CustomerEntity> Customers { get; set; }

}
