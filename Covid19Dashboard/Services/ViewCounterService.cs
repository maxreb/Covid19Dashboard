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
		const string databaseUniqueFilePath = "data/views/unique_views.json";

		private readonly ConcurrentDictionary<DateTime, int> _viewsPerDate;
		private readonly ConcurrentDictionary<DateTime, ConcurrentDictionary<string, int>> _uniqueViews;
		private readonly Timer _timer;
		private readonly ILogger<ViewCounterService> _logger;

		bool newViewsSinceLastWrite;
		public ViewCounterService(ILogger<ViewCounterService> logger)
		{
			var dir = Path.GetDirectoryName(databaseFilePath)!;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			ReadAndParse(out _viewsPerDate, databaseFilePath);
			ReadAndParse(out _uniqueViews, databaseUniqueFilePath);

			_timer = new Timer(WriteToFile, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
			_logger = logger;
		}


		private static void ReadAndParse<T>(out T data, string filePath) where T : class, new()
		{
			T? res = null;
			if (File.Exists(filePath))
			{
				var json = File.ReadAllText(filePath);
				res = JsonSerializer.Deserialize<T>(json);
			}
			data = res ?? new T();
		}

		public void NewView(string id)
		{
			var now = DateTime.Now.Date;

			var unique = _uniqueViews.GetOrAdd(now, _ => new ConcurrentDictionary<string, int>());
			var cnt = unique.GetOrAdd(id, 0);
			_uniqueViews[now][id] = cnt + 1;

			cnt = _viewsPerDate.GetOrAdd(now, 0);
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
		private bool TryGetUniqueViewsFromDay(DateTime date, out int views)
		{
			date = date.Date;
			var res = _uniqueViews.TryGetValue(date, out var val);
			views = val?.Count ?? 0;
			return res;
		}

		public int GetUniqueViewsFromDay(DateTime date)
		{
			TryGetUniqueViewsFromDay(date, out int views);
			return views;
		}

		public int GetTodaysViews() => GetViewsFromDay(DateTime.Today);
		public int GetTodaysUniqueViews() => GetUniqueViewsFromDay(DateTime.Today);
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
				json = JsonSerializer.Serialize(_uniqueViews);
				File.WriteAllText(databaseUniqueFilePath, json);
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
