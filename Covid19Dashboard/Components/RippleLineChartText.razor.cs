using ChartJs.Blazor.ChartJS.Common;
using ChartJs.Blazor.ChartJS.Common.Axes;
using ChartJs.Blazor.ChartJS.Common.Axes.Ticks;
using ChartJs.Blazor.ChartJS.Common.Enums;
using ChartJs.Blazor.ChartJS.Common.Handlers;
using ChartJs.Blazor.ChartJS.Common.Properties;
using ChartJs.Blazor.ChartJS.Common.Time;
using ChartJs.Blazor.ChartJS.LineChart;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Covid19Dashboard.Components
{
	public partial class RippleLineChartText
	{
		[Parameter]
		public string Title { get; set; } = "Data";
		[Parameter]
		public string? TextStyle { get; set; }
		[Parameter]
		public IList<TimeTuple<double>> Data { get; set; } = default!;
		[Parameter]
		public bool ShowTrendIcon { get; set; }
		[Parameter]
		public bool ShowTrendNumber { get; set; }

		private double dataToday;
		private double dataYesterday;
		private bool showTrend = false;

		bool showOnlyGraph = false;
		private string TitleClass => showOnlyGraph ? "rlct-title rlct-title-up" : "rlct-title rlct-title-down";
		private string TextClasses => showOnlyGraph ? "rlct-text rlct-text-hide" : "rlct-text rlct-text-show";

		private readonly LineConfig _chartConfig;
		public RippleLineChartText()
		{
			_chartConfig = CreateNewChartConfig();
		}


		private void ToggleGraphView()
		{
			showOnlyGraph = !showOnlyGraph;
			var xAxis = ((TimeAxis)_chartConfig.Options.Scales.xAxes.First());
			var yAxis1 = ((LinearCartesianAxis)_chartConfig.Options.Scales.yAxes[0]);
			var yAxis2 = ((LinearCartesianAxis)_chartConfig.Options.Scales.yAxes[1]);
			var dataSets = _chartConfig.Data.Datasets.Cast<LineDataset<TimeTuple<double>>>();
			if (showOnlyGraph)
			{
				//yAxis2.Display = AxisDisplay.False;
				xAxis.Display = AxisDisplay.True;
				yAxis1.Ticks.MaxTicksLimit = 8;
				yAxis1.GridLines.Display = true;
				yAxis1.GridLines.Color = "#aaa";

				_chartConfig.Options.Title.Display = false;

				foreach (var data in dataSets)
				{
					data.BackgroundColor = "#FFFA";
					data.BorderColor = "#FFFC";
				}
			}
			else
			{
				yAxis2.Display = AxisDisplay.True;
				xAxis.Display = AxisDisplay.False;
				yAxis1.Ticks.MaxTicksLimit = 2;
				yAxis1.GridLines.Display = false;
				_chartConfig.Options.Title.Display = true;
				yAxis1.GridLines.Color = "#0000";

				foreach (var data in dataSets)
				{
					data.BackgroundColor = "#666";
					data.BorderColor = "#555";
				}


			}
		}

		protected override void OnParametersSet()
		{
			if (Data == null || Data.Count < 1)
				throw new ArgumentNullException(nameof(Data));
			if (ShowTrendIcon && ShowTrendNumber)
				throw new ArgumentException($"You have to choose between {nameof(ShowTrendIcon)} and {nameof(ShowTrendNumber)}");
			showTrend = ShowTrendNumber || ShowTrendIcon;
			dataToday = Math.Round(Data[Data.Count - 1].YValue, 1);
			if (Data.Count > 1)
			{
				dataYesterday = Math.Round(Data[Data.Count - 2].YValue, 1);
			}
			else
			{
				showTrend = false;
			}
			double min = Data.Min(x => x.YValue);
			double max = Data.Max(x => x.YValue);
			double den = 10;
			if (min > 1000)
				den = 100;
			min = Math.Floor(min / den) * den;
			max = Math.Ceiling(max / den) * den;

			_chartConfig.Data.Datasets.Clear();

			foreach (LinearCartesianAxis yAxis in _chartConfig.Options.Scales.yAxes)
			{
				yAxis.Ticks.Min = min;
				yAxis.Ticks.Max = max;
			}

			var data = new LineDataset<TimeTuple<double>>(Data)
			{
				BorderColor = (showOnlyGraph ? "#FFFA" : "#666"),
				BackgroundColor = (showOnlyGraph ? "#FFFC" : "#555"),
				Fill = true,
				BorderWidth = 2,
				PointRadius = 0,
				LineTension = 0.1
			};
			_chartConfig.Data.Datasets.Add(data);
		}

		private (string icon, string style) GetIconAndStyle(double a, double b)
		{
			string style = "width:100%;color:";
			if (a > b)
				return ("arrow_upward", style + "#f00");
			else if (a == b)
				return ("indeterminate_check_box", style + "#fff");
			return ("arrow_downward", style + "#0f0");
		}


		LineConfig CreateNewChartConfig()
	 => new LineConfig
	 {

		 Options = new LineOptions
		 {
			 Title = new OptionsTitle { Text = " - 7 Tage - ", Display = true, Position = Position.Bottom },
			 Scales = new Scales
			 {
				 xAxes = new List<CartesianAxis>
				 {
							new TimeAxis
							{
								Display = AxisDisplay.False,
								GridLines = new GridLines
								{
									Display = true,
									Color = "#aaa",
									BorderDash =  new[]{ 4.0,8.0 }
								}, Ticks = new TimeTicks{ FontColor = "#aaa"}

							}
				 },
				 yAxes = new List<CartesianAxis> {
							new LinearCartesianAxis {
								Position = Position.Left,
								Ticks = new LinearCartesianTicks{ MaxTicksLimit = 2, FontColor = "#aaa" },
								GridLines = new GridLines{
									Display = false,
									BorderDash =  new[]{ 8.0,4.0 }
								}
							}
							,new LinearCartesianAxis {
								Position = Position.Right,
								Ticks = new LinearCartesianTicks{ MaxTicksLimit = 2, FontColor = "#aaa" },
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
}

