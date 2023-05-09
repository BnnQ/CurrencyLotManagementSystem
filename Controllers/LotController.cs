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

    public LotController(ILotManager lotManager, UserManager<User> userManager, IMapper mapper)
    {
        this.lotManager = lotManager;
        currencies = new SelectList(Enum.GetValues<Currency>());
        
        this.userManager = userManager;
        this.mapper = mapper;
    }

    [HttpGet]
    [RetrieveModelErrorsFromRedirector]
    public IActionResult List(Currency? currency)
    {
        ViewBag.Currencies = currencies;
        return View(currency is not null ? lotManager.GetByCurrencyAsync(currency.Value) : new List<Lot>());
    }
    
    [HttpPost]
    public async Task<IActionResult> GetLotsByCurrency(Currency currency)
    {
        return PartialView(viewName: "_Lots", await lotManager.GetByCurrencyAsync(currency));
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Currencies = currencies;
        return View(new CreationViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreationViewModel creationViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(creationViewModel);
        }
        
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return NotFound();
        }

        var lot = mapper.Map<Lot>(creationViewModel);
        lot.SellerNickname = user.UserName!;
        lot.SellerSurname = user.Surname;

        await lotManager.AddAsync(lot);
        return RedirectToList();
    }
    
    [HttpPost]
    [KeepModelErrorsOnRedirect]
    public async Task<IActionResult> Buy(string lotId, string popReceipt)
    {
        try
        {
            await lotManager.DeleteAsync(lotId, popReceipt);
        }
        catch
        {
            ModelState.AddSummaryError("The time has expired to buy the lot.");
        }
        
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