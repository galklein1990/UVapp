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

using Newtonsoft.Json;

namespace UVapp
{
    //TBD: 7 days of the week exposete , Last day recorded, SkinType, maxUv
    class User
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int TimeExposed { get; set; } //counting minutes
        public string Date { get; set; }
        int skinType { get; set; }
        int maxUv { get; set; }
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
        public User(string userName, string password, SkinType skinType)
        {
            this.UserName = userName;
            this.Password = password;
            this.Date = getDateString();    //DateTime.Now.ToString()
            this.Id = userName + "-" + password + "-" + this.Date;
            this.skinType = (int)skinType;
            this.maxUv = 0;
        }
        public User(string userName, string password)       // TBD: just for now, for debugging purposes
        {
            this.UserName = userName;
            this.Password = password;
            this.Date = getDateString();
            this.Id = userName + "-" + password + "-" + this.Date;
            this.skinType = 0;
            this.maxUv = 0;
        }
        public User()       // TBD: just for now, for debugging purposes
        {
        }
        public string getDateString()
        {
            string[] dateArray = DateTime.Now.ToString().Split(' ')[0].Split('/');
            string dateStr = "";
            for(int i = 0; i < dateArray.Length - 1; i++)
            {
                dateStr = dateStr + dateArray[i] + ".";
            }
            dateStr = dateStr + dateArray[dateArray.Length - 1];
            return dateStr;
        }

    }
}