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
    private readonly ILogger<AccountController> logger;

    public AccountController(UserManager<User> userManager, SignInManager<User> signManager, IMapper mapper, ILoggerFactory loggerFactory)
    {
        this.userManager = userManager;
        this.signManager = signManager;
        this.mapper = mapper;
        logger = loggerFactory.CreateLogger<AccountController>();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        logger.LogInformation("[GET] Register: returning view");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationViewModel registrationViewModel, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("[POST] Register: model contains errors, returning view");
            return View(registrationViewModel);
        }

        var user = mapper.Map<User>(registrationViewModel);
        user.LockoutEnabled = false;
        var password = registrationViewModel.Password;
        var registrationResult = await userManager.CreateAsync(user, password);

        if (registrationResult.Succeeded)
        {
            await signManager.SignInAsync(user, isPersistent: false);
            logger.LogInformation("[POST] Register: successfully registered user {UserName}", user.UserName);
            return !string.IsNullOrWhiteSpace(returnUrl) ? RedirectToLocal(returnUrl) : RedirectToHome();
        }

        TryAddModelErrorsFromResult(registrationResult);
        logger.LogWarning("[POST] Register: registration result is not succeeded, returning view");
        return View(registrationViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    [RetrieveModelErrorsFromRedirector]
    public IActionResult Login()
    {
        logger.LogInformation("[GET] Login: returning view");
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
            logger.LogWarning("[POST] Login: model contains errors, returning view");
            return View(loginViewModel);
        }

        var user = await userManager.FindByNameAsync(loginViewModel.UserName);
        if (user is null)
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.UserName),
                errorMessage: "No user found with this nickname.");
            
            logger.LogWarning("[POST] Login: model contains errors, returning view");
            return View(loginViewModel);
        }

        var result = await signManager.PasswordSignInAsync(
            user, loginViewModel.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            logger.LogInformation("[POST] Login: successfully executed Login for user {UserName}", user.UserName);
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
        logger.LogWarning("[POST] Login: user {UserName} login result is not succeeded, returning view", user.UserName);
        return View(loginViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentNullException(nameof(provider));

        logger.LogInformation("[GET] ExternalLogin: executing external login request (provider: {Provider})", provider);
        
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
            logger.LogWarning("[GET] ExternalLoginCallback: external login failed for a third-party reason");
            return RedirectToLoginForcibly(returnUrl);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
        var surname = info.Principal.FindFirstValue(ClaimTypes.Surname);
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User { UserName = email, Email = email, Surname = surname, LockoutEnabled = false };

            await userManager.CreateAsync(user);
            logger.LogInformation("[GET] ExternalLoginCallback: successfully registered new user {UserName} through external login", user.UserName);
        }

        var addingLoginResult = await userManager.AddLoginAsync(user, info);
        if (addingLoginResult.Succeeded)
        {
            logger.LogInformation("[GET] ExternalLoginCallback: successfully added new login to user {UserName} through external login", user.UserName);
        }

        var result = await signManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("[GET] ExternalLoginCallback: successfully logged in user {UserName} through {LoginProvider} login", user.UserName, info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        
        logger.LogWarning("[GET] ExternalLoginCallback: user {UserName} external login result through {LoginProvider} is not succeeded, redirecting to login", user.UserName, info.LoginProvider);
        return RedirectToLoginForcibly(returnUrl);
    }

    [HttpGet]
    [Authorize(policy: "Authenticated")]
    public async Task<IActionResult> Logout()
    {
        logger.LogInformation("[GET] Logout: signing out user {UserName}", User.Identity?.Name);
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