using Covid19Dashboard.Entities;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ChartJs.Blazor;
using ChartJs.Blazor.Common;
using ChartJs.Blazor.Common.Axes;
using ChartJs.Blazor.Common.Axes.Ticks;
using ChartJs.Blazor.Common.Enums;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;
using Covid19Dashboard.Algorithms;

namespace Covid19Dashboard.Components
{
	public partial class RippleLineChartText
	{
		[Parameter] public string Title { get; set; } = "Data";
		[Parameter] public string? TextStyle { get; set; }
		[Parameter] public IList<TimePoint> Data { get; set; } = default!;
		[Parameter] public bool ShowTrendIcon { get; set; }
		[Parameter] public bool ShowTrendNumber { get; set; }

		[Parameter] public IEnumerable<LineChartThresholds> Thresholds { get; set; } = Enumerable.Empty<LineChartThresholds>();
		[Parameter] public string? InfoTooltip { get; set; }

		private double _dataToday;
		private double _dataYesterday;
		private bool _showTrend = false;

		private bool _showOnlyGraph;
		private LineDataset<TimePoint>? _mainDataset;

		private string TitleClass => _showOnlyGraph ? "rlct-title rlct-title-up" : "rlct-title rlct-title-down";
		private string TextClasses => _showOnlyGraph ? "rlct-text rlct-text-hide" : "rlct-text rlct-text-show";

		private int TotalDays { get; set; }

		private readonly LineConfig _chartConfig;
		private readonly TimeAxis _xAxis;
		private readonly LinearCartesianAxis _yAxis1;
		private readonly LinearCartesianAxis _yAxis2;

		public RippleLineChartText()
		{
			_chartConfig = CreateNewChartConfig();
			_showOnlyGraph = false;

			_xAxis = (TimeAxis)_chartConfig.Options.Scales.XAxes.First();
			_yAxis1 = (LinearCartesianAxis)_chartConfig.Options.Scales.YAxes[0];
			_yAxis2 = (LinearCartesianAxis)_chartConfig.Options.Scales.YAxes[1];
		}


		private void ToggleGraphView()
		{
			if (_mainDataset == null)
				return;
			_showOnlyGraph = !_showOnlyGraph;


			foreach (var t in Thresholds)
			{
				t.ShowLine(_showOnlyGraph);
			}

			if (_showOnlyGraph)
			{
				_xAxis.Display = AxisDisplay.True;
				_yAxis1.Ticks.MaxTicksLimit = 8;
				_yAxis1.GridLines.Display = true;
				_yAxis1.GridLines.Color = "#aaa";

				_chartConfig.Options.Title.Display = false;


				_mainDataset.BackgroundColor = "#FFFA";
				_mainDataset.BorderColor = "#FFFC";
			}
			else
			{
				_yAxis2.Display = AxisDisplay.True;
				_xAxis.Display = AxisDisplay.False;
				_yAxis1.Ticks.MaxTicksLimit = 2;
				_yAxis1.GridLines.Display = false;
				_chartConfig.Options.Title.Display = true;
				_yAxis1.GridLines.Color = "#0000";


				_mainDataset.BackgroundColor = "#666";
				_mainDataset.BorderColor = "#555";
			}
		}

		protected override void OnParametersSet()
		{
			if (Data == null || Data.Count < 1)
				throw new ArgumentNullException(nameof(Data));
			if (ShowTrendIcon && ShowTrendNumber)
				throw new ArgumentException($"You have to choose between {nameof(ShowTrendIcon)} and {nameof(ShowTrendNumber)}");

			//If the data has more then 200 entries, downsample it
			var data = Data;
			if (data.Count > 200)
			{
				data = Lttb.LargestTriangleThreeBuckets(Data, 200).ToList();
				_xAxis.Time.Min = new DateTime(2020, 03, 01);
			}
			else
			{
				_xAxis.Time.Min = Data.First().Time;
			}


			TotalDays = (int)(data.First().Time - DateTime.Now).TotalDays * -1;
			TotalDays += 1;
			_chartConfig.Options.Title.Text = $" - {TotalDays} Tage - ";
			_showTrend = ShowTrendNumber || ShowTrendIcon;
			_dataToday = data[^1].Y;
			var roundNumbers =
				_dataToday < 1000
					? (int)Math.Ceiling(3 - Math.Log10(_dataToday))
					: 0; //1000 -> 0, 100 -> 1, 10 -> 2
			_dataToday = Math.Round(_dataToday, roundNumbers);


			if (data.Count > 1)
			{
				_dataYesterday = Math.Round(data[^2].Y, (data[^1].Y > 100) ? 1 : 2);
			}
			else
			{
				_showTrend = false;
			}

			double min = data.Min(x => x.Y);
			double max = data.Max(x => x.Y);
			double den = 10;
			if (min > 1000)
				den = 100;
			min = Math.Floor(min / den) * den;
			max = Math.Ceiling(max / den) * den;

			_chartConfig.Data.Datasets.Clear();

			foreach (var cartesianAxis in _chartConfig.Options.Scales.YAxes)
			{
				var yAxis = (LinearCartesianAxis)cartesianAxis;
				yAxis.Ticks.Min = min;
				yAxis.Ticks.Max = max;
			}


			_mainDataset = new LineDataset<TimePoint>(data)
			{
				BorderColor = (_showOnlyGraph ? "#FFFA" : "#666"),
				BackgroundColor = (_showOnlyGraph ? "#FFFC" : "#555"),
				Fill = true,
				BorderWidth = 2,
				PointRadius = 0,
				LineTension = 0.1
			};


			_chartConfig.Data.Datasets.Add(_mainDataset);
			foreach (var t in Thresholds)
			{
				var thrData = t.GetDatasetFromExistingTimeTuples(data);
				t.ShowLine(_showOnlyGraph);
				_chartConfig.Data.Datasets.Add(thrData);
			}
		}

		private static (string icon, string style) GetIconAndStyle(double newVal, double oldVal)
		{
			string style = "width:100%;color:";
			if (newVal > oldVal)
				return ("arrow_upward", style + "#f00");
			else if (Math.Abs(newVal - oldVal) < 0.01)
				return ("indeterminate_check_box", style + "#fff");
			return ("arrow_downward", style + "#0f0");
		}


		LineConfig CreateNewChartConfig()
			=> new LineConfig
			{
				Options = new LineOptions
				{
					Title = new OptionsTitle { Text = $" - {TotalDays} Tage - ", Display = true, Position = Position.Bottom },
					Scales = new Scales
					{
						XAxes = new List<CartesianAxis>
						{
							new TimeAxis
							{
								Time = new TimeOptions(),
								Display = AxisDisplay.False,
								GridLines = new GridLines
								{
									Display = true,
									Color = "#aaa",
									BorderDash = new[] { 10.0, 5.0 }
								},
								Ticks = new TimeTicks { FontColor = "#aaa" }
							}
						},
						YAxes = new List<CartesianAxis>
						{
							new LinearCartesianAxis
							{
								Position = Position.Left,
								Ticks = new LinearCartesianTicks { MaxTicksLimit = 2, FontColor = "#aaa" },
								GridLines = new GridLines
								{
									Display = false,
									BorderDash = new[] { 10.0, 5.0 }
								}
							},
							new LinearCartesianAxis
							{
								Position = Position.Right,
								Ticks = new LinearCartesianTicks { MaxTicksLimit = 2, FontColor = "#aaa" },
								GridLines = new GridLines
								{
									Display = false
								}
							}
						}
					},
					Responsive = true,
					Legend = new Legend
					{
						Display = false
					},
				}
			};
	}
}