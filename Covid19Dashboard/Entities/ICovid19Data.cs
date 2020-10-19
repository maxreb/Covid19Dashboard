using System;

namespace Covid19Dashboard.Entities
{
	public interface ICovid19Data
	{
		int Cases { get; }
		double Cases7Per100k { get; }
		double CasesPer100k { get; }
		DateTime LastUpdate { get; }
		string District { get; }
		int TotalDeath { get; }
		double DeathRate { get; }
		double CasesPerPopulation { get; }
	}
}