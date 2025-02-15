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
            loginBtn = FindViewById<Button>(Resource.Id.LoginButton);
            Cancelbtn = FindViewById<Button>(Resource.Id.cancelButton);

            home.Click += Home_Click;
            link.Click += Link_Click;
            Cancelbtn.Click += Cancel_Click;
            loginBtn.Click += Login_Click;
        }

        private void Login_Click(object sender, EventArgs e)
        {
            //  בדוק אם אחד מהשדות ריק
            if (string.IsNullOrEmpty(username.Text) || string.IsNullOrEmpty(password.Text))
            {
                Toast.MakeText(this, "Username or Password is empty", ToastLength.Long).Show();
                return; // צא מהפונקציה
            }

            if (SignalRHub.Connection.State == HubConnectionState.Connected)
            {
                // שלח את פרטי המשתמש החדש אל השרת על מנת שירשום אותו
                string user = username.Text;
                string password = password.Text;
                string result = await SignalRHub.Connection.InvokeAsync<string>(
                    "LoginUser",
                    user,
                    password
                );

                if (result == "Success")
                {
                    Toast.MakeText(this, "User Login Successfully", ToastLength.Long).Show();

                    // שמור את שם המשתמש במכשיר בשביל שימושים עתידיים
                    // בתוך shared preferences
                    // string appName = Resources.GetString(Resource.String.app_name);
                    // var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
                    // var editor = prefs.Edit();
                    // editor.PutString("UserName", user);
                    // editor.Apply();

                    // חזור לחלון הקודם
                    Finish();
                }
                else if (result == "UserDoesNotExist")
                {
                    Toast.MakeText(this, "This User Does Not Exist !!", ToastLength.Long).Show();
                }
            }
            else
            {
                Toast.MakeText(this, "Server Is Not Connected !!", ToastLength.Long).Show();
            }
        }
       
        private void Link_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(NewUserRegistration));
            StartActivity(intent);
        }

        private void Home_Click(object sender, EventArgs e)
        {
            Finish();
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }
}