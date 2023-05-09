using AutoMapper;
using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.ViewModels.Account;

namespace CurrencyLotManagementSystem.Services.MapperProfiles;

public class UserRegistrationProfile : Profile
{
    public UserRegistrationProfile()
    {
        CreateMap<RegistrationViewModel, User>();
    }
}