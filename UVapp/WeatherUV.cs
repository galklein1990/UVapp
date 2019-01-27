using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UVapp
{

    class WeatherUV
    {
        // This class retrieves uv from a weather API

        // TODO: Find a better API... This one only gives the day's peak...
        private static readonly string weatherApiUri = "http://api.openweathermap.org/data/2.5/uvi?appid=596ab39d6dfaf4e459ae952417934677";

        struct uvApiResponse
        {
            public double lat;
            public double lon;
            public string date_iso;
            public long date;
            public double value;
        }

        public static async Task<double> GetWeatherUvAsync(HttpClient httpClient, double locationLatitude, double locationLongitude)
        {
            Uri requestUri = new Uri(weatherApiUri + $"&lat={locationLatitude}&lon={locationLongitude}");
            HttpResponseMessage response = await httpClient.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<uvApiResponse>(content).value;
            }
            else
            {
                return -1;
            }
        }
    }
}