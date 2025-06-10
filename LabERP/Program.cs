using LabERP.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LabERP.Interface;
using LabERP.Models.Core;
using LabERP.Models.Handlers;

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

// 註冊您的接口和實現
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ILaboratoryRepository, InMemoryLaboratoryRepository>();
builder.Services.AddSingleton<IEquipmentRepository, InMemoryEquipmentRepository>();
builder.Services.AddSingleton<IFinanceRepository, InMemoryFinanceRepository>();
builder.Services.AddSingleton<IBankAccountRepository, InMemoryBankAccountRepository>();
builder.Services.AddSingleton<ISalaryRepository, InMemorySalaryRepository>();
builder.Services.AddSingleton<IExpenseAttachmentRepository, InMemoryExpenseAttachmentRepository>();
builder.Services.AddSingleton<IExpenseRequestRepository, InMemoryExpenseRequestRepository>();

// ★ 添加 WorkSession 相關的服務註冊 ★
builder.Services.AddSingleton<IWorkSessionRepository, InMemoryWorkSessionRepository>(); // 需要創建這個實現類
builder.Services.AddScoped<IWorkSessionHandler, WorkSessionHandler>();

// 註冊服務
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<LaboratoryHandler>();
builder.Services.AddScoped<IFinanceHandler, FinanceHandler>();
builder.Services.AddScoped<IExpenseRequestHandler, ExpenseRequestHandler>();

// 添加 IUserHandler 的實現
builder.Services.AddScoped<IUserHandler, UserHandler>(); // 請替換為您實際的實現類
builder.Services.AddScoped<IAccountHandler, AccountHandler>();
builder.Services.AddScoped<AccountController>();
builder.Services.AddScoped<IEquipmentHandler, EquipmentHandler>();
builder.Services.AddScoped<EquipmentController>();

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