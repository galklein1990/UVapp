  using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net.Http;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Microsoft.Band.Sensors;   // For UVI
using Newtonsoft.Json;


namespace UVapp
{
    class ServerRecommendations
    {
        private static string funcUri = "https://uvsafe.azurewebsites.net/api/HttpTrigger1?code=qa6RV0m9soe93D8mwKavYoAuVuTEIlMoAY7mH/TuSas4gs8Ge2cMnw==";

        public static async Task<string> getEnumUVRecommendation(UVIndexLevel uviLevel, HttpClient client)
        {
            var uri = new Uri(funcUri);
            var json = JsonConvert.SerializeObject(new { uvi = uviLevel.Name() });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Console.WriteLine("Connecting to server");

            HttpResponseMessage response = await client.PostAsync(uri, content);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return $"Server error: {response.StatusCode} \n {response.ReasonPhrase}";
            }
        }
    }
}