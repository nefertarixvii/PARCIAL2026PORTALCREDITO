using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 🔹 DB SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 🔹 Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// 🔥 🔥 CACHE (INTELIGENTE: LOCAL vs RENDER)

// 👉 Si existe Redis → úsalo
var redisConnection = builder.Configuration["Redis:ConnectionString"];

if (!string.IsNullOrEmpty(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}
else
{
    // 👉 Si NO hay Redis → usa memoria (LOCAL)
    builder.Services.AddDistributedMemoryCache();
}

// 🔹 SESIONES (BIEN CONFIGURADAS)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // duración sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

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
app.UseStaticFiles(); // 🔥 necesario

app.UseRouting();

// 🔹 AUTH (orden correcto)
app.UseAuthentication();
app.UseAuthorization();

// 🔹 SESIÓN (después de auth)
app.UseSession();

// 🔹 Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();