using CurrencyLotManagementLibrary.Models.Entities;
using CurrencyLotManagementSystem.Authentication;
using CurrencyLotManagementSystem.Filters;
using CurrencyLotManagementSystem.Models.Contexts;
using CurrencyLotManagementSystem.Services;
using CurrencyLotManagementSystem.Services.Abstractions;
using CurrencyLotManagementSystem.Services.MapperProfiles;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

namespace CurrencyLotManagementSystem.Utils.Extensions;

public static class StartupExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<Configuration.Azure>(builder.Configuration.GetRequiredSection("Azure"));
        
        builder.Services.AddDbContext<LotManagementContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
        });
        
        builder.Services.AddIdentity<User, IdentityRole>()
            .AddEntityFrameworkStores<LotManagementContext>()
            .AddDefaultTokenProviders();

        builder.Services.Configure<IdentityOptions>(options =>
        {
            // Password settings
            if (!builder.Environment.IsProduction())
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 3;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            }
            else
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            }

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(builder.Environment.IsProduction() ? 60 : 1);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
        });
        
        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(value: 60);
            
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";

            options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
        });
            
        builder.Services.AddAuthentication()
            .AddCookie()
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration[key: "Authentication:Google:ClientId"] ?? throw new InvalidOperationException("Configuration value Authentication:Google:ClientId is not provided.");
                options.ClientSecret = builder.Configuration[key: "Authentication:Google:ClientSecret"] ?? throw new InvalidOperationException("Configuration value Authentication:Google:ClientSecret is not provided.");
                options.SaveTokens = true;
                options.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
                options.Events = new GoogleOAuthEvents();
            })
            .AddGitHub(options =>
            {   
                options.ClientId = builder.Configuration[key: "Authentication:GitHub:ClientId"] ?? throw new InvalidOperationException("Configuration value Authentication:GitHub:ClientId is not provided.");
                options.ClientSecret = builder.Configuration[key: "Authentication:GitHub:ClientSecret"] ?? throw new InvalidOperationException("Configuration value Authentication:GitHub:ClientSecret is not provided.");
                options.Scope.Add(item: "user:email");
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(name: "Authenticated", policy => policy.RequireAuthenticatedUser());
        });
        
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.IdleTimeout = TimeSpan.FromMinutes(value: 60);
        });

        builder.Services.AddAutoMapper(profiles =>
        {
            profiles.AddProfile<UserRegistrationProfile>();
            profiles.AddProfile<LotCreationProfile>();
        });

        builder.Services.AddAzureClients(clients =>
        {
            clients.AddQueueServiceClient(builder.Configuration.GetConnectionString("AzureQueue"));
        });

        builder.Services.AddSingleton<ILotManager, AzureQueueLotManager>();

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add<KeepModelErrorsOnRedirectAttribute>();
            options.Filters.Add<RetrieveModelErrorsFromRedirectorAttribute>();
        });
        
        return builder;
    }

    public static void Configure(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Lot}/{action=List}/{id?}");

    }

}