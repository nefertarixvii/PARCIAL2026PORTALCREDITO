using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Connection string
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=app.db";

// 🔥 PARCHE: Soporte automático para URLs de Render (postgres:// y postgresql://)
if (connectionString.Contains("postgres://") || connectionString.Contains("postgresql://"))
{
    // Estandarizamos a "postgres://" para que la clase Uri de .NET no se maree con la 'l'
    var cleanString = connectionString.Replace("postgresql://", "postgres://");
    
    var databaseUri = new Uri(cleanString);
    var userInfo = databaseUri.UserInfo.Split(':');

    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.AbsolutePath.TrimStart('/')};User Id={userInfo[0]};Password={userInfo[1]};Ssl Mode=Require;Trust Server Certificate=true;";
}

// 🔹 DB PostgreSQL (Usando la cadena ya formateada y segura)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 🔹 Identity + Roles
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// 🔹 Cache local
builder.Services.AddDistributedMemoryCache();

// 🔹 Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 🔹 MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 🔹 Aplicar Migraciones automáticamente en producción de forma robusta
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Cambiado a Migrate() para que soporte correctamente el historial de Entity Framework en PostgreSQL
    db.Database.Migrate();

    Console.WriteLine("Base de datos creada/actualizada correctamente con sus migraciones.");
}
catch (Exception ex)
{
    Console.WriteLine("ERROR BD:");
    Console.WriteLine(ex.Message);
}

// 🔹 Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();