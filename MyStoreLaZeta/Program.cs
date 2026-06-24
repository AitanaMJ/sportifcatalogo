using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MyStoreLaZeta.Entities;
using MyStoreLaZeta.Context;
using MyStoreLaZeta.Repositories;
using MyStoreLaZeta.Services;


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
builder.Services.AddScoped<MyStoreLaZeta.Services.EmailService>();


builder.Services.AddSession(options => { options.IdleTimeout = TimeSpan.FromMinutes(30); });
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

app.UseSession();
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

    // Verificamos si ya existe algún usuario con Type = "Admin"
    if (!context.Users.Any(u => u.Type == "Admin"))
    {
        var adminUser = new User
        {
            FullName = "Administrador Principal",
            Email = "adminlazeta1@gmail.com",
            // Encriptamos la contraseña con BCrypt, igual que en tu registro normal
            Password = BCrypt.Net.BCrypt.HashPassword("simon123"),
            Type = "Admin"
        };

        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}
// --- FIN DATA SEEDING ---

app.Run();


app.Run();
