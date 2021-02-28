using Blazored.LocalStorage;
using ChartJs.Blazor.ChartJS.Common.Time;
using Covid19Dashboard.Components;
using Covid19Dashboard.Entities;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using RKIWebService.Entities;
using RKIWebService.Services;
using RKIWebService.Services.Arcgis;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Threading.Tasks;

namespace Covid19Dashboard.Pages
{
	public partial class Index
	{
		[Inject] ICovidApiService CovidApi { get; set; } = default!;
		[Inject] IMatToaster Toaster { get; set; } = default!;
		[Inject] ILogger<Index> Logger { get; set; } = default!;
		[Inject] IJSRuntime JsRuntime { get; set; } = default!;
		[Inject] ViewCounterService ViewCounterService { get; set; } = default!;
		[Inject] ILocalStorageService LocalStorage { get; set; } = default!;
		[Inject] IMatDialogService MatDialogService { get; set; } = default!;
		[Inject] NavigationManager NavigationManager { get; set; } = default!;
		[Parameter] public string? City { get; set; }
		public string? lastCity;

		private List<TimeTuple<double>> Data7 { get; } = new List<TimeTuple<double>>();
		private List<TimeTuple<double>> DataTotal { get; } = new List<TimeTuple<double>>();
		private bool ShowAllData { get; set; }
		private string TextStyle7 => "font-size: 120px; font-weight: 800; color: " + colorRki;
		private bool Succeeded { get; set; }
		private ICovid19Data? DatasetCurrent { get; set; }
		private bool DataUpToDate { get; set; }
		IEnumerable<string> Cities => CitiesRepository.CitiesToKeys.Keys;
		private MatAutocompleteList<string?>? CityList { get; set; }


		string colorRki = "#000";


		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (firstRender)
			{
				var id = await LocalStorage.GetItemAsStringAsync("id");
				if (id == null)
				{
					id = Guid.NewGuid().ToString();
					await LocalStorage.SetItemAsync("id", id);
				}

				Logger.LogInformation($"New user {id} with parameter /{City ?? ""}");
				ViewCounterService.NewView(id);
				await ShowCampaigns();
			}
		}

		private async Task ShowCampaigns()
		{
			//If the current campaign is older then seven days, don't show it.
			if ((DateTime.UtcNow - Campaigns.Current.DateUTC).TotalDays > 7)
				return;
			List<Campaign> cookieCampaigns;
			if (await LocalStorage.ContainKeyAsync(Campaigns.CookieName))
				cookieCampaigns = await LocalStorage.GetItemAsync<List<Campaign>>(Campaigns.CookieName);
			else
				cookieCampaigns = new List<Campaign>();


			if (cookieCampaigns.Count == 0 ||
				cookieCampaigns.Last().DateUTC != Campaigns.Current.DateUTC)
			{
				await MatDialogService.OpenAsync(typeof(DialogWithPaypal),
					new MatDialogOptions
					{
						Attributes = new Dictionary<string, object> { ["Campaign"] = Campaigns.Current }
					});

				await LocalStorage.SetItemAsync(Campaigns.CookieName, Campaigns.All);
			}
		}

		protected override void OnParametersSet()
		{
			if (string.IsNullOrEmpty(City))
				City = "Kiel";
			else
				City = City.Replace('_', ' ');
			Update();

		}
		protected override bool ShouldRender()
		{
			if (string.IsNullOrEmpty(City))
			{
				if (CityList?.Id is string id)
					InvokeAsync(async () => await JsRuntime.InvokeVoidAsync("workaroundJs.focusElement", id));
				return false;
			}
			else
			{
				Update();
				return true;
			}
		}
		protected bool ShowStateData { get; set; } = false;
		private void SwitchToState() => ShowStateData = true;
		private void SwitchToCounty() => ShowStateData = false;

		private static string GetUriFromCity(string city)
		{
			city = city.Replace(' ', '_');//We need to do this as %20 chars are not allowed yet, see https://github.com/dotnet/aspnetcore/pull/26769
			return $"/{Uri.EscapeUriString(city)}";
		}
		private void Update()
		{

			try
			{
				//if (NavigationManager.)
				Data7.Clear();
				DataTotal.Clear();
				Succeeded = false;
				DataUpToDate = false;

				if (string.IsNullOrEmpty(City) || City.Length < 3)
					return;

				try
				{
					var path = GetUriFromCity(City);
					if (new UriBuilder(NavigationManager.Uri).Path != path)
						NavigationManager.NavigateTo(path);
				}
				catch { }
				if (CitiesRepository.CitiesToKeys.TryGetValue(City, out string? key))
					ShowCityData(key);

			}
			catch (Exception ex)
			{
				Toaster.Add(ex.Message, MatToastType.Danger, "Error");
			}
		}

		private void ShowCityData(string? cityKey)
		{
			if (string.IsNullOrEmpty(cityKey))
				return;
			DateTime from = ShowAllData ?
									DateTime.MinValue :
									DateTime.UtcNow.AddDays(-7);
			IEnumerable<ICovid19Data> data;
			if (ShowStateData)
				Succeeded = CovidApi.TryGetStateData(cityKey, from, out data);
			else
				Succeeded = CovidApi.TryGetCityData(cityKey, from, out data);

			var test = CovidApi.GetCurrentCityData(cityKey);
			var test2 = CovidApi.GetCurrentStateData(cityKey);
			if (Succeeded)
			{
				if (lastCity != City)
				{
					Logger.LogDebug($"Received data for city {City} ({cityKey})");
					lastCity = City;
				}
				DatasetCurrent = data.Last();
				DataUpToDate = DateTime.Now.AddHours(-5).Date <= DatasetCurrent.LastUpdate.Date;
				Data7.AddRange(data.Select(x => new TimeTuple<double>(new Moment(x.LastUpdate), x.Cases7Per100k)));
				DataTotal.AddRange(data.Select(x => new TimeTuple<double>(new Moment(x.LastUpdate), x.Cases)));
				colorRki = RkiColorFromValue(DatasetCurrent.Cases7Per100k);

				if (!DataUpToDate)
				{
					Toaster.Add($"Die aktuellen Covid-19 Fälle können derzeit nicht angezeigt werden. Das RKI arbeitet an der Lösung des Problems...", MatToastType.Danger, "Störung RKI");
				}
				else if (DateTime.Now.Date != DatasetCurrent.LastUpdate.Date)
				{
					Toaster.Add($"Das Dashboard zeigt zur Zeit noch den Datenstand vom Vortag an. In der Regel erfolgt die Aktualisierung der dem RKI neu übermittelten Covid-19 Fälle ab 03:00 Uhr. Bitte achten Sie auf die Angabe des Datenstandes unten im Dashboard", MatToastType.Info, "Hinweis");
				}
			}
			else
			{
				Toaster.Add($"Keine Daten für die Stadt {City}", MatToastType.Warning);
			}
		}

		private static string RkiColorFromValue(double value)
			=>
			 value switch
			 {
				 var x when x >= 500 => "#DA0183",
				 var x when x >= 250 => "#661313",
				 var x when x >= 100 => "#931315",
				 var x when x >= 50 => "#D13624",
				 var x when x >= 25 => "#FBB234",
				 var x when x >= 5 => "#FBEF7E",
				 _ => "#FBF8CA"
			 };

	}
}