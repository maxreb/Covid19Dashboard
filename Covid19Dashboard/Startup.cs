using Blazored.LocalStorage;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reble.RKIWebService;
using RKIWebService.Services;
using RKIWebService.Services.Arcgis;

namespace Covid19Dashboard
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{

			services.AddHttpClient();
			services.AddRazorPages();
			services.AddServerSideBlazor();

			services.AddRKIWebService();

			services.AddSingleton<ViewCounterService>();

			services.AddBlazoredLocalStorage();

			services.AddMatBlazor();
			services.AddMatToaster(config =>
			{
				config.Position = MatToastPosition.BottomCenter;
				config.PreventDuplicates = true;
				config.NewestOnTop = true;
				config.ShowCloseButton = true;
				config.MaximumOpacity = 100;
				config.RequireInteraction = true;
				config.VisibleStateDuration = 3000;
			});

		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseStaticFiles();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapBlazorHub();
				endpoints.MapFallbackToPage("/_Host");
			});
		}
	}
}
