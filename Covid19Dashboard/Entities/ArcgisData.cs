using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
#nullable disable

namespace Covid19Dashboard.Entities
{
	public class UniqueIdField
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("isSystemMaintained")]
		public bool IsSystemMaintained { get; set; }
	}

	public class SpatialReference
	{
		[JsonPropertyName("wkid")]
		public int Wkid { get; set; }

		[JsonPropertyName("latestWkid")]
		public int LatestWkid { get; set; }
	}

	public class Field
	{
		[JsonPropertyName("name")]
		public string Name { get; set; }

		[JsonPropertyName("type")]
		public string Type { get; set; }

		[JsonPropertyName("alias")]
		public string Alias { get; set; }

		[JsonPropertyName("sqlType")]
		public string SqlType { get; set; }

		[JsonPropertyName("domain")]
		public object Domain { get; set; }

		[JsonPropertyName("defaultValue")]
		public object DefaultValue { get; set; }

		[JsonPropertyName("length")]
		public int? Length { get; set; }
	}

	public class Attributes : ICovid19Data
	{
		private string lastUpdateString;

		[JsonPropertyName("cases7_per_100k")]
		public double Cases7Per100k { get; set; }

		[JsonPropertyName("last_update")]
		public string LastUpdateString
		{
			get => lastUpdateString; set
			{
				lastUpdateString = value;
				LastUpdate = DateTime.ParseExact(value, @"dd.MM.yyyy, HH:mm U\hr", null);
			}
		}
		[JsonIgnore]
		public DateTime LastUpdate { get; private set; }
		[JsonPropertyName("cases")]
		public int Cases { get; set; }

		[JsonPropertyName("GEN")]
		public string District { get; set; }

		[JsonPropertyName("cases_per_100k")]
		public double CasesPer100k { get; set; }

		[JsonPropertyName("death_rate")]
		public double DeathRate { get; set; }
		[JsonPropertyName("cases_per_population")]
		public double CasesPerPopulation { get; set; }
		[JsonPropertyName("deaths")]
		public int TotalDeath { get; set; }
		[JsonPropertyName("RS")]
		public string CityKey { get; set; }
	}


	public class Feature
	{
		[JsonPropertyName("attributes")]
		public Attributes Attributes { get; set; }
	}

	public class ArcgisData
	{
		[JsonPropertyName("objectIdFieldName")]
		public string ObjectIdFieldName { get; set; }

		[JsonPropertyName("uniqueIdField")]
		public UniqueIdField UniqueIdField { get; set; }

		[JsonPropertyName("globalIdFieldName")]
		public string GlobalIdFieldName { get; set; }

		[JsonPropertyName("geometryType")]
		public string GeometryType { get; set; }

		[JsonPropertyName("spatialReference")]
		public SpatialReference SpatialReference { get; set; }

		[JsonPropertyName("fields")]
		public List<Field> Fields { get; set; }

		[JsonPropertyName("features")]
		public List<Feature> Features { get; set; }
	}
}