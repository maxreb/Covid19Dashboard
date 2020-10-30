using ChartJs.Blazor.ChartJS.Common;
using ChartJs.Blazor.ChartJS.Common.Axes;
using ChartJs.Blazor.ChartJS.Common.Axes.Ticks;
using ChartJs.Blazor.ChartJS.Common.Enums;
using ChartJs.Blazor.ChartJS.Common.Handlers;
using ChartJs.Blazor.ChartJS.Common.Properties;
using ChartJs.Blazor.ChartJS.Common.Wrappers;
using ChartJs.Blazor.ChartJS.LineChart;
using ChartJs.Blazor.ChartJS.MixedChart;
using ChartJs.Blazor.Charts;
using ChartJs.Blazor.Util;
using Covid19Dashboard.Entities;
using Covid19Dashboard.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
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
		private ICovid19Data? data;
		private ICovid19Data? dataYesterday;

		private LineConfig chart7Config;
		private LineConfig chartTConfig;

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
			cities = CitiesRepository.CitiesToKeys.Keys;
			chart7Config = CreateConfig();
			chartTConfig = CreateConfig();
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

				if (CitiesRepository.CitiesToKeys.TryGetValue(City, out string? key) && key != null)
				{
					data = await CovidApi.GetFromCityKey(key);
					if (data == null)
					{
						Toaster.Add($"Keine Daten für die Stadt {City}", MatToastType.Warning);
					}

					else
					{
						CovidApi.TryGetFromCityKey(key, 1, out dataYesterday);

						chart7Config.Data.Datasets.Add(new LineDataset<object>(new object[] { 3, 4, 5 })
						{
							Label = "Data"
						});

						colorRki = data.Cases7Per100k switch
						{
							var x when x == 0 => "#fff",
							var x when x < 5 => "#D2D0AC",
							var x when x >= 5 && x < 35 => "#D7D289",
							var x when x >= 35 && x < 50 => "#D2990B",
							var x when x >= 50 & x < 100 => "#B42B37",
							_ => "#910B1E"
						};
						FillCharts(key, 7);
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

		void FillCharts(string key, int daysInPast)
		{
			List<Point> data7Per100k = new List<Point>();
			List<Point> dataTotalCases = new List<Point>();
			double min7 = double.MaxValue;
			double minT = double.MaxValue;
			for (uint i = 0; i <= daysInPast; i++)
			{
				if (CovidApi.TryGetFromCityKey(key, i, out ICovid19Data? data) && data != null)
				{
					data7Per100k.Add(new Point(daysInPast - i, data.Cases7Per100k));
					if (data.Cases7Per100k < min7)
						min7 = data.Cases7Per100k;
					if (data.Cases < minT)
						minT = data.Cases;
					dataTotalCases.Add(new Point(daysInPast - i, data.Cases));
				}
			}
			min7 = Math.Floor(min7 / 10) * 10;
			minT = Math.Floor(minT / 10) * 10;
			chart7Config.Data.Datasets.Clear();
			chartTConfig.Data.Datasets.Clear();
			((LinearCartesianAxis)chart7Config.Options.Scales.yAxes.First()).Ticks.Min = min7;
			((LinearCartesianAxis)chartTConfig.Options.Scales.yAxes.First()).Ticks.Min = minT;

			var data7 = new LineDataset<Point>(data7Per100k)
			{
				BorderColor = ColorUtil.FromDrawingColor(System.Drawing.Color.Gray),
				BorderWidth = 2,
				PointRadius = 0,
				LineTension = 0.1
			};
			var dataT = new LineDataset<Point>(dataTotalCases)
			{
				BorderColor = ColorUtil.FromDrawingColor(System.Drawing.Color.Gray),
				BorderWidth = 1,
				PointRadius = 0,
				LineTension = 0.1
			};
			chart7Config.Data.Datasets.Add(data7);
			chartTConfig.Data.Datasets.Add(dataT);
		}

		LineConfig CreateConfig()
		{
			return new LineConfig
			{
				
				Options = new LineOptions
				{
					Title = new OptionsTitle { Text = " - 7 Tage - ", Display = true, Position = Position.Bottom },
					Scales = new Scales
					{
						xAxes = new List<CartesianAxis>
						{
							new LinearCartesianAxis
							{
								Display = AxisDisplay.False,
								ScaleLabel = new ScaleLabel{LabelString = "Time"}
							}
						},
						yAxes = new List<CartesianAxis> {
							new LinearCartesianAxis {
								Ticks = new LinearCartesianTicks{ MaxTicksLimit = 3 },
								GridLines = new GridLines{
									Display = false
								}
							}
						}
					},
					Responsive = true,
					Hover = new LineOptionsHover { Enabled = false },
					Legend = new Legend
					{

						Display = false
					},
				}
			};
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
