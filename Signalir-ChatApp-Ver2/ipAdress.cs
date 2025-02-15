using Android.App;
using Android.Content;
using Android.InputMethodServices;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signalir_ChatApp
{
    [Activity(Label = "ipAdress")]
    public class ipAdress : Activity
    {
        private ImageView home;
        private EditText ipAddressText;
        private EditText portText;
        private Button okButton;
        private Button cancellButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.ipAdress);


            ipAddressText = FindViewById<EditText>(Resource.Id.IPText);
            portText = FindViewById<EditText>(Resource.Id.portText);
            okButton = FindViewById<Button>(Resource.Id.okButton);
            cancellButton = FindViewById<Button>(Resource.Id.cancelButton);
            home = FindViewById<ImageView>(Resource.Id.HomeIcon);
            home.Click += Home_Click;

            string appName = Resources.GetString(Resource.String.app_name);

            var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);


            string savedServerIpAddress = prefs.GetString("IpAddress", "NoServerIp");



            if ((savedServerIpAddress != "NoServerIp") && (savedServerIpAddress != null))
            {
                savedServerIpAddress = prefs.GetString("IpAddress", "NoServerIp");
                //  רוצים להפריד בן כתובת איפי לפורט
                //  :  נחפש 
                string[] parts = savedServerIpAddress.Split(':');
                ipAddressText.Text = parts[0];
                portText.Text = parts[1];
            }




            //   כפתור הרשמה נלחץ
            okButton.Click += (sender, e) =>
            {
                string ipPlusPort = ipAddressText.Text + ":" + portText.Text;
                string appName = Resources.GetString(Resource.String.app_name);

                var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
                var editor = prefs.Edit();

                editor.Remove("IpAddress");
                editor.PutString("IpAddress", ipPlusPort);
                editor.Apply();
                Finish();
            };

            cancellButton.Click += (sender, e) =>
            {
                Finish();
            };
        }
        private void Home_Click(object sender, EventArgs e)
        {
            Finish();
        }
    }

}