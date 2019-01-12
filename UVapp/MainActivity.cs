using Android.App;

using Android.Widget;
using Android.OS;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Timers;

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
        private IBandConnectionCallback bandConnCallback;   // band connection "event listener"

        TextView currUVText, bandConnText, uvMinutesText, samplingIntervalText, currentlySamplingText;
        Random rnd = new Random();
        long currentUV;
        Button connectBandButton;
        double samplingIntervalMinutes = 1;
        double uvMinutesSpent = 0;

        UVSensor uvSensor;

        Timer uvMeasureTimer;

        // Constant strings that don't need to be retyped
        string bandConnTextBase = "Band Connection: ";
        string samplingIntervalTextBase = "Sampling interval: ";
        string currUVTextBase = "Current UV: ";
        string uvMinutesTextBase = "UV Minutes Spent: ";


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            bandConnText = FindViewById<TextView>(Resource.Id.bandConnectionText);
            currUVText = FindViewById<TextView>(Resource.Id.currentUVText);
            uvMinutesText = FindViewById<TextView>(Resource.Id.uvMinutesText);
            samplingIntervalText = FindViewById<TextView>(Resource.Id.samplingIntervalText);
            currentlySamplingText = FindViewById<TextView>(Resource.Id.currentlySamplingText);

            connectBandButton = FindViewById<Button>(Resource.Id.connectbtn);

            connectBandButton.Click += connectBandClick;

            uvMeasureTimer = new Timer(1000);
            uvMeasureTimer.Elapsed += async (sender, args) =>
            {
                RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                    currentlySamplingText.Text = "Sampling UV...";
                });
                uvMeasureTimer.Interval = MinutesToMS(samplingIntervalMinutes);
                await measureUV();
            };
            uvMeasureTimer.AutoReset = true;
            uvMeasureTimer.Enabled = false; // Will only be enabled when the band connects

            samplingIntervalText.Text = $"Current sampling interval: {samplingIntervalMinutes} minutes";
            currUVText.Text = "";
            bandConnText.Text = "";
            uvMinutesText.Text = $"UV minutes spent: {(int)uvMinutesSpent}";
            currentlySamplingText.Text = "";
        }

        private async void connectBandClick(object sender, System.EventArgs e)
        {
           
            IBandInfo[] pairedBands = BandClientManager.Instance.GetPairedBands();

            if (pairedBands.Length < 1)
            {
                bandConnText.Text = "Band not paired!";
                //BandClientManager.Instance.Dispose
                return;
            }

            if (bandClient == null)
                bandClient = BandClientManager.Instance.Create(BaseContext, pairedBands[0]);

            ConnectionState connState = bandClient.ConnectionState;

            if (!bandClient.IsConnected)
            {
                if (bandConnCallback != null)
                {
                    bandClient.UnregisterConnectionCallback();
                }

                bandConnCallback = bandClient.RegisterConnectionCallback(async connectionState =>
                {
                    RunOnUiThread(() =>
                    {
                        bandConnText.Text = bandConnTextBase + connectionState.Name();
                    });
                    
                    if (connectionState == ConnectionState.Connected)
                    {
                        //await measureUV();
                        uvMeasureTimer.Start();
                    }
                    else
                    {
                        uvMeasureTimer.Stop();
                    }
                });

                try
                {
                    connState = await bandClient.ConnectTaskAsync();
                    if (connState != ConnectionState.Connected)
                    {
                        bandConnText.Text += "\nBand connection failed";
                    }
                }
                catch(BandException ex)
                {
                    bandConnText.Text = "Band Connection Error: " + ex.Message;
                }
            } 
        }

        private async Task measureUV()
        {
            try
            {
                //uv = rnd.Next(0, 11);
                if (bandClient == null)
                {
                    return;
                }

                if (uvSensor == null)
                    uvSensor = bandClient.SensorManager.CreateUVSensor();

                UVIndexLevel uviDescription = null;

                uvSensor.ReadingChanged += (o, args) =>
                {

                    uviDescription = args.SensorReading.UVIndexLevel;
                    long uviNum = args.SensorReading.UVExposureToday;
                    uvMinutesSpent += currentUV*(samplingIntervalMinutes);
                    currentUV = uviNum;

                    RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                        currUVText.Text = currUVTextBase + $"{currentUV} ({uviDescription})";
                        uvMinutesText.Text = uvMinutesTextBase + uvMinutesSpent;
                        currentlySamplingText.Text = "";
                    });
                };
                
                uvSensor.StartReadings();
            }
            catch (BandException ex)
            {
                currUVText.Text = "Error Reading UV: " + ex.Message;
            }
        }

        /*
        private async void getUVClick(object sender, System.EventArgs e)
        {
            try
            {
                //uv = rnd.Next(0, 11);
            if (bandClient == null)
            {
                recommendation.Text = "Connect band first";
                return;
            }

            var uvSensor = bandClient.SensorManager.CreateUVSensor();
            UVIndexLevel uviDescription = null;
            Boolean uvRead = false;

            string recString;
            uvSensor.ReadingChanged += (o, args) =>
            {
                    
                uviDescription = args.SensorReading.UVIndexLevel;
                long uviNum = args.SensorReading.UVExposureToday;
                uvRead = true;
                timesUVRead += 1;

                RunOnUiThread(async () => {      // To access the text, you need to run on ui thread
                    recommendation.Text = "Getting recommendation...";

                    recommendation.Text = $"Exposure today is {uviNum}\n";
                    recString = await getEnumUVRecommendation(uviDescription);
                    recommendation.Text += recString;
                    recommendation.Text += $"\n Read {timesUVRead} times";
                });

                uvSensor.StopReadings();
                uvSensor.StartReadings();
            };

            recommendation.Text = "Getting UV Reading...";
            uvSensor.StartReadings();


            }
            catch (BandException ex)
            {
                recommendation.Text = ex.Message;
                ex.ErrorType.Name();
            }
        }
        */

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
        

        double MinutesToMS(double minutes)
        {
            return 60 * 1000 * minutes;
        }
        double msToMinutes(double milliseconds)
        {
            return milliseconds / (60 * 1000);
        }
    }
}

