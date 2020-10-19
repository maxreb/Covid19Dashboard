using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Covid19Dashboard.Data
{
	public class CitiesGermany
	{
		//CSV File from:
		//https://github.com/andrena/java8-workshop/blob/master/demos/Liste-Staedte-in-Deutschland.csv
		const string CSVFile = "data/cities-germany.csv";
		public Dictionary<string, string> _citiesToKeys;
		public IReadOnlyDictionary<string, string> CitiesToKeys => _citiesToKeys;

		public CitiesGermany()
		{
			_citiesToKeys = new Dictionary<string, string>();
			using var parser = new TextFieldParser(CSVFile)
			{
				TextFieldType = FieldType.Delimited
			};
			parser.SetDelimiters(";");
			while (!parser.EndOfData)
			{
				
				//Processing row
				string[] fields = parser.ReadFields();
				if (parser.LineNumber == 2)
					continue;
				_citiesToKeys[fields[1]] = fields[0];
			}

		}


	}
}
