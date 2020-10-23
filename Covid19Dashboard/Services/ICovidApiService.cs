using Covid19Dashboard.Entities;
using System.Threading.Tasks;

namespace Covid19Dashboard.Services
{
	public interface ICovidApiService
	{
		ValueTask<ICovid19Data?> GetFromDistrictName(string name);
		ValueTask<ICovid19Data?> GetFromCityKey(string key);
		/// <summary>
		/// Tries to obtain data from the city key
		/// </summary>
		/// <param name="key">City Key</param>
		/// <param name="daysBeforeLastSet">Days before last data set (0: Today, 1: Yesterday, ...)</param>
		/// <param name="data">On success: the data return</param>
		/// <returns></returns>
		bool TryGetFromCityKey(string key, uint daysBeforeLastSet, out ICovid19Data? data);
	}
}