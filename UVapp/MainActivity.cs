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
        TextView appExposureTimeText, bandExposureTimeText, skinColorText, timeYouCanSpendText, uvMinutesLeftText; 
        Random rnd = new Random();
        
        Button connectBandButton;

        int currentUV;
        double samplingIntervalMinutes = 1;
        double uvMinutesSpent = 0;
        double uvMinutesLeft;
        long exposureMinutesApp;    // The exposure minutes we measure
        long exposureMinutesBand;   // The exposure minutes the band measures

        SkinType userSkinType = SkinType.Fitz2;

        UVSensor uvSensor;

        Timer uvMeasureTimer;
        DateTime lastUvSampleTime;
        bool connLostSinceLastSample = true;

        // Constant strings that don't need to be retyped
        readonly string bandConnTextBase = "Band Connection: ";
        readonly string samplingIntervalTextBase = "Sampling interval: ";
        readonly string currUVTextBase = "Current UV: ";
        readonly string uvMinutesTextBase = "UV Minutes Spent: ";
        readonly string skinColorTextBase = "Your skin type: ";
        readonly string appExposureTimeTextBase = "App measured exposure mins: ";
        readonly string bandExposureTimeTextBase = "Band measured exposure mins: ";
        readonly string timeYouCanSpendTextBase = "Minutes you can spend under current exposure: ";
        readonly string uvMinutesLeftTextBase = "UV Minutes Left: ";

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
            appExposureTimeText = FindViewById<TextView>(Resource.Id.appExposureTimeText);
            bandExposureTimeText = FindViewById<TextView>(Resource.Id.bandExposureTimeText);
            skinColorText = FindViewById<TextView>(Resource.Id.skinColorText);
            timeYouCanSpendText = FindViewById<TextView>(Resource.Id.timeYouCanSpendText);
            uvMinutesLeftText = FindViewById<TextView>(Resource.Id.uvMinutesLeftText);

            connectBandButton = FindViewById<Button>(Resource.Id.connectbtn);

            connectBandButton.Click += connectBandClick;

            uvMeasureTimer = new Timer(1000);   // Initial interval is 1 second and it is changed after first sample
            uvMeasureTimer.Elapsed += async (sender, args) =>
            {
                RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                    currentlySamplingText.Text = "Sampling UV...";
                });
                uvMeasureTimer.Interval = MinutesToMS(samplingIntervalMinutes);
                await sampleBandUV();
            };
            uvMeasureTimer.AutoReset = true;
            uvMeasureTimer.Enabled = false; // Will only be enabled when the band connects

            lastUvSampleTime = DateTime.MinValue;
            uvMinutesLeft = userSkinType.UVMinutesToBurn();

            samplingIntervalText.Text = $"Sampling interval: {samplingIntervalMinutes} minutes";
            currUVText.Text = "";
            bandConnText.Text = "";
            uvMinutesText.Text = $"UV minutes spent: {(int)uvMinutesSpent}";
            currentlySamplingText.Text = "";
            skinColorText.Text = skinColorTextBase + userSkinType.ToString();
            timeYouCanSpendText.Text = timeYouCanSpendTextBase + "safe";
            appExposureTimeText.Text = appExposureTimeTextBase + 0;
            bandExposureTimeText.Text = "";
            uvMinutesLeftText.Text = uvMinutesLeftTextBase + (int)uvMinutesLeft;
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutInt("currentUV", currentUV);
            outState.PutDouble("uvMinutesSpent", uvMinutesSpent);
            outState.PutDouble("uvMinutesLeft", uvMinutesLeft);
            outState.PutLong("exposureMinutesApp", exposureMinutesApp);
            outState.PutLong("exposureMinutesBand", exposureMinutesBand);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);

            currentUV = savedInstanceState.GetInt("currentUV");
            uvMinutesSpent = savedInstanceState.GetDouble("uvMinutesSpent");
            uvMinutesLeft = savedInstanceState.GetDouble("uvMinutesLeft");
            exposureMinutesApp = savedInstanceState.GetLong("exposureMinutesApp");
            exposureMinutesBand = savedInstanceState.GetLong("exposureMinutesBand");
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
                        connLostSinceLastSample = true;
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

        private async Task sampleBandUV()
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
                    int uviNum = UVvalues.UvEnumToInt(uviDescription);

                    long prevExposureMinutes = exposureMinutesBand;
                    exposureMinutesBand = args.SensorReading.UVExposureToday;
                    long exposureInterval = exposureMinutesBand - prevExposureMinutes;

                    // currentUV was still not updated, the UV from the previous sample is used
                    if (!connLostSinceLastSample)
                    {
                        TimeSpan timeSinceLastSample = DateTime.Now - lastUvSampleTime;
                        
                        if (currentUV != 0)
                            exposureMinutesApp += (long)timeSinceLastSample.TotalMinutes;
                        
                    }

                    uvMinutesSpent += currentUV * exposureInterval;
                    uvMinutesLeft -= currentUV * exposureInterval;
                    currentUV = uviNum;
                    connLostSinceLastSample = false;
                    lastUvSampleTime = DateTime.Now;

                    RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                        currUVText.Text = currUVTextBase + $"{currentUV} ({uviDescription})";
                        uvMinutesText.Text = uvMinutesTextBase + uvMinutesSpent;
                        uvMinutesLeftText.Text = uvMinutesLeftTextBase + (int)uvMinutesLeft;

                        if (currentUV != 0)
                        {
                            timeYouCanSpendText.Text = timeYouCanSpendTextBase + (int)uvMinutesLeft / currentUV;
                        }
                        else
                        {
                            timeYouCanSpendText.Text = timeYouCanSpendTextBase + "Safe";
                        }

                        appExposureTimeText.Text = appExposureTimeTextBase + exposureMinutesApp;
                        bandExposureTimeText.Text = bandExposureTimeTextBase + exposureMinutesBand;

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

