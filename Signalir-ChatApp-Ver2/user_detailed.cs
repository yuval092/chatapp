using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using AndroidX.Browser.CustomTabs;
using FFImageLoading.Helpers;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;








namespace Signalir_ChatApp
{

    [Activity(Label = "user_detailed")]
    public class user_detailed : Activity
    {

        public DatabaseHelper userDetailedDbHelper { get; set; }

        private Android.Widget.ListView chatListView;

        private EditText inputBox;
        private ImageView sendButton;
        private ImageView sendPhotoButton;
        private TextView remoteUserNameDisplay;
        private List<ChatMessage> messages;
        private ChatAdapter chatAdapter;



        public ImageView startVideoCall;
        public ImageView startAudioCall;

        string localUserRegestrationName = "";
        string remoteUserName = "";
        public static bool User_DetailedactivtyFocoused = false;
        List<ConnectedUsersList> conectedusers = new List<ConnectedUsersList> { };

        private BroadcastReceiver broadcastReceiver;
        private const int PickFileRequestCode = 1000;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.user_detailed); // Set the new layout


            localUserRegestrationName = Intent.GetStringExtra("localUserName");
            remoteUserName = Intent.GetStringExtra("remoteUserName");

            remoteUserNameDisplay = FindViewById<TextView>(Resource.Id.userName);
            remoteUserNameDisplay.Text = remoteUserName;

            //====================================================================
            //          שאנו רוצים להאזין    Broadcast  הרשמה 
            //====================================================================
            broadcastReceiver = new SignalRBroadcastReceiver(this);

            RegisterReceiver(broadcastReceiver, new IntentFilter("ReceiveMessage"));
            RegisterReceiver(broadcastReceiver, new IntentFilter("FileWatingToDownload"));
            RegisterReceiver(broadcastReceiver, new IntentFilter("OpenWebrtcPage"));

            //=========================================================================
            //                  SQL light   חיבור 
            //=========================================================================

            userDetailedDbHelper = new DatabaseHelper(Android.App.Application.Context);



            chatListView = FindViewById<Android.Widget.ListView>(Resource.Id.messagesListView);


            inputBox = FindViewById<EditText>(Resource.Id.messageInput);
            sendButton = FindViewById<ImageView>(Resource.Id.sendButton);

            sendPhotoButton = FindViewById<ImageView>(Resource.Id.sendPhoto);

            // Initialize message list and adapter
            messages = new List<ChatMessage>();
            chatAdapter = new ChatAdapter(this, messages);
            chatListView.Adapter = chatAdapter;

            sendButton.Click += SendButton_Click;
            sendPhotoButton.Click += SendPhoto_Click;


            startVideoCall = FindViewById<ImageView>(Resource.Id.videoCall);
            startAudioCall = FindViewById<ImageView>(Resource.Id.voiceCall);

            //  שיחת וידיאו
            startVideoCall.Click += async (sender, e) =>
            {
                // Toast.MakeText(this, answerOrCall , ToastLength.Short).Show();
                await openRemoteWebPage(localUserRegestrationName, remoteUserName, "Call", "Video");
                await openRemoteWebPage(remoteUserName, localUserRegestrationName, "Answer", "Video");
            };

            // שיחת אודיו בלבד   
            startAudioCall.Click += async (sender, e) =>
            {
                // Toast.MakeText(this, answerOrCall , ToastLength.Short).Show();
                await openRemoteWebPage(localUserRegestrationName, remoteUserName, "Call", "Audio");
                await openRemoteWebPage(remoteUserName, localUserRegestrationName, "Answer", "Audio");
            };

            // נממש את הטיפול ב רשימת הצט כפונקציה נפרדת
            chatListHandler();

        } // OnCreate



        protected override void OnResume()
        {
            base.OnResume();

            User_DetailedactivtyFocoused = true;
            UpdateRemoteUserList(localUserRegestrationName, remoteUserName);

        }



        protected override void OnPause()
        {
            base.OnPause();
            User_DetailedactivtyFocoused = false;


        }
        //   הצג פריט אחרון ברשימה
        public void JumpToLastItem()
        {

            chatListView.Post(() =>
             {

                 //  chatListView.SetSelection(chatAdapter.Count - 1);
                 chatListView.SmoothScrollToPosition(chatAdapter.Count - 1);


             });

        }


        ///===========        פריט נבחר ברשימת הצט     ===================

        private void chatListHandler()
        {
            chatListView.ItemClick += (sender, e) =>
            {
                int position = e.Position; // Get the clicked position
                var listData = messages[position];
                if (listData.MessageTypeOptions == "Photo")
                {
                    string imagePath = listData.FilePath;
                    ChatAdapter.ShowFullScalePhoto(this, imagePath);

                }


            };
        }



        //=========================    כפתור שלח תמונה נלחץ =======================


        private void SendPhoto_Click(object sender, System.EventArgs e)
        {
            // Trigger the file picker intent
            var intent = new Intent(Intent.ActionGetContent);
            intent.SetType("image/*");
            intent.AddCategory(Intent.CategoryOpenable);
            StartActivityForResult(intent, PickFileRequestCode);
        }


        protected async override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // Check if the request code matches and if the result is OK
            if (requestCode == PickFileRequestCode && resultCode == Result.Ok && data != null)
            {
                var fileUri = data.Data; 
               

                if (fileUri != null)
                {
                    // Get the file name from the URI
                    var fileName = FileUtiles.GetFileName(this, fileUri) ?? "unknown_file";

                    bool isUploaded = await FileUtiles.UploadFileUsingStream(this, fileUri, fileName, localUserRegestrationName, remoteUserName);

                    //   מעתיקים את הקןבץ לספרית  /download/myChat/Outgoing


                    var targetDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath + "/MyChat/Outgoing";
                    if (!System.IO.Directory.Exists(targetDirectory))
                    {
                        System.IO.Directory.CreateDirectory(targetDirectory);
                    }

                    var now = DateTime.Now;

                    string newFileName = now.ToString("yyMMddHHmmssfff") + fileName;
                    
                    var targetFilePath = System.IO.Path.Combine(targetDirectory, newFileName);

                    if (System.IO.File.Exists(targetFilePath))
                    {
                        // Delete the existing file
                        System.IO.File.Delete(targetFilePath);
                    }
                   
                    using (var inputStream = ContentResolver.OpenInputStream(fileUri))
                    using (var outputStream = System.IO.File.Create(targetFilePath))
                    {
                        await inputStream.CopyToAsync(outputStream);
                    }

                    
                     string trueFilePath = targetFilePath;
                    // שלח תמונה 


                   

                    if (isUploaded)
                    {
                        // תמונה נשלחה בהצלחה
                        var db = userDetailedDbHelper.WritableDatabase;

                        string photoOrintation = FileUtiles.GetImageOrientation(trueFilePath);


                        ContentValues values = new ContentValues();
                        values.Put("sendingUserName", localUserRegestrationName);
                        values.Put("MessageType", "sent");
                        values.Put("recivingUserName", remoteUserName);
                        values.Put("MessageText", "");
                        values.Put("MessageTypeOptions", "Photo");
                        values.Put("FilePath", trueFilePath);
                        values.Put("PortraitOrlandscape", photoOrintation);
                        db.Insert("Messages", null, values);
                        db.Close();
                        Toast.MakeText(this, "File Uploded successfully !", ToastLength.Short).Show();

                        if (!string.IsNullOrEmpty(trueFilePath))
                        {
                            // Create a new chat message and add it to the list
                            ChatMessage newMessage = new ChatMessage
                            {
                                Text = "",
                                Time = System.DateTime.Now.ToString("hh:mm tt"),
                                IsIncoming = true, // Assume the message is outgoing
                                FilePath =  trueFilePath,
                                MessageTypeOptions = "Photo" ,
                                PortraitOrlandscape = photoOrintation
                            };
                            messages.Add(newMessage);
                            chatAdapter.NotifyDataSetChanged();
                            JumpToLastItem();
                        }

                    }
                    else Toast.MakeText(this, "Uploade Fail !", ToastLength.Short).Show();
                }
            }
        }


        //==============================   כפתור שלח הודעה נלחץ  ====================
        private async void SendButton_Click(object sender, System.EventArgs e)
        {

            string messageText = inputBox.Text.Trim();

            var user = TransferUserListManager.ConnectedUsers.FirstOrDefault(u => u.UserName == remoteUserName);
            if (user != null)
            {

                if (SignalRHub.Connection != null && SignalRHub.Connection.State == HubConnectionState.Connected)
                {
                    await SendMessage(localUserRegestrationName, user.UserName, messageText);
                }

            }


            var db = userDetailedDbHelper.WritableDatabase;

            ContentValues values = new ContentValues();
            values.Put("sendingUserName", localUserRegestrationName);
            values.Put("MessageType", "sent");
            values.Put("recivingUserName", user.UserName);
            values.Put("MessageText", messageText);
            values.Put("MessageTypeOptions", "Message");
            values.Put("FilePath", "");
            values.Put("PortraitOrlandscape", "");
            db.Insert("Messages", null, values);
            db.Close();



            if (!string.IsNullOrEmpty(messageText))
            {
                // Create a new chat message and add it to the list
                ChatMessage newMessage = new ChatMessage
                {
                    Text = messageText,
                    Time = System.DateTime.Now.ToString("hh:mm tt"),
                    IsIncoming = true, // Assume the message is outgoing
                    FilePath = "",
                    MessageTypeOptions = "Message"
                };
                messages.Add(newMessage);
                chatAdapter.NotifyDataSetChanged(); // Refresh the ListView
                inputBox.Text = string.Empty; // Clear the input box
                //   chatListView.SmoothScrollToPosition(messages.Count - 1); // Scroll to the bottom
                //  chatListView.SetSelection(messages.Count - 1);
                JumpToLastItem();
            }
        }


        //=================================================================
        //============     שלח הודעה למשתמש 

        private async Task SendMessage(string sendingUserName, string receivingUserName, string message)
        {
            if (SignalRHub.Connection.State != HubConnectionState.Connected)    // האם אנחנו מחוברים לשרת?
            {
                Toast.MakeText(this, "Server Is Not Connected !!", ToastLength.Long).Show();
                return;
            }

            // שלח את ההודעה אל המשתמש שאנחנו רוצים.
            // במידה וקיבלנו שגיאה, הצג אותה במסך.
            string result = await SignalRHub.Connection.InvokeAsync<string>(
                "SendMessageToUser",
                sendingUserName,
                receivingUserName,
                message
            );

            if (result == "BadSendingUserName")
            {
                Toast.MakeText(Android.App.Application.Context, $"Error: The Sending UserName Was Wrong", ToastLength.Long).Show();
            }
            else if (result == "SendingUserNameDidntLogin")
            {
                Toast.MakeText(Android.App.Application.Context, $"Error: You Must Login Before Sending Messages", ToastLength.Long).Show();
            }
            else if (result == "ReceivingUserNameDidntLogin")
            {
                Toast.MakeText(Android.App.Application.Context, $"Error: User {receivingUserName} Is Not Online", ToastLength.Long).Show();
            }
            else if (result == "MessageCantBeEmpty")
            {
                Toast.MakeText(Android.App.Application.Context, $"Error: Can't Send An Empty Message", ToastLength.Long).Show();
            }
        }


        //=================================================================

        // ===============      התקבלה הודעה  =============================

        public void UpdateRecivedMessage(string sendingUserName, string recivedUserName, string message)
        {
            //   עדכן רשימה רק עבןר המשתמש הספציפי
            if (remoteUserName == sendingUserName)
            {

                var db = userDetailedDbHelper.WritableDatabase;

                ContentValues values = new ContentValues();
                values.Put("sendingUserName", sendingUserName);
                values.Put("MessageType", "recive");
                values.Put("recivingUserName", recivedUserName);
                values.Put("MessageText", message);
                values.Put("MessageTypeOptions", "Message");
                values.Put("FilePath", "");
                values.Put("PortraitOrlandscape", "");
                db.Insert("Messages", null, values);
                db.Close();



                RunOnUiThread(() =>
                     {

                         ChatMessage newMessage = new ChatMessage
                         {
                             Text = message,
                             Time = System.DateTime.Now.ToString("hh:mm tt"),
                             IsIncoming = false, // This message is incoming
                             FilePath = "",
                             MessageTypeOptions = "Message"

                         };
                         messages.Add(newMessage);

                         // chatListView.SmoothScrollToPosition(messages.Count - 1); // Scroll to the bottom
                         // chatListView.SetSelection(messages.Count - 1);
                         //  JumpToLastItem();
                         chatAdapter.NotifyDataSetChanged(); // Refresh the ListView

                     });
            }
        }
        //================================================================
        //===============    התקבלה תמונה =================================

        public async void DownloadFile(string recivingUserName, string sendingUserName, string fileUrl, string portraitOrlandscape, string filetype)
        {
            if (user_detailed.User_DetailedactivtyFocoused)
            {
                if (remoteUserName == sendingUserName)
                {
                    try
                    {
                        string filepath = await FileUtiles.SaveFileToDownloadsAsync(recivingUserName, sendingUserName, fileUrl);
                        if (filepath != null)
                        {
                            var db = userDetailedDbHelper.WritableDatabase;
                            ContentValues values = new ContentValues();
                            values.Put("sendingUserName", sendingUserName);
                            values.Put("MessageType", "recive");
                            values.Put("recivingUserName", recivingUserName);
                            values.Put("MessageText", "");
                            values.Put("MessageTypeOptions", "Photo");
                            values.Put("FilePath", filepath);
                            values.Put("PortraitOrlandscape", portraitOrlandscape);
                            db.Insert("Messages", null, values);
                            db.Close();

                        }

                        //    עדכן רשימה והצג
                        string timeFormatted = DateTime.Now.ToString("HH:MM:SS");

                        ChatMessage newMessage = new ChatMessage
                        {
                            Text = "",
                            Time = timeFormatted,
                            IsIncoming = false,  //   תמונה מתקבלת
                            MessageTypeOptions = "Photo",
                            FilePath = filepath,
                            PortraitOrlandscape = portraitOrlandscape
                        };
                        messages.Add(newMessage);
                        chatAdapter.NotifyDataSetChanged();
                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");

                    }

                }

            }
        }

        //=================================================================

        //================  עדכון הודעות המשתמש המרוחק   מבסיס הנתונים ====

        public void UpdateRemoteUserList(string localUser, string remoteUser)
        {
            chatAdapter.ClearMessages();
            chatAdapter.NotifyDataSetChanged();
            var userMessages = userDetailedDbHelper.GetMessagesForUsers(localUser, remoteUser);
            foreach (var message in userMessages)
            {
                DateTime timestamp = DateTime.Parse(message["Timestamp"]);
                string timeFormatted = timestamp.ToString("HH:mm:ss");


                string test5 = message["Timestamp"];
                bool sentOrRecive = false;
                if (message["MessageType"] == "sent")
                {
                    sentOrRecive = true;
                }
                else { sentOrRecive = false; }

                ChatMessage newMessage = new ChatMessage
                {
                    Text = message["MessageText"],
                    Time = timeFormatted,
                    IsIncoming = sentOrRecive, // This message is incoming
                    MessageTypeOptions = message["MessageTypeOptions"],
                    FilePath = message["FilePath"],
                    PortraitOrlandscape = message["PortraitOrlandscape"],
                };
                messages.Add(newMessage);
                //   chatAdapter.NotifyDataSetChanged(); // Refresh the ListView
                // chatListView.SmoothScrollToPosition(messages.Count - 1); // Scroll to the bottom
                // chatListView.SetSelection(messages.Count - 1);

                //   JumpToLastItem();
                chatAdapter.NotifyDataSetChanged();
            }

            chatAdapter.NotifyDataSetChanged();
            // JumpToLastItem();

        }




        public void OpenWebPageWithCustomTab(string url)
        {
            var customTabsIntent = new CustomTabsIntent.Builder()
                .SetShowTitle(true)
                .Build();
            customTabsIntent.LaunchUrl(this, Android.Net.Uri.Parse(url));

        }
        //  call at Server side 
        public async Task openRemoteWebPage(string localUserName, string remoteUserName, string play, string audioOrVideo)
        {
            await SignalRHub.Connection.InvokeAsync("openRemoteWebPage", localUserName, remoteUserName, play, audioOrVideo);

        }


        public void OpenWebRtcPage(string remoteUser, string play, string audioOrVideo)
        {


            RunOnUiThread(() =>
            {
                string userPrams1 = "webrtc-" + localUserRegestrationName;
                string userPrams2 = "webrtc-" + remoteUser;


                if (localUserRegestrationName != remoteUser)
                {
                    if (play == "Answer")
                    {

                        Android.Content.Intent intent = new Android.Content.Intent(this, typeof(VideoCallDialogActivity));
                        intent.PutExtra("Message", remoteUser);
                        intent.PutExtra("VideoOrAudio", audioOrVideo);
                        VideoCallDialogActivity.ResultCallback = HandleDialogResult;
                        StartActivity(intent);




                    }

                    if (play == "Call")
                    {
                        string url = $"https://" + SignalRHub.remoteServerIp + $"/test.html#param1={userPrams1}&param2={userPrams2}&param3=Call&param4={audioOrVideo}";
                        //   string url = $"https://farkash-amit.tplinkdns.com:5000/webrtcchat.html#param1={userPrams1}&param2={userPrams2}&param3=Call";

                        OpenWebPageWithCustomTab(url);

                    }

                }
                else { Android.Widget.Toast.MakeText(this, $"ילד רע אי אפשר להתקשר לעצמך", Android.Widget.ToastLength.Short).Show(); }
            });

        }





        public void HandleDialogResult(bool result, string remoteUser, string audioOrVideo)
        {
            if (result)
            {
                SignalRHub.CallIsActive = true;
                string userPrams1 = "webrtc-" + localUserRegestrationName;
                string userPrams2 = "webrtc-" + remoteUser;

                var user = TransferUserListManager.ConnectedUsers.FirstOrDefault(u => u.UserName == userPrams2);
                if (user != null)
                {

                    // string url = $"https://192.168.0.111:5000/webrtcchat.html#param1={userPrams1}&param2={userPrams2}";
                    //  string url = $"https://farkash-amit.tplinkdns.com:5000/webrtcchat.html#param1={userPrams1}&param2={userPrams2}&param3=Answer";
                    string url = $"https://" + SignalRHub.remoteServerIp + $"/test.html#param1={userPrams1}&param2={userPrams2}&param3=Answer&param4={audioOrVideo}";
                    OpenWebPageWithCustomTab(url);
                }
                else
                {

                    Toast.MakeText(Android.App.Application.Context, $"שיחה נותקה", ToastLength.Long).Show();

                }


            }
            else
            {
                //  מצא את החיבור של משתמש הוידאו
                string webRtcName = "webrtc-" + remoteUser;
                var user = TransferUserListManager.ConnectedUsers.FirstOrDefault(u => u.UserName == webRtcName);

                if (user != null)
                {
                    SignalRHub.CallIsActive = false;
                    string webRtcUserId = user.ConnectionId;
                    SignalRHub.Connection.InvokeAsync("endCall", webRtcUserId);
                }
            }

        }






        private class SignalRBroadcastReceiver : BroadcastReceiver
        {
            //  הנוכחי כדי לאפשר גישה   activty חייבים להעביר את ה   
            //  MainActivty  למחלקה הראשית 

            private readonly user_detailed contextAactivity;

            public SignalRBroadcastReceiver(user_detailed activity)
            {
                contextAactivity = activity;
            }
            public override void OnReceive(Context context, Android.Content.Intent intent)
            {
                string action = intent.Action;

                switch (action)
                {


                    case "ReceiveMessage":

                        string message = intent.GetStringExtra("Message");
                        string sendingUserName = intent.GetStringExtra("SendingUser");
                        string recivingUserName = intent.GetStringExtra("RecivingUser");
                        if (User_DetailedactivtyFocoused == true)
                        {
                            contextAactivity.UpdateRecivedMessage(sendingUserName, recivingUserName, message);

                        }



                        break;


                    case "OpenWebrtcPage":
                        if (User_DetailedactivtyFocoused == true)
                        {

                            string webRtcUser = intent.GetStringExtra("RemoteUser");
                            string play = intent.GetStringExtra("Play");
                            string audioOrVideo = intent.GetStringExtra("AudioOrVideo");
                            contextAactivity.OpenWebRtcPage(webRtcUser, play, audioOrVideo);
                        }

                        break;

                    case "FileWatingToDownload":

                        recivingUserName = intent.GetStringExtra("RecivingUserName");
                        sendingUserName = intent.GetStringExtra("SendingUserName");
                        string fileUrl = intent.GetStringExtra("FileUrl");
                        string portraitOrlandscape = intent.GetStringExtra("PortraitOrlandscape");
                        string filetype = intent.GetStringExtra("FileType");

                        contextAactivity.DownloadFile(recivingUserName, sendingUserName, fileUrl, portraitOrlandscape, filetype);

                        break;


                }
            }

        }






    }



}