using System.Globalization;
using DapperApp.Web.Extensions;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

var turkish = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkish;
CultureInfo.DefaultThreadCurrentUICulture = turkish;

// Add services to the container.



builder.Services.RegisterServices();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var localization = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkish),
    SupportedCultures = [turkish],
    SupportedUICultures = [turkish]
};
app.UseRequestLocalization(localization);

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
