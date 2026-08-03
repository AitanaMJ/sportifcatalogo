using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using CatalogoPro.Entities;
using CatalogoPro.Context;
using CatalogoPro.Repositories;
using CatalogoPro.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlString"));
});

builder.Services.AddScoped(typeof(GenericRepository<>));
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(
    options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// --- INICIO DATA SEEDING (Creación automática del Admin) ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    // Verificamos si ya existe algún usuario con Type = "Admin"
    if (!context.Users.Any(u => u.Type == "Admin"))
    {
        // Leemos las credenciales desde appsettings.json
        var adminEmail = config["AdminConfig:Email"];
        var adminPassword = config["AdminConfig:Password"];

        var adminUser = new User
        {
            FullName = "Administrador Principal",
            Email = adminEmail,
            // Encriptamos la contraseña obtenida del archivo de configuración
            Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Type = "Admin"
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}
// --- FIN DATA SEEDING ---

app.Run();