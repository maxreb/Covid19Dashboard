using ChartJs.Blazor.ChartJS.Common.Time;
using Covid19Dashboard.Entities;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

using System.Linq;

namespace Covid19Dashboard.Pages
{
	public partial class Index
	{
		[Inject] ICovidApiService CovidApi { get; set; } = default!;
		[Inject] IMatToaster Toaster { get; set; } = default!;
		[Inject] ILogger<Index> Logger { get; set; } = default!;
		[Parameter] public string? City { get; set; }

		private List<TimeTuple<double>> Data7 { get; } = new List<TimeTuple<double>>();
		private List<TimeTuple<double>> DataTotal { get; } = new List<TimeTuple<double>>();
		private string TextStyle7 => "font-size: 120px; font-weight: 800; color: " + colorRki;
		private bool Succeeded { get; set; }
		private ICovid19Data? DatasetCurrent { get; set; }
		private bool DataUpToDate { get; set; }
		IEnumerable<string> Cities => CitiesRepository.CitiesToKeys.Keys;


		string colorRki = "#000";


		protected override void OnAfterRender(bool firstRender)
		{
			if (firstRender)
				Logger.LogInformation($"New user with parameter /{City ?? ""}");
		}

		protected override void OnParametersSet()
		{
			if (string.IsNullOrEmpty(City))
				City = "Kiel";
			Update();

		}
		protected override bool ShouldRender()
		{
			if (string.IsNullOrEmpty(City))
			{
				return false;
			}
			else
			{
				Update();
				return true;
			}
		}
		private void Update()
		{
			try
			{
				Data7.Clear();
				DataTotal.Clear();
				Succeeded = false;
				DataUpToDate = false;

				if (string.IsNullOrEmpty(City) || City.Length < 3)
					return;

				if (CitiesRepository.CitiesToKeys.TryGetValue(City, out string? key) && key != null)
				{
					Succeeded = CovidApi.TryGetFromCityKey(key, DateTime.UtcNow.AddDays(-7), out IEnumerable<ICovid19Data> data);
					if (Succeeded)
					{
						Logger.LogDebug($"Received data for city {City} ({key})");
						DatasetCurrent = data.Last();
						DataUpToDate = DateTime.Now.AddHours(-5).Date <= DatasetCurrent.LastUpdate.Date;
						Data7.AddRange(data.Select(x => new TimeTuple<double>(new Moment(x.LastUpdate), x.Cases7Per100k)));
						DataTotal.AddRange(data.Select(x => new TimeTuple<double>(new Moment(x.LastUpdate), x.Cases)));
						colorRki = DatasetCurrent.Cases7Per100k switch
						{
							var x when x == 0 => "#fff",
							var x when x < 5 => "#D2D0AC",
							var x when x >= 5 && x < 35 => "#D7D289",
							var x when x >= 35 && x < 50 => "#D2990B",
							var x when x >= 50 & x < 100 => "#B42B37",
							_ => "#910B1E"
						};

						if (!DataUpToDate)
						{
							Toaster.Add($"Die aktuellen Covid-19 Fälle können derzeit nicht angezeigt werden. Das RKI arbeitet an der Lösung des Problems...", MatToastType.Danger, "Störung RKI");
						}
					}
					else
					{
						Toaster.Add($"Keine Daten für die Stadt {City}", MatToastType.Warning);
					}
				}
			}
			catch (Exception ex)
			{
				Toaster.Add(ex.Message, MatToastType.Danger, "Error");
			}
		}
	}
}