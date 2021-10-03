# Covid19 RKI Dashboard
A very simple Covid-19 Dashboard for German Cities using data from the Robert Koch Institut 

# Demo

Demo at: https://covid19.reble.eu  
You can set a direct link to any city you want by entering it after the URL  
Example: https://covid19.reble.eu/Hamburg

![Screenshot](https://github.com/maxreb/Covid19Dashboard/blob/main/doc/screenshot.png?raw=true)

# Installation
This works for Linux and for Windows

You need: git, .net core 5.0

```
git clone https://github.com/maxreb/Covid19Dashboard
git clone https://github.com/maxreb/RKIWebService
cd Covid19Dashboard
dotnet run --project Covid19Dashboard/Covid19Dashboard.csproj
```
And then open your Browser and browse to http://localhost:5000