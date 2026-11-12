using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NexusIsland.Services;

public class WeatherReport
{
    public double Temperature { get; set; } = 24;
    public string Condition { get; set; } = "Clear Sky";
    public string Icon { get; set; } = "☀️";
    public string LocationName { get; set; } = "Local";
    public int Humidity { get; set; } = 55;
    public double WindSpeed { get; set; } = 8;
    public double TempMax { get; set; } = 28;
    public double TempMin { get; set; } = 19;
}

public class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    public WeatherReport CurrentWeather { get; private set; } = new();

    public async Task<WeatherReport> FetchWeatherAsync(double lat = 0, double lon = 0, string city = "Auto")
    {
        try
        {
            string locationName = city;

            if (lat == 0 && lon == 0)
            {
                // Auto-detect via ip-api
                try
                {
                    var geoJson = await Http.GetStringAsync("http://ip-api.com/json/?fields=status,city,lat,lon");
                    using var geoDoc = JsonDocument.Parse(geoJson);
                    if (geoDoc.RootElement.GetProperty("status").GetString() == "success")
                    {
                        lat = geoDoc.RootElement.GetProperty("lat").GetDouble();
                        lon = geoDoc.RootElement.GetProperty("lon").GetDouble();
                        locationName = geoDoc.RootElement.GetProperty("city").GetString() ?? "Local";
                    }
                }
                catch
                {
                    // Default to London / GMT fallback
                    lat = 51.5074;
                    lon = -0.1278;
                    locationName = "Local City";
                }
            }

            string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m&daily=temperature_2m_max,temperature_2m_min&timezone=auto";
            var weatherJson = await Http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(weatherJson);

            var current = doc.RootElement.GetProperty("current");
            double temp = current.GetProperty("temperature_2m").GetDouble();
            int humidity = current.GetProperty("relative_humidity_2m").GetInt32();
            int code = current.GetProperty("weather_code").GetInt32();
            double wind = current.GetProperty("wind_speed_10m").GetDouble();

            var daily = doc.RootElement.GetProperty("daily");
            double max = daily.GetProperty("temperature_2m_max")[0].GetDouble();
            double min = daily.GetProperty("temperature_2m_min")[0].GetDouble();

            var (cond, icon) = MapWmoCode(code);

            CurrentWeather = new WeatherReport
            {
                Temperature = Math.Round(temp, 1),
                Condition = cond,
                Icon = icon,
                LocationName = locationName,
                Humidity = humidity,
                WindSpeed = Math.Round(wind, 1),
                TempMax = Math.Round(max, 1),
                TempMin = Math.Round(min, 1)
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather fetch error: {ex.Message}");
        }

        return CurrentWeather;
    }

    private static (string Condition, string Icon) MapWmoCode(int code) => code switch
    {
        0 => ("Clear Sky", "☀️"),
        1 or 2 => ("Partly Cloudy", "🌤️"),
        3 => ("Overcast", "☁️"),
        45 or 48 => ("Foggy", "🌫️"),
        51 or 53 or 55 => ("Drizzle", "🌦️"),
        61 or 63 or 65 => ("Rain", "🌧️"),
        71 or 73 or 75 => ("Snow", "❄️"),
        80 or 81 or 82 => ("Rain Showers", "🌦️"),
        95 or 96 or 99 => ("Thunderstorm", "⚡"),
        _ => ("Cloudy", "⛅")
    };
}
