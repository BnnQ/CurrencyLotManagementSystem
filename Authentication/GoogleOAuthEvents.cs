using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace CurrencyLotManagementSystem.Authentication
{
    public class GoogleOAuthEvents : OAuthEvents
    {
        public override async Task CreatingTicket(OAuthCreatingTicketContext context)
        {
            await base.CreatingTicket(context);

            var accessToken = context.AccessToken;
            var credential = GoogleCredential.FromAccessToken(accessToken);
            var service = new PeopleServiceService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Loternos"
            });

            var request = service.People.Get("people/me");
            request.PersonFields = "names";

            var person = await request.ExecuteAsync();
            var surname = person.Names.FirstOrDefault(name => !string.IsNullOrWhiteSpace(name.FamilyName))?.FamilyName;
            if (surname is not null)
            {
                var identity = (context.Principal!.Identity as ClaimsIdentity)!;
                identity.AddClaim(new Claim(ClaimTypes.Surname, surname, ClaimValueTypes.String));
            }   

        }
    }
}