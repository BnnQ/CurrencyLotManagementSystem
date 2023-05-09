using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CurrencyLotManagementSystem.ViewModels.Account;

public class LoginViewModel
{
    [Required]
    [DisplayName(displayName: "Nickname")]
    public string UserName { get; set; } = null!;
    
    [Required]
    [DataType(DataType.Password)]
    [DisplayName(displayName: "Password")]
    public string Password { get; set; } = null!;
}