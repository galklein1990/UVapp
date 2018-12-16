using Android.App;

using Android.Widget;
using Android.OS;
using System;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.Band;
using Microsoft.Band.Sensors;


[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]
[assembly: UsesPermission(Microsoft.Band.BandClientManager.BindBandService)]

namespace UVapp
{
    [Activity(Label = "UVapp", MainLauncher = true)]
    public class MainActivity : Activity
    {

        private HttpClient httpClient;
        private IBandClient bandClient;

        TextView greeting , recommendation;
        Random rnd = new Random();
        int uv;
        Button uvButton;
        Button connectBandButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            greeting = FindViewById<TextView>(Resource.Id.greetTxt);
            recommendation = FindViewById<TextView>(Resource.Id.recommendTxt);
            uvButton = FindViewById<Button>(Resource.Id.getUVbtn);
            connectBandButton = FindViewById<Button>(Resource.Id.connectbtn);
            uvButton.Click += getUVClick;
            connectBandButton.Click += connectBandClick;
            
        }

        private async void connectBandClick(object sender, System.EventArgs e)
        {
            try
            { 
                IBandInfo[] pairedBands = BandClientManager.Instance.GetPairedBands();

                if (pairedBands.Length < 1)
                {
                    recommendation.Text = "Band not found!";
                    return;
                }

                bandClient = BandClientManager.Instance.Create(BaseContext, pairedBands[0]);

                recommendation.Text = "Connecting...";
                ConnectionState connState = await bandClient.ConnectTaskAsync();

                if (connState == ConnectionState.Connected)
                    recommendation.Text = "Connection Successful!";
                else
                    recommendation.Text = "Connection Failed!";
                
            }
            catch (BandException ex)
            {
                recommendation.Text = "Error!!\n";
                recommendation.Text += ex.Message;
            }
            
        }

        private void getUVClick(object sender, System.EventArgs e)
        {
            //try
            //{
                //uv = rnd.Next(0, 11);
            if (bandClient == null)
            {
                recommendation.Text = "Connect band first";
                return;
            }

            var uvSensor = bandClient.SensorManager.CreateUVSensor();
                
            uvSensor.ReadingChanged += (o, args) =>
            {
                    
                UVIndexLevel uvi = args.SensorReading.UVIndexLevel;
                RunOnUiThread(() => {
                    recommendation.Text = "Getting recommendation...";
                    // I don't know how to get a numerical 0-11 value
                    string recString = ClientRecommendations.getEnumUVRecommendation(uvi);
                    recommendation.Text = recString;
                    //uvSensor.StopReadings();
                });
                
            };

            recommendation.Text = "Getting UV Reading...";
            uvSensor.StartReadings();
                
           // }
            //catch (BandException ex)
           // {
              //  recommendation.Text = ex.Message;
           // }
        }


        
        private async Task<string> getEnumUVRecommendation(UVIndexLevel uvi)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }
            return await ServerRecommendations.getEnumUVRecommendation(uvi, httpClient);
        }

       
        private string getIntUVRecommendation(int uv)
        {
            return ClientRecommendations.getIntUVRecommendation(uv);
        }
   
    }
}

