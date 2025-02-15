using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Signalir_ChatApp
{
    [Activity(Label = "NewUserRegistration")]
    public class NewUserRegistration : Activity
    {
        private EditText userNameText;
        private EditText phoneNumberText;
        private EditText passwordText;
        private Button registerButton;
        private Button cancellButton;
        private ImageView home;
        private TextView link;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.NewUserRegistration);

            home = FindViewById<ImageView>(Resource.Id.HomeIcon);
            link = FindViewById<TextView>(Resource.Id.loginLink);
            cancellButton = FindViewById<Button>(Resource.Id.cancelButton);
            userNameText = FindViewById<EditText>(Resource.Id.userNameText);
            passwordText = FindViewById<EditText>(Resource.Id.passwordText);
            registerButton = FindViewById<Button>(Resource.Id.registerButton);
            phoneNumberText = FindViewById<EditText>(Resource.Id.phoneNumberText);

            // נגדיר מה יקרה בעת לחיצה על אחד מהכפתורים או תמונות
            link.Click += Link_Click;
            home.Click += Home_Click;
            cancellButton.Click += Cancel_Click;
            registerButton.Click += Register_Click;
        }

        private async void Register_Click(object sender, EventArgs e)
        {
            //  בדוק אם אחד מהשדות ריק
            if (string.IsNullOrEmpty(userNameText.Text) || string.IsNullOrEmpty(phoneNumberText.Text) || string.IsNullOrEmpty(passwordText.Text))
            {
                Toast.MakeText(this, "Username or Phone or Password is empty", ToastLength.Long).Show();
                return; // צא מהפונקציה
            }

            if (SignalRHub.Connection.State == HubConnectionState.Connected)
            {
                // שלח את פרטי המשתמש החדש אל השרת על מנת שירשום אותו
                string user = userNameText.Text.Trim();
                string phonenumber = phoneNumberText.Text.Trim();
                string password = passwordText.Text.Trim();
                string result = await SignalRHub.Connection.InvokeAsync<string>(
                    "RegisterNewUser",
                    user,
                    phonenumber,
                    password
                );

                if (result == "Success")
                {
                    Toast.MakeText(this, "User Registered Successfully", ToastLength.Long).Show();

                    // שמור את שם המשתמש במכשיר בשביל שימושים עתידיים
                    // בתוך shared preferences
                    // string appName = Resources.GetString(Resource.String.app_name);
                    // var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
                    // var editor = prefs.Edit();
                    // editor.PutString("UserName", userNameText.Text);
                    // editor.Apply();

                    // חזור לחלון הקודם
                    Finish();
                }
                else if (result == "UserExists")
                {
                    Toast.MakeText(this, "This User Already Exists !!", ToastLength.Long).Show();
                }
            }
            else
            {
                Toast.MakeText(this, "Server Is Not Connected !!", ToastLength.Long).Show();
            }
        }

        private void Home_Click(object sender, EventArgs e)
        {
            Finish();
        }
        private void Link_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(LoginActivity));
            StartActivity(intent);
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }
}