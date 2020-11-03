using Covid19Dashboard.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Covid19Dashboard.Services
{
	public sealed class ArcgisService : ICovidApiService, IDisposable
	{
		const string databasePath = "data/arcgis/";
		const string query = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?outFields=cases7_per_100k,last_update,cases,GEN,rs,cases_per_100k,death_rate,deaths,cases_per_population&returnGeometry=false&outSR=4326&f=json";
		const string queryAll = query + "&where=1%3D1";
		const string cityNameQuery = query + "&where=gen=%27{0}%27";
		const string cityKeyQuery = query + "&where=rs=%27{0}%27";
		const int maxNumOfDataSets = 14;//14 Days


		private readonly HttpClient _http;
		private readonly Timer _timer;
		private readonly ILogger<ArcgisService> _logger;
		private readonly SortedDictionary<DateTime, ArcgisData> _pastData;

		private ArcgisData? currentDataSet;
		DateTime lastUpdate;



		public ArcgisService(IHttpClientFactory http, ILogger<ArcgisService> logger)
		{
			_http = http.CreateClient();
			_logger = logger;
			_pastData = new SortedDictionary<DateTime, ArcgisData>();
			ReadDatabase();
			_timer = new Timer(CheckForNewData, null, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(30));
		}

		public async ValueTask<ICovid19Data?> GetFromDistrictName(string name)
			=> currentDataSet?.Features.FirstOrDefault(x => x.Attributes.District == name)?.Attributes ?? await GetFromDistrictNameQuery(name);

		private Task<ICovid19Data?> GetFromDistrictNameQuery(string cityName)
		{
			var url = string.Format(cityNameQuery, cityName);
			return Query(url);
		}

		public async ValueTask<ICovid19Data?> GetFromCityKey(string key)
		{
			ICovid19Data? data = currentDataSet?.Features.FirstOrDefault(x => x.Attributes.CityKey == key)?.Attributes;
			data ??= await GetFromCityKeyQuery(key);
			_logger.LogDebug("Recevied for disctrict {0}", data?.District ?? key);
			return data;

		}


		private Task<ICovid19Data?> GetFromCityKeyQuery(string key)
		{
			var url = string.Format(cityKeyQuery, key);
			return Query(url);
		}

		public bool TryGetFromCityKey(string key, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null)
		{
			to ??= lastUpdate;
			if (!(currentDataSet?.Features.Any(t => t.Attributes.CityKey == key) ?? false))
			{
				data = Enumerable.Empty<ICovid19Data>();
				return false;
			}
			data = _pastData
				.Where(x => x.Key >= from && x.Key <= to)
				.Select(x => (ICovid19Data?)x.Value.Features.FirstOrDefault(t => t.Attributes.CityKey == key)?.Attributes)
				.OfType<ICovid19Data>();
			return data.Any();
		}


		private void ReadDatabase()
		{

			_logger.LogInformation("Read database...");
			if (!Directory.Exists(databasePath))
			{
				Directory.CreateDirectory(databasePath);
				return;
			}
			var files = Directory.GetFiles(databasePath, "*.json");
			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var data = JsonSerializer.Deserialize<ArcgisData>(json);
				lastUpdate = data.Features.First().Attributes.LastUpdate;
				currentDataSet = data;
				_pastData[lastUpdate] = data;
			}
			_logger.LogInformation("Database: {0} records found", _pastData.Count);
			CleanUpDatabase();
		}

		//This will remove the oldest datasets when there are
		//more then maxNumOfDataSets (default: 14) from data file location
		private void CleanUpDatabase()
		{
			var files = Directory.GetFiles(databasePath, "*.json");
			if (files.Length > maxNumOfDataSets)
			{
				_logger.LogInformation("Clean up database, delete {0} files...", files.Length - maxNumOfDataSets);
				foreach (var fileToDelete in files.Take(files.Length - maxNumOfDataSets))
				{
					File.Delete(fileToDelete);
				}
			}
			if (_pastData.Count > maxNumOfDataSets)
			{
				_logger.LogInformation("Clean up memory, delete {0} datasets...", _pastData.Count - maxNumOfDataSets);
				var rm = _pastData.Keys.Take(_pastData.Count - maxNumOfDataSets);
				foreach (var r in rm)
					_pastData.Remove(r);
			}
		}

		private async void CheckForNewData(object? obj)
		{
			try
			{
				var temp = await GetFromCityKeyQuery("01002");
				if (temp == null)
				{
					_logger.LogWarning("CheckForNewData: Data returned was null");
					return;
				}
				if (temp.LastUpdate > lastUpdate)
				{
					_logger.LogInformation("New updated arrived");
					var json = await QueryJson(queryAll);
					using (var hash = SHA256.Create())
					{
						//This is because sometimes there could be a new date,
						//but the data passed to us is the same as the data from yesterday...
						//Even the RKI is not perfect all the time...
						var jsonFromYesterday = File.ReadAllText(Path.Combine(databasePath, lastUpdate.ToString("yyyyMMdd") + ".json"));
						if (hash.ComputeHash(Encoding.UTF8.GetBytes(json)) ==
							hash.ComputeHash(Encoding.UTF8.GetBytes(jsonFromYesterday)))
						{
							_logger.LogInformation("Dataset from today is the same as yesterday --> Skip this dataset");
							return;
						}
					}
					lastUpdate = temp.LastUpdate;
					currentDataSet = JsonSerializer.Deserialize<ArcgisData>(json);
					_pastData[lastUpdate] = currentDataSet;
					File.WriteAllText(Path.Combine(databasePath, lastUpdate.ToString("yyyyMMdd") + ".json"), json);
					CleanUpDatabase();
				}
			}
			catch (Exception e)
			{
				_logger.LogError(e, "CheckForNewData");
			}
		}

		private async Task<ICovid19Data?> Query(string url)
		{
			string json = await QueryJson(url).ConfigureAwait(false);
			var data = JsonSerializer.Deserialize<ArcgisData>(json);
			if (data.Features == null)
			{
				throw new Exception(json);
			}
			return data.Features.FirstOrDefault()?.Attributes;
		}



		private async Task<string> QueryJson(string url)
		{
			var res = await _http.GetAsync(url).ConfigureAwait(false);
			if (res != null)
			{
				return await res.Content.ReadAsStringAsync().ConfigureAwait(false); ;
			}
			throw new Exception("No data");
		}

		public void Dispose()
		{
			_timer.Dispose();
		}
	}
}
