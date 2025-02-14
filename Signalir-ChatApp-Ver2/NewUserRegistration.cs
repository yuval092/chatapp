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

        public BroadcastReceiver broadcastReceiver;

        private EditText userNameText;
        private EditText phoneNumberText;
        private EditText passwordText;
        private Button registerButton;
        private Button cancellButton;
        private ImageView home;
        private TextView link;

        //   TaskCompletionSource<string>   אשר מסתיים רק כאשר קורה אירוע TASK יוצרים 
        //                     במקרה שלנו כאשר הגיע תשובה מהשרת העם המשתמש קיים או לא 
        private TaskCompletionSource<string> responseCompletionSource;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.NewUserRegistration);

            //   הרשמה לארוע של תשובה מהשרת 
            broadcastReceiver = new SignalRBroadcastReceiver(this);
            RegisterReceiver(broadcastReceiver, new IntentFilter("BrodcastAddNewUserToDataBaseResults"));

            userNameText = FindViewById<EditText>(Resource.Id.userNameText);
            phoneNumberText = FindViewById<EditText>(Resource.Id.phoneNumberText);
            passwordText = FindViewById<EditText>(Resource.Id.passwordText);
            home = FindViewById<ImageView>(Resource.Id.HomeIcon);
            link = FindViewById<TextView>(Resource.Id.loginLink);

            registerButton = FindViewById<Button>(Resource.Id.registerButton);
            cancellButton = FindViewById<Button>(Resource.Id.cancelButton);
            home.Click += Home_Click;
            link.Click += Link_Click;


            //   כפתור הרשמה נלחץ
            registerButton.Click += async (sender, e) =>
            {


                if (!string.IsNullOrEmpty(userNameText.Text) && !string.IsNullOrEmpty(phoneNumberText.Text))


                    if (SignalRHub.Connection.State == HubConnectionState.Connected)
                    {

                        // שמחכה לסיום הארוע TASK הפעלת ה 
                        responseCompletionSource = new TaskCompletionSource<string>();

                        //   broadcast receiver  רישום 
                        var receiver = new SignalRBroadcastReceiver(this);
                        Application.Context.RegisterReceiver(receiver, new IntentFilter("BrodcastAddNewUserToDataBaseResults"));


                        string user = userNameText.Text;
                        string phonenumber = phoneNumberText.Text;
                        string password = passwordText.Text;
                        await SignalRHub.Connection.InvokeAsync("AddNewUserToDataBase", user, phonenumber,password);


                      //   מחכים לתשובה מהשרת  
                         string userNameExistInDataBase = await responseCompletionSource.Task;

                        // מבטלים הרשמה לאירוע
                        Application.Context.UnregisterReceiver(receiver);



                        if (userNameExistInDataBase == "UserAllreadyExist")
                            {
                                Toast.MakeText(this, "This User is Already Exist !!", ToastLength.Long).Show();
                                userNameExistInDataBase = "";
                            }

                            if (userNameExistInDataBase == "UserSuccessfullyRegistered")

                            {
                                //  מצא את השם של האפליקציה
                                string appName = Resources.GetString(Resource.String.app_name);

                                var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
                                var editor = prefs.Edit();
                                 //    מוחק שם משתמש קיים רק לצורך בדיקות
                                editor.Remove("UserName");
                                editor.PutString("UserName", userNameText.Text);
                                editor.Apply();
                                await SignalRHub.Connection.InvokeAsync("RegisterUser", userNameText.Text);
                                userNameText.Text = "";
                                phoneNumberText.Text = "";
                                userNameExistInDataBase = "";
                                Toast.MakeText(this, "User successfully registered", ToastLength.Long).Show();

                            Finish();

                            }
                            
                        }

                    

            };


            cancellButton.Click += async (sender, e) =>
            {

                Finish();

            };




        }
        private void Home_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
        }
        private void Link_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(LoginActivity));
            StartActivity(intent);
        }



        private class SignalRBroadcastReceiver : BroadcastReceiver
        {

            private readonly NewUserRegistration contextAactivity;

            public SignalRBroadcastReceiver(NewUserRegistration activity)
            {
                contextAactivity = activity;
            }
            public override void OnReceive(Context context, Android.Content.Intent intent)
            {
                string action = intent.Action;

                switch (action)
                {

                    case "BrodcastAddNewUserToDataBaseResults":
                        string user = intent.GetStringExtra("User");
                        string message = intent.GetStringExtra("Message");
                        contextAactivity.responseCompletionSource?.TrySetResult(message);

                        break;




                }
            }

        }




    }

}