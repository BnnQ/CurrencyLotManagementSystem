using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CurrencyLotManagementSystem.Utils.Extensions;

public static class ModelStateDictionaryExtensions
{
    public static ModelStateDictionary AddSummaryError(this ModelStateDictionary modelState, string errorMessage)
    {
        modelState.AddModelError(string.Empty, errorMessage);
        return modelState;
    }

    public static ModelStateDictionary AddErrorForProperty(this ModelStateDictionary modelState, string propertyName, string errorMessage)
    {
        modelState.AddModelError(propertyName, errorMessage);
        return modelState;
    }
    
    public static ModelStateDictionary AddSummaryErrorForProperty(this ModelStateDictionary modelState, string propertyName, string errorMessage) 
    {
        modelState.AddSummaryError(errorMessage).AddErrorForProperty(propertyName, errorMessage);
        return modelState;
    }
}