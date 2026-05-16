using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Connection string
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

// 🔹 DB SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 🔹 Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 🔥 CACHE (LOCAL O REDIS)

var redisConnection =
    builder.Configuration["Redis:ConnectionString"];

// 👉 Render con Redis
if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}
else
{
    // 👉 Local sin Redis
    builder.Services.AddDistributedMemoryCache();
}

// 🔹 Sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;
});

// 🔹 MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 🔹 CREAR ROL Y USUARIO ANALISTA
using (var scope = app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        scope.ServiceProvider
        .GetRequiredService<UserManager<IdentityUser>>();

    // 🔹 Crear rol Analista
    if (!await roleManager.RoleExistsAsync("Analista"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Analista"));
    }

    // 🔹 Usuario analista
    string email = "analista@test.com";
    string password = "Analista123!";

    var user =
        await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(user, password);

        await userManager.AddToRoleAsync(
            user,
            "Analista");
    }
}

// 🔹 Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// 🔹 AUTH
app.UseAuthentication();

app.UseAuthorization();

// 🔹 SESSION
app.UseSession();

// 🔹 Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();