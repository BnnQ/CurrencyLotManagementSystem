using System.ComponentModel.DataAnnotations;

namespace CurrencyLotManagementSystem.ViewModels.Account;

public class RegistrationViewModel
{
    [Required(ErrorMessage = "Please enter an user name.")]
    [Display(Name = "User name")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Please enter an email.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Please enter a password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = null!;
    
    [Required(ErrorMessage = "Please enter a surname.")]
    [Display(Name = "Surname")]
    public string Surname { get; set; } = null!;
}