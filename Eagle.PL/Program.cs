using Eagle.BL.Services;
using Eagle.DAL.Data;
using Eagle.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EagleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<EagleDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
});

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<SaleService>();
builder.Services.AddScoped<OverrideCodeService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    string[] roles = { "Manager", "Cashier" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new Role(role));
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    const string managerEmail = "admin@eagle.local";
    if (await userManager.FindByEmailAsync(managerEmail) is null)
    {
        var manager = new User { UserName = managerEmail, Email = managerEmail, FullName = "Eagle " };
        var result = await userManager.CreateAsync(manager, "Admin@123");
        if (result.Succeeded)
            await userManager.AddToRoleAsync(manager, "Manager");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();