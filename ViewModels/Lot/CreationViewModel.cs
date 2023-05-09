using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CurrencyLotManagementLibrary.Enums;

namespace CurrencyLotManagementSystem.ViewModels.Lot;

public class CreationViewModel
{
    [Required]
    public Currency Currency { get; set; }
    
    [Required]
    [Range(0, 1000000000)]
    public double Amount { get; set; }

    [Required]
    [DisplayName("Lot expiration time")]
    public TimeSpan ExpirationTime { get; set; }
    
}