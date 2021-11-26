using System.Collections.Generic;
using System.Linq;
using ChartJs.Blazor.Common.Time;
using ChartJs.Blazor.LineChart;

namespace Covid19Dashboard.Entities
{
	public record LineChartThresholds(double Threshold, string ColorHex, int[]? Borderdash = null)
	{
		private static readonly int[] defaultBorderdash = new int[] { 10, 5 };
		private LineDataset<TimePoint>? thrData;


		internal LineDataset<TimePoint> GetDatasetFromExistingTimeTuples(ICollection<TimePoint> data)
		{
			var t = new List<TimePoint>() { new(data.First().Time, Threshold), new(data.Last().Time, Threshold) };
			thrData = new LineDataset<TimePoint>(t)
			{
				BorderDash = Borderdash ?? defaultBorderdash,
				BorderColor = ColorHex,
				BorderWidth = 2,
				PointRadius = 0
			};
			return thrData;
		}
		internal void ShowLine(bool show)
		{
			if (thrData != null)
				thrData.ShowLine = show;
		}
	}
}

