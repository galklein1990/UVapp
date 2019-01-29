using Android.App;
using Android.Content;

using Android.Widget;
using Android.OS;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Timers;

using Microsoft.Band;
using Microsoft.Band.Sensors;

using Android.Support.V4.App;
using TaskStackBuilder = Android.Support.V4.App.TaskStackBuilder;


[assembly: UsesPermission(Android.Manifest.Permission.Bluetooth)]
[assembly: UsesPermission(Microsoft.Band.BandClientManager.BindBandService)]

namespace UVapp
{
    [Activity(Label = "UVapp", MainLauncher = true)]
    public class MainActivity : Activity
    {

        public static readonly string CHANNEL_ID = "notificationChannel1";
        internal static readonly string Key = "update";
        internal static string UVKey = "-1";
        // public string update = "uv detected";

        public static bool loggedIn = false; //should be replaced with a function -getter- from cloud

        private HttpClient httpClient;
        private IBandClient bandClient;
        private IBandConnectionCallback bandConnCallback;   // band connection "event listener"

        TextView currUVText, currUVWeatherText, bandConnText, uvMinutesText, samplingIntervalText, currentlySamplingText;
        TextView appExposureTimeText, bandExposureTimeText, skinColorText, timeYouCanSpendText, uvMinutesLeftText, gettingUvWeatherText; 
        Random rnd = new Random();
        
        Button connectBandButton;

        bool firstExposureNotificationSent = false;
        bool halfAllowedUVnotificationSent = false;

        int currentUV;
        double weatherCurrentUV;
        double samplingIntervalMinutes = 1;
        double weatherRequestIntervalMinutes = 30;  // It's actually limited to 60 requests per minute
        double uvMinutesSpent = 0;
        double uvMinutesLeft;
        long exposureMinutesApp;    // The exposure minutes we measure
        long exposureMinutesBand;   // The exposure minutes the band measures

        SkinType userSkinType = SkinType.Fitz2;

        UVSensor uvSensor;

        Timer uvWeatherTimer;
        Timer uvSampleTimer;
        DateTime lastUvSampleTime;
        bool connLostSinceLastSample = true;

        // Where I spend all my time
        readonly double defaultLatitude = 32.1148223;
        readonly double defaultLongitude = 34.8070341;

        // Constant strings that don't need to be retyped
        readonly string bandConnTextBase = "Band Connection: ";
        readonly string samplingIntervalTextBase = "Sampling interval: ";
        readonly string currUVTextBase = "Measured UV: ";
        readonly string currUVWeatherTextBase = "Current UV according to Weather: ";
        readonly string uvMinutesTextBase = "Accumulated UV: ";
        readonly string skinColorTextBase = "Your skin type: ";
        readonly string appExposureTimeTextBase = "App measured exposure mins: ";
        readonly string bandExposureTimeTextBase = "Exposure time today: ";
        readonly string timeYouCanSpendTextBase = "Additional time you can spend under current level: ";
        readonly string uvMinutesLeftTextBase = "UV Minutes Left: ";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            //login
            if (!loggedIn) {
                Intent loginIntent = new Intent(this, typeof(Login_activity));
                StartActivity(loginIntent);
            }
            bandConnText = FindViewById<TextView>(Resource.Id.bandConnectionText);
            currUVText = FindViewById<TextView>(Resource.Id.currentUVText);
            currUVWeatherText = FindViewById<TextView>(Resource.Id.currentUVWeatherText);
            uvMinutesText = FindViewById<TextView>(Resource.Id.uvMinutesText);
            samplingIntervalText = FindViewById<TextView>(Resource.Id.samplingIntervalText);
            currentlySamplingText = FindViewById<TextView>(Resource.Id.currentlySamplingText);
            appExposureTimeText = FindViewById<TextView>(Resource.Id.appExposureTimeText);
            bandExposureTimeText = FindViewById<TextView>(Resource.Id.bandExposureTimeText);
            skinColorText = FindViewById<TextView>(Resource.Id.skinColorText);
            timeYouCanSpendText = FindViewById<TextView>(Resource.Id.timeYouCanSpendText);
            uvMinutesLeftText = FindViewById<TextView>(Resource.Id.uvMinutesLeftText);
            gettingUvWeatherText = FindViewById<TextView>(Resource.Id.gettingUvFromWeatherText);

            connectBandButton = FindViewById<Button>(Resource.Id.connectbtn);

            connectBandButton.Click += ConnectBandClick;

            uvSampleTimer = new Timer(1000);   // Initial interval is 1 second and it is changed after first sample
            uvSampleTimer.Elapsed += async (sender, args) =>
            {
                RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                    currentlySamplingText.Text = "Sampling UV...";
                });
                uvSampleTimer.Interval = MinutesToMS(samplingIntervalMinutes);
                await SampleBandUV();
            };
            uvSampleTimer.AutoReset = true;
            uvSampleTimer.Enabled = false; // Will only be enabled when the band connects

            uvWeatherTimer = new Timer(1000);     // Initial interval is 1 second and it is changed after first request
            uvWeatherTimer.Elapsed += async (sender, args) =>
            {
                uvWeatherTimer.Interval = MinutesToMS(weatherRequestIntervalMinutes);
                if (httpClient == null)
                {
                    httpClient = new HttpClient();
                }

                RunOnUiThread(() =>
                {
                    gettingUvWeatherText.Text = "Getting UV from weather...";
                });
                weatherCurrentUV = await WeatherUV.GetWeatherUvAsync(httpClient, defaultLatitude, defaultLongitude);

                RunOnUiThread(() =>
                {
                    gettingUvWeatherText.Text = "";
                    currUVWeatherText.Text = currUVWeatherTextBase + (int)weatherCurrentUV;
                });
            };
            uvWeatherTimer.AutoReset = true;
            uvWeatherTimer.Enabled = true;

            lastUvSampleTime = DateTime.MinValue;
            if (savedInstanceState == null)
            {
                uvMinutesLeft = userSkinType.UVMinutesToBurn();
            }
            else
            {
                uvMinutesLeft = savedInstanceState.GetDouble("uvMinutesLeft", userSkinType.UVMinutesToBurn());
            }

            samplingIntervalText.Text = ""; //$"Sampling interval: {samplingIntervalMinutes} minutes";
            currUVText.Text = "";
            currUVWeatherText.Text = "";
            bandConnText.Text = "";
            uvMinutesText.Text = uvMinutesTextBase + $"{(int)uvMinutesSpent} ({(int)(uvMinutesSpent/userSkinType.UVMinutesToBurn())}%)";
            currentlySamplingText.Text = "";
            skinColorText.Text = skinColorTextBase + userSkinType.RomanNumeralsName();
            timeYouCanSpendText.Text = timeYouCanSpendTextBase + "Safe";
            appExposureTimeText.Text = ""; //appExposureTimeTextBase + 0;
            bandExposureTimeText.Text = "";
            uvMinutesLeftText.Text = ""; //uvMinutesLeftTextBase + (int)uvMinutesLeft;
            gettingUvWeatherText.Text = "";
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

        private async void ConnectBandClick(object sender, System.EventArgs e)
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
                        uvSampleTimer.Start();
                    }
                    else
                    {
                        connLostSinceLastSample = true;

                        RunOnUiThread(() =>
                        {
                            currentlySamplingText.Text = "";
                        });
                        
                        uvSampleTimer.Stop();
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

        private async Task SampleBandUV()
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
                    long exposureSinceLastSample = exposureMinutesBand - prevExposureMinutes;

                    // currentUV was still not updated, the UV from the previous sample is used
                    if (!connLostSinceLastSample)
                    {
                        TimeSpan timeSinceLastSample = DateTime.Now - lastUvSampleTime;
                        
                        if (currentUV != 0)
                            exposureMinutesApp += (long)timeSinceLastSample.TotalMinutes;
                        
                    }

                    if (currentUV != 0)
                    {
                        uvMinutesSpent += currentUV * exposureSinceLastSample;
                        uvMinutesLeft -= currentUV * exposureSinceLastSample;
                    }
                    else
                    {
                        /* If the last uv sample was 0, we use the weather UV
                         * Note that exposureSinceLastSample may be 0 if there
                         * was no exposure
                         */
                        uvMinutesSpent += weatherCurrentUV * exposureSinceLastSample;
                        uvMinutesLeft -= weatherCurrentUV * exposureSinceLastSample;
                    }

                    currentUV = uviNum;
                    connLostSinceLastSample = false;
                    lastUvSampleTime = DateTime.Now;

                    if (currentUV > 0 && !firstExposureNotificationSent) {
                        NotifyUser($"UV level of {currentUV} detected!", $"If you are going to be exposed to the sun for more than {FormatTimeAmountForUser(userSkinType.MinutesToBurn(currentUV))}, wear protective clothing, hat and UV-blocking sunglasses and apply SPF 30+ sunscreen");
                        firstExposureNotificationSent = true;
                    }

                    if (uvMinutesLeft < userSkinType.UVMinutesToBurn()/2)
                    {
                        NotifyUser("You've been exposed to over 50% of your allowed UV today!", $"Try to avoid additional sun exposure and make sure to apply SPF 30+ sunscreen and wear protective clothing, especially if you are going to be exposed for more than { FormatTimeAmountForUser(uvMinutesLeft / currentUV)}");
                        halfAllowedUVnotificationSent = true;
                    }

                    RunOnUiThread(() => {      // To access the text, you need to run on ui thread
                        currUVText.Text = currUVTextBase + $"{currentUV} ({uviDescription})";
                        uvMinutesText.Text = uvMinutesTextBase + $"{(int)uvMinutesSpent} ({(int)(uvMinutesSpent*100 / userSkinType.UVMinutesToBurn())}%)";
                        //uvMinutesLeftText.Text = uvMinutesLeftTextBase + (int)uvMinutesLeft;

                        if (currentUV != 0)
                        {

                            timeYouCanSpendText.Text = timeYouCanSpendTextBase + FormatTimeAmountForUser(uvMinutesLeft / currentUV);
                        }
                        else
                        {
                            timeYouCanSpendText.Text = timeYouCanSpendTextBase + "Safe";
                        }

                        //appExposureTimeText.Text = appExposureTimeTextBase + exposureMinutesApp;
                        bandExposureTimeText.Text = bandExposureTimeTextBase + FormatTimeAmountForUser(exposureMinutesBand);

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

        private async Task<string> GetEnumUVRecommendation(UVIndexLevel uvi)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient();
            }
            return await ServerRecommendations.getEnumUVRecommendation(uvi, httpClient);
        }

       
        private string GetIntUVRecommendation(int uv)
        {
            return ClientRecommendations.getIntUVRecommendation(uv);
        }
        
        public static string FormatTimeAmountForUser(double minutes)
        {
            if (minutes < 120)
            {
                return $"{(int)minutes} minutes";
            }
            else
            {
                return $"{(int)(minutes / 60)} hours and {((int)minutes)%60} minutes";
            }
        }
        double MinutesToMS(double minutes)
        {
            return 60 * 1000 * minutes;
        }
        double MsToMinutes(double milliseconds)
        {
            return milliseconds / (60 * 1000);
        }

        //////////////////////NOTIFICATIONS/////////////////////
        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.
                return;
            }

            var name = Resources.GetString(Resource.String.channel_name);
            var description = GetString(Resource.String.channel_describtion);
            var channel = new NotificationChannel(CHANNEL_ID, name, NotificationImportance.Default)
            {
                Description = description
            };

            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);
        }



        void NotifyUser(string title, string update)
        {

            Intent notifyIntent = new Intent(this, typeof(NotificationService));
            notifyIntent.PutExtra("update" , update);
            notifyIntent.PutExtra("title", title);
            this.StartService(notifyIntent);

        }
    }
}

