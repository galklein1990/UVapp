﻿//    [Activity(Label = "Register_activity")]
//public class Register_activity : Activity


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
using Android.Content;

namespace UVapp
{

    [Activity(Label = "Register_activity")]
    public class Register_activity : Activity
    {
        EditText username;
        EditText password;
        TextView txt;
        Button registerBtn ;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.registerLayout);
            registerBtn = FindViewById<Button>(Resource.Id.registerButton);
            username = FindViewById<EditText>(Resource.Id.username);
            password = FindViewById<EditText>(Resource.Id.password);
            txt = FindViewById<TextView>(Resource.Id.registertxt);
            registerBtn.Click += registerFunction;
        
        }

        public void registerFunction(object sender, System.EventArgs e)
        {
            //call cloud fuction to check if user is in database that returns a number describing a situation 
            UserManager userManager = new UserManager();
            User user = new User(username.Text, password.Text);
            int res = userManager.GetUserLoginStatus(username.Text, password.Text);
            switch (res)
            {
                case 0:
                    txt.Text = "user allready registered";
                    password.Text = "";
                    username.Text = "";
                    break;
                case 1:
                   
                    UserManager.createUser(user);
                    Intent mainIntent = new Intent(this, typeof(MainActivity));
                    mainIntent.PutExtra("name", username.Text);
                    txt.Text = "successfully registeration!";
                    password.Text = "";
                    username.Text = "";

                    MainActivity.loggedIn = true;
                    StartActivity(mainIntent);
                    break;
                default: break;
            }

            return;
        }


       
    }





}