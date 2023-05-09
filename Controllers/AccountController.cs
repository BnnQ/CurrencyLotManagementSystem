using System.Security.Claims;
using AutoMapper;
using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.Filters;
using CurrencyLotManagementSystem.Utils.Extensions;
using CurrencyLotManagementSystem.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyLotManagementSystem.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signManager;
    private readonly IMapper mapper;

    public AccountController(UserManager<User> userManager, SignInManager<User> signManager, IMapper mapper)
    {
        this.userManager = userManager;
        this.signManager = signManager;
        this.mapper = mapper;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationViewModel registrationViewModel, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return View(registrationViewModel);
        }

        var user = mapper.Map<User>(registrationViewModel);
        user.LockoutEnabled = false;
        var password = registrationViewModel.Password;
        var registrationResult = await userManager.CreateAsync(user, password);

        if (registrationResult.Succeeded)
        {
            await signManager.SignInAsync(user, isPersistent: false);
            return !string.IsNullOrWhiteSpace(returnUrl) ? RedirectToLocal(returnUrl) : RedirectToHome();
        }

        TryAddModelErrorsFromResult(registrationResult);
        return View(registrationViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    [RetrieveModelErrorsFromRedirector]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel)
    {
        if (string.IsNullOrWhiteSpace(loginViewModel.UserName))
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.UserName),
                errorMessage: "The nickname field is required.");
        }

        if (string.IsNullOrWhiteSpace(loginViewModel.Password))
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Password),
                errorMessage: "The password field is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(loginViewModel);
        }

        var user = await userManager.FindByNameAsync(loginViewModel.UserName);
        if (user is null)
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.UserName),
                errorMessage: "No user found with this nickname.");
            return View(loginViewModel);
        }

        var result = await signManager.PasswordSignInAsync(
            user, loginViewModel.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return RedirectToAction(controllerName: "Lot", actionName: nameof(LotController.List));
        }

        string errorMessage;
        if (await userManager.IsLockedOutAsync(user))
        {
            errorMessage =
                "Your account has been blocked due to a high number of failed login attempts.";
        }
        else
        {
            const int MaxFailedAccessAttempts = 5;
            int remainingAccessTries =
                MaxFailedAccessAttempts - await userManager.GetAccessFailedCountAsync(user);

            errorMessage = remainingAccessTries > 3
                ? "Wrong password. Please try again."
                : $"Wrong password. Remaining tries: {remainingAccessTries}";
        }

        ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Password), errorMessage);
        return View(loginViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentNullException(nameof(provider));

        var redirectUrl = Url.Action(action: nameof(ExternalLoginCallback), controller: "Account",
            values: new { returnUrl });

        var properties = signManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    [KeepModelErrorsOnRedirect]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
    {
        var info = await signManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return RedirectToLoginForcibly(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
        var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User { UserName = email, Email = email, Surname = surname, LockoutEnabled = false };

            await userManager.CreateAsync(user);
        }

        await userManager.AddLoginAsync(user, info);
        var result = await signManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        return result.Succeeded ? RedirectToLocal(returnUrl) : RedirectToLoginForcibly(returnUrl);
    }

    [HttpGet]
    [Authorize(policy: "Authenticated")]
    public async Task<IActionResult> Logout()
    {
        await signManager.SignOutAsync();
        return RedirectToHome();
    }

    #region Utils

    private IActionResult RedirectToHome()
    {
        return RedirectToAction(controllerName: "Lot", actionName: nameof(LotController.List));
    }

    private IActionResult RedirectToLocal(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
        {
            return Redirect(url);
        }
        else
        {
            return RedirectToHome();
        }
    }

    private IActionResult RedirectToLoginForcibly(string returnUrl)
    {
        ModelState.AddSummaryError("Something went wrong.");
        return RedirectToAction(actionName: nameof(Login), routeValues: new { returnUrl });
    }

    private void TryAddModelErrorsFromResult(IdentityResult? result)
    {
        if (result?.Errors.Any() is true)
        {
            foreach (var error in result.Errors)
                ModelState.AddSummaryError(error.Description);
        }
    }

    #endregion
}