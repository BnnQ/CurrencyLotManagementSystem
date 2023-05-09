using CurrencyLotManagementLibrary.Enums;
using CurrencyLotManagementLibrary.Models.Entities;

namespace CurrencyLotManagementSystem.Services.Abstractions;

public interface ILotManager
{
    public Task AddAsync(Lot lot);

    public Task<IEnumerable<Lot>> GetByCurrencyAsync(Currency currency);

    public Task DeleteAsync(string id, string popReceipt);

}