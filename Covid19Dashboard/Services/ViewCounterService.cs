using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Covid19Dashboard.Services
{
	public sealed class ViewCounterService : IDisposable
	{
		const string databaseFilePath = "data/views/views.json";

		private readonly ConcurrentDictionary<DateTime, int> _viewsPerDate;
		private readonly Timer _timer;
		private readonly ILogger<ViewCounterService> _logger;

		bool newViewsSinceLastWrite;
		public ViewCounterService(ILogger<ViewCounterService> logger)
		{
			var dir = Path.GetDirectoryName(databaseFilePath)!;
			ConcurrentDictionary<DateTime, int>? dict = null;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			if (File.Exists(databaseFilePath))
			{
				var json = File.ReadAllText(databaseFilePath);
				dict = JsonSerializer.Deserialize<ConcurrentDictionary<DateTime, int>>(json);
			}

			_viewsPerDate = dict ?? new ConcurrentDictionary<DateTime, int>();

			_timer = new Timer(WriteToFile, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
			_logger = logger;
		}
		public void NewView()
		{
			var now = DateTime.Now.Date;
			var cnt = _viewsPerDate.GetOrAdd(now, 0);
			_viewsPerDate[now] = cnt + 1;
			newViewsSinceLastWrite = true;
		}

		private bool TryGetViewsFromDay(DateTime date, out int views)
		{
			date = date.Date;
			return _viewsPerDate.TryGetValue(date, out views);
		}
		public int GetViewsFromDay(DateTime date)
		{
			var b = TryGetViewsFromDay(date, out int views);
			return b ? views : 0;
		}

		public int GetTodaysViews() => GetViewsFromDay(DateTime.Today);
		public int GetTotalViews() => _viewsPerDate.Sum(t => t.Value);

		public IReadOnlyDictionary<DateTime, int> GetAllViews()
		{
			return new ReadOnlyDictionary<DateTime, int>(_viewsPerDate);
		}

		private void WriteToFile(object? obj)
		{
			if (!newViewsSinceLastWrite)
				return;
			try
			{
				newViewsSinceLastWrite = false;
				var json = JsonSerializer.Serialize(_viewsPerDate);
				File.WriteAllText(databaseFilePath, json);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error by writing views to file");
			}
		}
		public void Dispose()
		{
			_timer.Dispose();
			WriteToFile(null);
		}
	}
}
