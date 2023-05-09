using AutoMapper;
using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.ViewModels.Lot;

namespace CurrencyLotManagementSystem.Services.MapperProfiles;

public class LotCreationProfile : Profile
{
    public LotCreationProfile()
    {
        CreateMap<CreationViewModel, Lot>();
    }
}