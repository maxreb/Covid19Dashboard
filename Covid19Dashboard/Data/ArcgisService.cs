using Covid19Dashboard.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Covid19Dashboard.Data
{
	public class ArcgisService : ICovidApi
	{
		const string query = "https://services7.arcgis.com/mOBPykOjAyBO2ZKk/arcgis/rest/services/RKI_Landkreisdaten/FeatureServer/0/query?outFields=cases7_per_100k,last_update,cases,GEN,cases_per_100k,death_rate,deaths,cases_per_population&returnGeometry=false&outSR=4326&f=json";
		string _cityNameQuery = query + "&where=gen=%27{0}%27";
		string _cityKeyQuery = query + "&where=rs=%27{0}%27";
		private readonly HttpClient _http;

		public ArcgisService(IHttpClientFactory http)
		{
			_http = http.CreateClient();
		}

		public Task<ICovid19Data?> GetFromCityName(string cityName)
		{
			var url = string.Format(_cityNameQuery, cityName);
			return Query(url);
		}

		public Task<ICovid19Data?> GetFromCityKey(string key)
		{
			var url = string.Format(_cityKeyQuery, key);
			return Query(url);
		}


		private async Task<ICovid19Data?> Query(string url)
		{
			var res = await _http.GetAsync(url);
			if (res != null)
			{
				var json = await res.Content.ReadAsStringAsync();
				var data = JsonSerializer.Deserialize<ArcgisData>(json);
				if (data.Features == null)
				{
					throw new Exception(json);
				}
				if (data.Features.Count > 0)
				{
					return data;
				}
				return null;

				//return JsonDocument.Parse(json);
			}
			throw new Exception("No data");
		}
	}
}
