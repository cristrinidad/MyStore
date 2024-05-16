using MyStores.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using MyStores.DataAccess.Repository.IRepository;
using MyStores.DataAccess.Repository;
using Microsoft.AspNetCore.Identity;
using MyStores.Utilities;
using Microsoft.AspNetCore.Identity.UI.Services;
using Stripe;
using MyStores.DataAccess.DbInitializer;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(GetHerokuConnectionString(builder.Configuration), b => b.MigrationsAssembly("MyStore")));

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
var app = builder.Build();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DATABASE_URL")))
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    app.Urls.Add($"http://*:{port}");
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:SecretKey").Get<string>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
SeedDatabase();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();

void SeedDatabase()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}

string? GetHerokuConnectionString(IConfiguration configuration)
{

    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    string connectionString;

    if(!string.IsNullOrEmpty(databaseUrl))
    {
        var databaseUri = new Uri(databaseUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = databaseUri.LocalPath.TrimStart('/')
        };

        // Whether to use SSL or not
        // Require SSL in production, but you can disable it for development if needed
        builder.SslMode = SslMode.Require;
        builder.TrustServerCertificate = true;

        connectionString =  builder.ToString();
    }
    else
    {
        connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    return connectionString;
}