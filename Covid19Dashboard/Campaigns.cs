using Covid19Dashboard.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Covid19Dashboard
{
	public static class Campaigns
	{
		public static readonly List<Campaign> All = new List<Campaign> {

new Campaign { DateUTC = DateUTCParser("24.11.2020") ,
				Text = @"Es gibt neue Funktionen!
* Cookies! Damit du diese Nachricht nur einmal siehst
* Klickbar! Klick mal auf den Graphen und schau was passiert
* Design! Paar kleine Design Anpassungen hier und da..."},

new Campaign { DateUTC = DateUTCParser("26.12.2020") ,
				Text = @"Merry X-Mas und ein frohes Neues! Bleibt alle Gesund... Ein paar neue Funktionen gibt es auch: 
* Maximaler Zeitraum lässt dich alle Daten, die ich gesammelt habe anschauen, nicht nur die letzten 7 Tage
* Wenn du ne Stadt eingibst, wird dir jetzt auch der richtige Link für die Stadt im Browser angezeigt zum Teilen oder für Favoriten
* Weniger Logs - mittlerweile benutzten so viele diese App, dass mein Server mit Log Daten überschwemmt wurde. Danke dafür <3"},

new Campaign{
DateUTC = DateUTCParser("03.02.2021"),
Text = @"Neue Funktionen - exklusiv nur für dich!

Die Landesregierung Schleswig-Holsteins hat einen Perspektivenplan vorgeschlagen. Wenn der durchgesetzt wird, ist es nicht nur wichtig, wie es in deinem Landkreis sondern auch wie es in deinem Bundesland aussieht.

Deswegen:
* Daten können nun auch für dein Bundesland angezeigt werden
* Hübscheres Design - auch für FireFox
* Viele BugFixes
* Softwarestruktur überarbeitet",
Links = new List<(Uri uri, string text)>
	{
		(new Uri("https://www.schleswig-holstein.de/DE/Landesregierung/I/_startseite/Artikel2021/I/210126_stufenplan.html"),"Zum Stufenplan"),
		(new Uri("https://github.com/maxreb/Covid19Dashboard"),"Zum Quellcode dieser Software (GitHub)")
	}
},
new Campaign { DateUTC = DateUTCParser("04.03.2021") ,
				Text = @"Einige von euch haben die neue Funktion schon erkannt, nach der gestrigen Bund-Länder-Konferenz möchte ich sie euch noch mal kurz präsentieren: Wenn ihr auf den oberen Graphen klickt seht ihr jetzt die wichtigen Inzidenzwerte gestrichelt und farbig hervorgehoben:

35:	 Orange
50:  Rot
100: Dunkel Rot

Falls ihr noch Anregungen habt könnt ihr gerne bei GitHub ein 'Issue' eintragen, link findet ihr unten.

Bleibt Gesund!"
,ShowDonation = false,
Links = new List<(Uri uri, string text)>{(new Uri("https://github.com/maxreb/Covid19Dashboard/issues"),"GitHub Issues")}
} };

		public static Campaign Current => All.Last();
		public const string CookieName = "campaigns";



		private static DateTime DateUTCParser(string date) => DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);


	}
}
