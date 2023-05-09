using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CurrencyLotManagementSystem.Filters;
public class RetrieveModelErrorsFromRedirectorAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        base.OnResultExecuting(context);
        if (context.Result is not ViewResult) return;
        if (context.Controller is not Controller controller) return;

        var tempData = controller.TempData.Keys;
        if (tempData.Contains("ModelErrorList"))
        {
            var serializedModelErrorList = controller.TempData["ModelErrorList"]!.ToString();
            
            var modelErrorList = JsonSerializer.Deserialize<Dictionary<string, IList<string>>>(serializedModelErrorList!)!;
            var modelState = new ModelStateDictionary();
            foreach (var error in modelErrorList)
            {
                foreach (var errorValue in error.Value)
                {
                    modelState.AddModelError(error.Key, errorValue);   
                }
            }
            
            controller.ViewData.ModelState.Merge(modelState);
        }
    }
}