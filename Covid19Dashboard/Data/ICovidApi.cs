using Covid19Dashboard.Entities;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Covid19Dashboard.Data
{
	public interface ICovidApi
	{
		ValueTask<ICovid19Data?> GetFromDistrictName(string name);
		ValueTask<ICovid19Data?> GetFromCityKey(string key);
		bool TryGetFromCityKey(string key, DateTime date, out ICovid19Data? data);
	}
}