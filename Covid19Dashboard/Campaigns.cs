using Covid19Dashboard.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Covid19Dashboard
{
	public static class Campaigns
	{
		public static readonly List<Campaign> All = new List<Campaign> {
			new Campaign { Date = DateParser("24.11.2020") ,
				Text = @"Es gibt neue Funktionen!
* Cookies! Damit du diese Nachricht nur einmal siehst
* Klickbar! Klick mal auf den Graphen und schau was passiert
* Design! Paar kleine Design Anpassungen hier und da..."},
		};

		public static Campaign Current => All.Last();
		public const string CookieName = "campaigns";



		private static DateTime DateParser(string date) => DateTime.ParseExact(date, "dd.MM.yyyy", null);


	}
}
