using AutoMapper;
using ExcelAndElasticSearch.API.Entities;

namespace ExcelAndElasticSearch.API.Helpers;
public class MappingProfiles : Profile
{
	public MappingProfiles()
	{
		CreateMap<CustomerModelFromCsv, CustomerEntity>();
	}
}
