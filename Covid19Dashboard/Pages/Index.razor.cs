using Covid19Dashboard.Data;
using Covid19Dashboard.Entities;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Covid19Dashboard.Pages
{
	public partial class Index
	{
		[Inject] ICovidApi CovidApi { get; set; } = default!;
		[Inject] CitiesGermany CitiesGermany { get; set; } = default!;
		[Inject] IMatToaster Toaster { get; set; } = default!;
		private ICovid19Data? data;
		private ICovid19Data? dataYesterday;

		//bool showSpinner = false;
		string colorRki = "#000";
		IEnumerable<string> cities = Enumerable.Empty<string>();
		[Parameter]
		public string City
		{
			get => city; set
			{
				if (value != null)
				{
					city = value; OnUserFinish();
				}
			}
		}
		bool shouldRender = true;
		protected override bool ShouldRender() => shouldRender;

		protected async override Task OnInitializedAsync()
		{
			cities = CitiesGermany.CitiesToKeys.Keys;
			await Update();
		}

		private string city = "Kiel";


		private void OnUserFinish()
		{
			InvokeAsync(async () =>
			{
				await Update();
				shouldRender = true;
				StateHasChanged();
				shouldRender = false;
			});
		}


		private (string icon, string style) CompareToStringAndColor(double a, double b)
		{
			string style = "width:100%;color:";
			if (a > b)
				return ("arrow_upward", style + "#f00");
			else if (a == b)
				return ("indeterminate_check_box", style + "#fff");
			return ("arrow_downward", style + "#0f0");
		}


		private async Task Update()
		{
			try
			{
				data = null;

				if (string.IsNullOrEmpty(City) || City.Length < 3)
					return;

				if (CitiesGermany.CitiesToKeys.TryGetValue(City, out string? key) && key != null)
				{
					data = await CovidApi.GetFromCityKey(key);
					if (data == null)
					{
						Toaster.Add($"Keine Daten für die Stadt {City}", MatToastType.Warning);
					}

					else
					{
						CovidApi.TryGetFromCityKey(key, 1, out dataYesterday);
						colorRki = data.Cases7Per100k switch
						{
							var x when x == 0 => "#fff",
							var x when x < 5 => "#D2D0AC",
							var x when x >= 5 && x < 35 => "#D7D289",
							var x when x >= 35 && x < 50 => "#D2990B",
							var x when x >= 50 & x < 100 => "#B42B37",
							_ => "#910B1E"
						};
					}
				}
			}
			catch (Exception ex)
			{
				Toaster.Add(ex.Message, MatToastType.Danger, "Error");
			}
			finally
			{
				//showSpinner = false;
			}

		}

		//public Index()
		//{
		//	debounceTimer = new Timer(500);
		//	debounceTimer.Elapsed += OnUserFinish;
		//	debounceTimer.AutoReset = false;

		//}
		//protected void OnKeyUp()
		//{
		//	debounceTimer.Stop();
		//	showSpinner = true;
		//	debounceTimer.Start();
		//}

	}
}
