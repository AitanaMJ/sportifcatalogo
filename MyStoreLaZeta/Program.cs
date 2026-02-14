using Microsoft.EntityFrameworkCore;
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

builder.Services.AddSession(options => {options.IdleTimeout = TimeSpan.FromMinutes(60); });


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


app.Run();
