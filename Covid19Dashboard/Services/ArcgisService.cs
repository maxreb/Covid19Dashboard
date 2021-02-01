using Covid19Dashboard.Entities;
using Microsoft.Extensions.Configuration;
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
	public class ArcgisServiceOptions
	{
		public const string OptionsPath = "Arcgis";
		public int MaxDataSets { get; set; } = 365;
		public string DatabasePath { get; set; } = "data/arcgis/";
	}

	public class Dataset
	{
		public ArcgisData? CurrentDataSet { get; set; }
		public DateTime LastUpdate { get; set; }
		public SortedDictionary<DateTime, ArcgisData> PastData { get; } = new SortedDictionary<DateTime, ArcgisData>();
	}
	public sealed class ArcgisService : ICovidApiService, IDisposable
	{
		const string query = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?outFields=cases7_per_100k,last_update,cases,GEN,rs,cases_per_100k,death_rate,deaths,cases_per_population&returnGeometry=false&outSR=4326&f=json";
		const string queryState = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/Coronaf%C3%A4lle_in_den_Bundesl%C3%A4ndern/FeatureServer/0/query?outFields=Fallzahl,OBJECTID_1,LAN_ew_GEN,LAN_ew_EWZ,Aktualisierung,faelle_100000_EW,Death,cases7_bl_per_100k,cases7_bl,death7_bl,cases7_bl_per_100k_txt&returnGeometry=false&f=json";
		const string queryStateAll = queryState + "&where=1%3D1";
		const string queryStateKeyQuery = queryState + "&where=OBJECTID_1=%27{0}%27";
		const string queryAll = query + "&where=1%3D1";
		const string cityNameQuery = query + "&where=gen=%27{0}%27";
		const string cityKeyQuery = query + "&where=rs=%27{0}%27";


		private readonly HttpClient _http;
		private readonly Timer _timer;
		private readonly ILogger<ArcgisService> _logger;

		private readonly Dataset DataSetCity = new Dataset();
		private readonly Dataset DataSetState = new Dataset();


		ArcgisServiceOptions Options { get; }



		public ArcgisService(IHttpClientFactory http, ILogger<ArcgisService> logger, IConfiguration configuration)
		{
			_http = http.CreateClient();
			_logger = logger;
			Options = new ArcgisServiceOptions();
			configuration.GetSection(ArcgisServiceOptions.OptionsPath).Bind(Options);
			ReadDatabase();
			_timer = new Timer(CheckForNewData, false, TimeSpan.FromSeconds(0), TimeSpan.FromMinutes(30));
		}


		public async ValueTask<ICovid19Data?> GetFromCityKey(string cityKey, bool returnStateData = false)
		{
			ICovid19Data? data = Select(returnStateData).CurrentDataSet?.Features.FirstOrDefault(x => x.Attributes.CityKey == cityKey)?.Attributes;
			data ??= await GetFromCityKeyQuery(cityKey);
			_logger.LogDebug("Recevied for disctrict {0}", data?.District ?? cityKey);
			return data;

		}

		private Dataset Select(bool state) => state ? DataSetState : DataSetCity;
		private Task<ICovid19Data?> GetFromCityKeyQuery(string cityKey, bool returnStateData = false)
		{
			string key = cityKey;
			string url = cityKeyQuery;
			if (returnStateData)
			{
				key = CitiesRepository.GetStateFromCityKey(key).ToString();
				url = queryStateKeyQuery;
			}
			url = string.Format(url, key);
			return Query(url);
		}

		public bool TryGetFromCityKey(string cityKey, DateTime from, out IEnumerable<ICovid19Data> data, DateTime? to = null, bool returnStateData = false)
		{
			var dataset = Select(returnStateData);
			to ??= dataset.LastUpdate;
			//if dataset is null
			//or state key does not exist
			//or citykey does not exist
			//return empty data and false
			if (dataset.CurrentDataSet == null || !dataset.CurrentDataSet.Features.Any(keyFunc))
			{
				data = Enumerable.Empty<ICovid19Data>();
				return false;
			}
			data = dataset.PastData
				.Where(x => x.Key >= from && x.Key <= to)
				.Select(x => (ICovid19Data?)x.Value.Features.FirstOrDefault(keyFunc)?.Attributes)
				.OfType<ICovid19Data>();
			return data.Any();

			bool keyFunc(Feature t) => returnStateData ?
				t.Attributes.StateKey == CitiesRepository.GetStateFromCityKey(cityKey) :
				t.Attributes.CityKey == cityKey;
		}


		private void ReadDatabase()
		{

			_logger.LogInformation("Read database...");
			if (!Directory.Exists(Options.DatabasePath))
			{
				Directory.CreateDirectory(Options.DatabasePath);
				return;
			}
			var files = Directory.GetFiles(Options.DatabasePath, "*.json");
			foreach (var file in files)
			{
				bool state = file.Contains("-state.json");
				var json = File.ReadAllText(file);
				ArcgisData? data;
				if (state)
					data = StateDataToArcgisData(json);
				else
					data = JsonSerializer.Deserialize<ArcgisData>(json);
				if (data == null)
					_logger.LogError($"File {json} is empty.");
				else
				{
					var dataset = Select(state);
					dataset.LastUpdate = data.Features.First().Attributes.LastUpdate;
					dataset.CurrentDataSet = data;
					dataset.PastData[dataset.LastUpdate] = data;
				}
			}
			_logger.LogInformation("Database: {0} city and {0} state records found", DataSetCity.PastData.Count, DataSetState.PastData.Count);
			CleanUpDatabase();
		}

		//This will remove the oldest datasets when there are
		//more then maxNumOfDataSets (default: 14) from data file location
		private void CleanUpDatabase()
		{
			var allF = Directory.GetFiles(Options.DatabasePath, "*.json");

			cleanupFiles(allF.Where(t => t.EndsWith("-state.json")).ToArray());
			cleanupFiles(allF.Where(t => !t.EndsWith("-state.json")).ToArray());
			cleanupMem(DataSetCity);
			cleanupMem(DataSetState);

			void cleanupFiles(string[] files)
			{
				if (files.Length > Options.MaxDataSets)
				{
					_logger.LogInformation("Clean up database, delete {0} files...", files.Length - Options.MaxDataSets);
					var filesToDelete = files.OrderBy(f => f).Take(files.Length - Options.MaxDataSets);
					foreach (var file in filesToDelete)
					{
						_logger.LogDebug("Delete {file}", Path.GetFileName(file));
						File.Delete(file);
					}
				}
			}
			void cleanupMem(Dataset dataset)
			{
				if (dataset.PastData.Count > Options.MaxDataSets)
				{
					_logger.LogInformation("Clean up memory, delete {0} datasets...", dataset.PastData.Count - Options.MaxDataSets);
					var rm = dataset.PastData.Keys.Take(dataset.PastData.Count - Options.MaxDataSets);
					foreach (var r in rm)
						dataset.PastData.Remove(r);
				}
			}
		}

		private async void CheckForNewData(object? obj)
		{
			bool state = (bool)obj;
			try
			{
				var temp = await GetFromCityKeyQuery("01002", state);
				if (temp == null)
				{
					_logger.LogWarning("CheckForNewData: Data returned was null");
					return;
				}
				var dataset = Select(state);
				if (temp.LastUpdate > dataset.LastUpdate)
				{
					_logger.LogInformation("New updated arrived");
					var json = await QueryJson((state ? queryStateAll : queryAll));
					if (dataset.CurrentDataSet != null)
					{
						using (var hash = SHA256.Create())
						{
							//This is because sometimes there could be a new date,
							//but the data passed to us is the same as the data from yesterday...
							//Even the RKI is not perfect all the time...
							var jsonFromYesterday = File.ReadAllText(Path.Combine(Options.DatabasePath, dataset.LastUpdate.ToString("yyyyMMdd") + ".json"));
							if (hash.ComputeHash(Encoding.UTF8.GetBytes(json)) ==
								hash.ComputeHash(Encoding.UTF8.GetBytes(jsonFromYesterday)))
							{
								_logger.LogInformation("Dataset from today is the same as yesterday --> Skip this dataset");
								return;
							}
						}
					}
					dataset.LastUpdate = temp.LastUpdate;
					if (state)
						dataset.CurrentDataSet = StateDataToArcgisData(json);
					else
						dataset.CurrentDataSet = JsonSerializer.Deserialize<ArcgisData>(json);
					dataset.PastData[dataset.LastUpdate] = dataset.CurrentDataSet ?? throw new NullReferenceException("ArcgisData seems to be empty");
					string filename = dataset.LastUpdate.ToString("yyyyMMdd") + (state ? "-state" : "") + ".json";
					File.WriteAllText(Path.Combine(Options.DatabasePath, filename), json);
					if (state)//If it was for the state cleanup and return
						CleanUpDatabase();

				}
				if (!state)
					CheckForNewData(true);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "CheckForNewData");
			}
		}


		private static ArcgisData? StateDataToArcgisData(string jsonState)
		{
			jsonState = jsonState
				.Replace("\"cases7_bl_per_100k\"", "\"cases7_per_100k\"")
				.Replace("\"Fallzahl\"", "\"cases\"")
				.Replace("\"faelle_100000_EW\"", "\"cases_per_100k\"")
				.Replace("\"Death\"", "\"deaths\"");
			var res = JsonSerializer.Deserialize<ArcgisData>(jsonState);
			if (res is null)
				return null;
			foreach (var feature in res.Features)
			{
				feature.Attributes.DeathRate = feature.Attributes.TotalDeath / (double)feature.Attributes.TotalStatePopulation;
				feature.Attributes.CasesPerPopulation = feature.Attributes.Cases / (double)feature.Attributes.TotalStatePopulation;
			}
			return res;
		}

		private async Task<ICovid19Data?> Query(string url)
		{
			string json = await QueryJson(url).ConfigureAwait(false);
			var data = JsonSerializer.Deserialize<ArcgisData>(json);
			if (data?.Features == null)
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
