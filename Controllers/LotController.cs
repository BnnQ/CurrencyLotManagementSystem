using System.Security.Claims;
using AutoMapper;
using CurrencyLotManagementLibrary.Enums;
using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.Filters;
using CurrencyLotManagementSystem.Services.Abstractions;
using CurrencyLotManagementSystem.Utils.Extensions;
using CurrencyLotManagementSystem.ViewModels.Lot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CurrencyLotManagementSystem.Controllers;

[Authorize(policy: "Authenticated")]
public class LotController : Controller
{
    private readonly ILotManager lotManager;
    private readonly SelectList currencies;
    private readonly UserManager<User> userManager;
    private readonly IMapper mapper;
    private readonly ILogger<LotController> logger;

    public LotController(ILotManager lotManager, UserManager<User> userManager, IMapper mapper, ILoggerFactory loggerFactory)
    {
        this.lotManager = lotManager;
        currencies = new SelectList(Enum.GetValues<Currency>());
        
        this.userManager = userManager;
        this.mapper = mapper;
        logger = loggerFactory.CreateLogger<LotController>();
    }

    [HttpGet]
    [RetrieveModelErrorsFromRedirector]
    public IActionResult List()
    {
        ViewBag.Currencies = currencies;
        logger.LogInformation("[GET] List: returning view");
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> GetLotsByCurrency(Currency currency)
    {
        const string PartialViewName = "_Lots";
        logger.LogInformation("[POST] GetLotsByCurrency: returning {PartialViewName} partial view", PartialViewName);
        return PartialView(viewName: PartialViewName, await lotManager.GetByCurrencyAsync(currency));
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Currencies = currencies;
        logger.LogInformation("[GET] Create: returning view");
        return View(new CreationViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreationViewModel creationViewModel)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("[POST] Create: model contains errors, returning view");
            return View(creationViewModel);
        }
        
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            logger.LogWarning("[POST] Create: for some reason, unable to get the current user, returning 404 Not Found");
            return NotFound();
        }

        var lot = mapper.Map<Lot>(creationViewModel);
        lot.SellerNickname = user.UserName!;
        lot.SellerSurname = user.Surname;

        await lotManager.AddAsync(lot);
        logger.LogInformation("[POST] Create: successfully created lot by user {UserName}", user.UserName);
        return RedirectToList();
    }
    
    [HttpPost]
    [KeepModelErrorsOnRedirect]
    public async Task<IActionResult> Buy(string lotId, string popReceipt)
    {
        try
        {
            await lotManager.DeleteAsync(lotId, popReceipt);
            logger.LogInformation("[POST] Buy: successfully buyed lot {LotId} by user {UserName}", lotId, User.Identity?.Name);
        }
        catch
        {
            ModelState.AddSummaryError("The time has expired to buy the lot.");
        }
        
        logger.LogWarning("[POST] Buy: unsuccessful lot {LotId} purchase by user {UserName} because the time to buy the lot has expired, redirecting to list", lotId, User.Identity?.Name);
        return RedirectToList();
    }

    #region Utils

    private IActionResult RedirectToList()
    {
        return RedirectToAction(nameof(List));
    }
    
    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await userManager.FindByIdAsync(userId ?? string.Empty);

        return user;
    }

    #endregion
}