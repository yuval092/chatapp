using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signalir_ChatApp
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity : Activity
    {
        ImageView home;
        EditText username, password;
        TextView link;
        Button btn, Cancelbtn;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Login);
            // Create your application here
            home = FindViewById<ImageView>(Resource.Id.HomeIcon);
            username = FindViewById<EditText>(Resource.Id.connectionText);
            password = FindViewById<EditText>(Resource.Id.connectionText);
            link = FindViewById<TextView>(Resource.Id.SignUpLink);
            btn = FindViewById<Button>(Resource.Id.LoginButton);
            Cancelbtn = FindViewById<Button>(Resource.Id.cancelButton);
            home.Click += Home_Click;
            link.Click += Link_Click;
            Cancelbtn.Click += async (sender, e) =>
            {

                Finish();

            };



        }

       
        private void Link_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(NewUserRegistration));
            StartActivity(intent);
        }

        private void Home_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
    }
}