using WebApplication1.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication1.Interface;
using WebApplication1.Models.Core;
using WebApplication1.Models.Handlers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 添加身份驗證服務
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// 註冊 AccountController 為單例服務
builder.Services.AddSingleton<AccountController>();

// 添加 services to the container.
builder.Services.AddControllersWithViews();

// 註冊您的接口和實現
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ILaboratoryRepository, InMemoryLaboratoryRepository>();
builder.Services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();

// 註冊服務
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<LaboratoryHandler>();

// 添加 IUserHandler 的實現
builder.Services.AddScoped<IUserHandler, UserHandler>(); // 請替換為您實際的實現類

// 註冊 AccountController 為單例服務
builder.Services.AddSingleton<AccountController>();
builder.Services.AddSingleton<AccountHandler>();

builder.Services.AddSingleton<EquipmentController>();
builder.Services.AddSingleton<EquipmentHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();