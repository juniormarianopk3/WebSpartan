using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using WebSpartan.Models.Filters;
using WebSpartan.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<WhatsAppViewBagFilter>(); // adiciona o filtro global
}); 
builder.Services.AddDistributedMemoryCache(); // Armazena dados da sess�o na mem�ria
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tempo de expira��o
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
               .AddCookie(options =>
               {
                   options.LoginPath = "/Conta/Login";           // redireciona para /Conta/Login se n�o autenticado
                   options.AccessDeniedPath = "/Conta/AcessoNegado"; // p�gina quando usu�rio n�o tiver permiss�o
                   options.ExpireTimeSpan = TimeSpan.FromHours(1);
               });

// 2. Configurar autoriza��o com role �Admin�
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SomenteAdmin", policy =>
    {
        policy.RequireRole("Admin");
    });
});

builder.Services.Configure<AdminCredentialsSettings>(
    builder.Configuration.GetSection("AdminCredentials"));
builder.Services.AddScoped<WhatsAppViewBagFilter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession(); // Ative a sess�o aqui

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
