using System.Globalization;
using Blazored.LocalStorage;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Reble.RKIWebService;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole(configure => configure.TimestampFormat = "yy-MM-dd HH:mm:ss");
builder.Host.ConfigureAppConfiguration((hostContext, app) => app.AddEnvironmentVariables("REBLE_"));

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddRKIWebService();

builder.Services.AddSingleton<ViewCounterService>();
builder.Services.AddBlazoredLocalStorage();

builder.Services.AddMatBlazor();
builder.Services.AddMatToaster(config =>
{
    config.Position = MatToastPosition.BottomCenter;
    config.PreventDuplicates = true;
    config.NewestOnTop = true;
    config.ShowCloseButton = true;
    config.MaximumOpacity = 100;
    config.RequireInteraction = true;
    config.VisibleStateDuration = 3000;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
