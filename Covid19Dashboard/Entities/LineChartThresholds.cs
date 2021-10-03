using ChartJs.Blazor.ChartJS.Common.Time;
using ChartJs.Blazor.ChartJS.LineChart;
using System.Collections.Generic;
using System.Linq;

namespace Covid19Dashboard.Entities
{
	public record LineChartThresholds(double Threshold, string ColorHex, int[]? Borderdash = null)
	{
		private static readonly int[] defaultBorderdash = new int[] { 10, 5 };
		private LineDataset<TimeTuple<double>>? thrData;


		internal LineDataset<TimeTuple<double>> GetDatasetFromExistingTimeTuples(IEnumerable<TimeTuple<double>> data)
		{
			var t = data.Select(x => new TimeTuple<double>(x.Time, Threshold));
			thrData = new LineDataset<TimeTuple<double>>(t)
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

