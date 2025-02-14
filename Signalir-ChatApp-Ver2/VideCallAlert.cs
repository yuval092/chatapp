using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using System;
using static Xamarin.Essentials.Platform;

namespace Signalir_ChatApp
{
    //   משתמשים באקטיביטי כמו דיאלוג מגדירים  
    //  Theme = "@android:style/Theme.DeviceDefault.Dialog"

    [Activity(Label = "VideoCallDialog", Theme = "@android:style/Theme.DeviceDefault.Dialog")]
    public class VideoCallDialogActivity : Activity
    {
        //   מעבירים פונקציה חיצונית לאקטיביטי 
        public static Action<bool, string ,string> ResultCallback;
        private string message;
        private string audioOrvideo;
        private AudioPlayer audioPlayer;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);


            SetContentView(Resource.Layout.VideoCallAlert);


            message = Intent.GetStringExtra("Message");
            audioOrvideo = Intent.GetStringExtra("VideoOrAudio");

            
            if (audioOrvideo == "Video") Title = message + ": שיחת וידאו  ";
            else Title = message + ": שיחת נכנסת ";

            TextView popupMessage = FindViewById<TextView>(Resource.Id.popup_message);
            popupMessage.Text = "Video Call From: ";

            Button yesButton = FindViewById<Button>(Resource.Id.yes_button);
            Button noButton = FindViewById<Button>(Resource.Id.no_button);
            audioPlayer = new AudioPlayer();
            audioPlayer.PlayAudio("ring.mp3");

            // Yes button click
            yesButton.Click += (sender, e) =>
            {

                audioPlayer.StopAudio();
                ResultCallback?.Invoke(true, message, audioOrvideo);
                Finish();
            };

            // No button click
            noButton.Click += (sender, e) =>
            {

                audioPlayer.StopAudio();
                ResultCallback?.Invoke(false, message, audioOrvideo);
                Finish();
            };

            //  ניתוק אוטומטי אם לא עונים לאחר עשר שניות
            var handler = new Handler(Looper.MainLooper);
            handler.PostDelayed(() =>
            {
                if (SignalRHub.CallIsActive == false)

                {
                    audioPlayer.StopAudio();
                    ResultCallback?.Invoke(false, message, audioOrvideo);
                    Finish();
                }
            }, 10000);




        } //==


        protected override void OnPause()
        {
            base.OnPause();
            audioPlayer.StopAudio();
            Finish();

        }




    }

}