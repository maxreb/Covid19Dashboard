using Covid19Dashboard.Entities;
using System.Text.Json;
using System.Threading.Tasks;

namespace Covid19Dashboard.Data
{
	public interface ICovidApi
	{
		Task<ICovid19Data?> GetFromCityName(string cityName);
		Task<ICovid19Data?> GetFromCityKey(string key);
	}
}