using Blazored.LocalStorage;
using Covid19Dashboard.Components;
using Covid19Dashboard.Entities;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Reble.RKIWebService.Entities;
using Reble.RKIWebService.Services;
using ChartJs.Blazor.Common.Time;

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
		[Inject] HospitalizationService HospitalizationService { get; set; } = default!;
		[Parameter] public string? City { get; set; }
		public string? lastCity;

		private List<TimePoint> Data7 { get; } = new List<TimePoint>();
		private List<TimePoint> DataTotal { get; } = new List<TimePoint>();
		private bool ShowAllData { get; set; }
		private string TextStyle7 => "font-size: 120px; font-weight: 800; color: " + colorRki;
		private string TextStyleHosp => "font-size: 120px; font-weight: 800; color: " + colorRkiHosp;
		
		private bool Succeeded { get; set; }
		private ICovid19Data? DatasetCurrent { get; set; }
		private HospitalizationData? HospitalizationDatasetCurrent { get; set; }
		private bool DataUpToDate { get; set; }
		IEnumerable<string> Cities => CitiesRepository.CitiesToKeys.Keys;
		private MatAutocompleteList<string?>? CityList { get; set; }


		string colorRki = "#000";
		string colorRkiHosp = "#000";


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
				City = City.Replace('_', ' ');//To keep downwards compatibility
			Update();
		}

		protected override bool ShouldRender()
		{
			if (string.IsNullOrEmpty(City))
			{
				if (CityList?.Id is { } id)
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

		private const string? HospInfoTooltipString =
			"Die Hospitalisierungsinzidenz weist die hospitalisierten COVID-19-Fälle unter den in den letzten 7 Tagen gemeldeten Fällen bezogen auf 100.000 Menschen im jeweiligen Bundesland aus.\n" +
			"\n" +
			"Außerdem werden auch die in der Vergangenheit liegende Werte vom RKI täglich nachgereicht, weswegen der Graph für die letzten 7 Tage nicht sonderlich aussagekräftig ist.\n" +
			"\n" +
			"ab einem Wert von 3 --> 2G\n" +
			"ab einem Wert von 6 --> 2G+\n" +
			"ab einem Wert von 9 --> zusätzliche Maßnahmen je Land";

		private void SwitchToState() => ShowStateData = true;
		private void SwitchToCounty() => ShowStateData = false;

		private static string GetUriFromCity(string city) => "/" + city;

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
				catch
				{
				}

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
			DateTime from = ShowAllData ? DateTime.MinValue : DateTime.UtcNow.AddDays(-7);
			IEnumerable<ICovid19Data> data;
			if (ShowStateData)
				Succeeded = CovidApi.TryGetStateData(cityKey, from, out data);
			else
				Succeeded = CovidApi.TryGetCityData(cityKey, from, out data);

			if (Succeeded)
			{
				if (lastCity != City)
				{
					Logger.LogDebug($"Received data for city {City} ({cityKey})");
					lastCity = City;
				}

				var covid19Data = data as ICovid19Data[] ?? data.ToArray();
				DatasetCurrent = covid19Data.Last();


				var currentDate = DatasetCurrent.LastUpdate.Date;
				HospitalizationService.TaskCompletionSource.Task.Wait();
				var hospitalizationData = HospitalizationService.GetStateRecordsByCityKey(cityKey, from);
				var hospitalizationList = hospitalizationData.ToList();
				HospitalizationDatasetCurrent = hospitalizationList.Last();


				DataUpToDate = DateTime.Now.AddHours(-5).Date <= currentDate;
				Data7.AddRange(covid19Data.Select(x => new TimePoint(x.LastUpdate, x.Cases7Per100k)));
				DataTotal.AddRange(hospitalizationList.Select(x =>
					new TimePoint(x.Date, x.Hospitalization7TIncidence)));
				colorRki = RkiColorFromValue(DatasetCurrent.Cases7Per100k);
				colorRkiHosp = HospitalizationDatasetCurrent.Hospitalization7TIncidence switch
				{
					var i and >= 3 and < 6 => RkiColorFromValue(35),
					var i and > 6 and < 9 => RkiColorFromValue(50),
					var i and > 9 => RkiColorFromValue(500),
					_ => RkiColorFromValue(0)
				};

				if (!DataUpToDate)
				{
					ShowToastOnlyOncePerScope(
						$"Die aktuellen Covid-19 Fälle können derzeit nicht angezeigt werden. Das RKI arbeitet an der Lösung des Problems...",
						MatToastType.Danger, "Störung RKI");
				}
				else if (DateTime.Now.Date != DatasetCurrent.LastUpdate.Date)
				{
					ShowToastOnlyOncePerScope(
						$"Das Dashboard zeigt zur Zeit noch den Datenstand vom Vortag an. In der Regel erfolgt die Aktualisierung der dem RKI neu übermittelten Covid-19 Fälle ab 03:00 Uhr. Bitte achten Sie auf die Angabe des Datenstandes unten im Dashboard",
						MatToastType.Info, "Hinweis");
				}
			}
			else
			{
				Toaster.Add($"Keine Daten für die Stadt {City}", MatToastType.Warning);
			}
		}

		private readonly HashSet<int> messagesAlreadyShown = new HashSet<int>();

		private void ShowToastOnlyOncePerScope(string message, MatToastType type, string? title = null)
		{
			var hash = message.GetHashCode();
			if (!messagesAlreadyShown.Contains(hash))
			{
				Toaster.Add(message, type, title);
				messagesAlreadyShown.Add(hash);
			}
		}

		private static string RkiColorFromValue(double value)
			=>
				value switch
				{
					>= 1000 => "#790176",
					>= 500 => "#DA0183",
					>= 250 => "#661313",
					>= 100 => "#931315",
					>= 50 => "#D13624",
					>= 25 => "#FBB234",
					>= 5 => "#FBEF7E",
					_ => "#FBF8CA"
				};
	}
}