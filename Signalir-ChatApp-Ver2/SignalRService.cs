using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Signalir_ChatApp
{

    public static class SignalRHub
    {
        public static HubConnection Connection { get; set; }
        public static bool CallIsActive = false;
        public static string remoteServerIp;
        public static bool startedRunning = false;

    }


    [Service]
    public class SignalRService : Service
    {
        public static HubConnection hubConnection;
        private static bool isServiceRunning = false;

        // add for test 
        public static Intent ServiceServiceIntent;
        private const string NotificationChannelId = "SignalR_Channel";
        //משתנה שלא ניתן לשינוי ערכו, ניסיון לשינוי ערכו תוביל לשגיאה בעת  ההרצה const
        private const int NotificationId = 1;
        private static CancellationTokenSource _keepAliveTokenSource;
        // .NET היא מחלקה ב CancellationTokenSource המחלקה 
        // cancellation היא מאפשרת ליצור ולהפיץ אסימוני ביטול 
        // המשמשת לביצוע וניהול ביטול של משימות אסינכרוניות
        // שניתן להשתמש בהם על מנת לעצור פעולות אשר רצות
        public static bool ServerIsAlive = false;
        public override void OnCreate()
        {
            base.OnCreate();

            InitializeService();
        }

        //============================================
        private async void InitializeService()
        {
            // Initialize SignalR connection, create notification channel, etc.
            InitializeSignalR();
            //while (!InitializeSignalR())
            //{
            // לא הצלחנו לבצע אתחול. נישן קצת וננסה שוב.
            //Console.WriteLine("WAITINGGGGGGGGGGGGGGGGGG");
            //    Thread.Sleep(5000);
            //}
            CreateNotificationChannel();

            // Start the SignalR connection if it isn't running already
            if (!isServiceRunning)
            {
                Console.WriteLine("@@@@@@@@@@@@@@ 1");
                // Start the connection to SignalR hub
                Task.Run(async () => await ConnectToSignalRHub());
                Thread.Sleep(1000);
                StartKeepAliveAsync();

                // Create and show the foreground notification
                var notification = CreateForegroundNotification("SignalR Service", "Running in the background");
                StartForeground(NotificationId, notification);
                isServiceRunning = true;
                SignalRHub.startedRunning = true;
                Console.WriteLine("@@@@@@@@@@@@@@ 2");
            }
        }

        //=============================================

        private static bool InitializeSignalR()
        {
            // תבדוק האם הגדרנו לאיזה שרת להתחבר
            string appName = Application.Context.Resources.GetString(Resource.String.app_name);
            var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
            string savedIpAddress = prefs.GetString("IpAddress", "NoServerIp");
            if (string.IsNullOrEmpty(savedIpAddress) || savedIpAddress == "NoServerIp")
            {
                Console.WriteLine("Can't connect to server - IP address not configured.");
                return false;
            }
            Console.WriteLine("@@@@@@@@@@@@@@ 3");
            Console.WriteLine(savedIpAddress);

            // מעולה. עכשיו אפשר להתחבר לשרת!

            var handler = new HttpClientHandler
            {
                // Bypasses SSL validation - always returns true regardless of certificate validity
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            // תגדיר מה הכתובת של השרת, כך שנצליח להתחבר אליו
            string connectionURL = "https://" + savedIpAddress + "/echohub";

            // Create the SignalR connection with HTTPS and SSL validation bypass
            hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionURL, options =>
                {
                    options.HttpMessageHandlerFactory = _ => handler;

                })
               .WithAutomaticReconnect()
               .Build();

            // Assign the connection to the shared SignalRHub class
            SignalRHub.Connection = hubConnection;
            Console.WriteLine("@@@@@@@@@@@@@@ 4");
            return true;
        }


        //===========================================================
        private async Task ConnectToSignalRHub()
        {
            try
            {
                await hubConnection.StartAsync();

                var connectionId = hubConnection.ConnectionId;
                BroadcastConnectionStatus("StatusUpdate", "Connected to server ->", connectionId);


                hubConnection.On<string, string, string>("ReceiveMessage", (sendingUser, recivingUser, message) =>
                {
                    BroadcastMessage("ReceiveMessage", sendingUser, recivingUser, message);
                });

                hubConnection.On<List<ConnectedUser>>("UpdateUserList", (usersList) =>
                {
                    TransferUserListManager.UpdateUserList(usersList);
                    BrodcastUpdateUserList();
                });

                //    בקשה לשיחת וידאו התקבלה
                hubConnection.On<string, string, string>("OpenWebrtcPage", (remoteUser, play, audioOrVideo) =>
                {
                    if (play == "Call") BrodcastOpenWebrtcPage(remoteUser, play, audioOrVideo);
                    if (play == "Answer")
                    {
                        string incomingMessage = "Video call from :" + remoteUser;
                        ShowNotification(incomingMessage, "");

                        if (user_detailed.User_DetailedactivtyFocoused) BrodcastOpenWebrtcPage(remoteUser, play, audioOrVideo);
                        if (MainActivity.MainactivtyFocoused) BrodcastOpenWebrtcPage(remoteUser, play, audioOrVideo);
                        else
                        {


                            Task.Run(async () =>
                            {
                                //   התחל טיימר ל 5 שנייות
                                var timeout = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5));
                                while (!MainActivity.MainactivtyFocoused)
                                {

                                    await Task.Delay(100); // Adjust the delay as needed
                                                           // אם עבר הזמן סיים
                                    if (DateTime.UtcNow > timeout) return;
                                }


                                BrodcastOpenWebrtcPage(remoteUser, play, audioOrVideo);
                            });

                        }
                    }
                });

                //    השרת מחובר
                hubConnection.On<string>("ServerIsAlive", (user) =>
                {
                    ServerIsAlive = true;
                });

                hubConnection.On<string, string>("AddNewUserToDataBaseResults", (user, message) =>
                {
                    BrodcastAddNewUserToDataBaseResults(user, message);
                });

                //   קובץ מוכן להורדה 
                hubConnection.On<string, string, string, string, string>("FileWaitingForDownload", (recivingUserName, sendingUserName, fileUrl, portraitOrlandscape, fileType) =>
                {
                    BrodcastFileWatingToDownload(recivingUserName, sendingUserName, fileUrl, portraitOrlandscape, fileType);
                });

                hubConnection.Closed += async (error) =>
                {
                    await ReconnectToSignalRHub();
                };
            }
            catch (Exception ex)
            {
                //  BroadcastMessage("Error","", $"Error: {ex.Message}");
                if (ex.Message.Contains("Connection timed out"))
                    await ReconnectToSignalRHub();
            }
        }
        private void BroadcastConnectionStatus(string action, string message, string connectionId)
        {
            var intent = new Intent(action);
            intent.PutExtra("Message", message);
            intent.PutExtra("ConnectionId", connectionId);
            SendBroadcast(intent);
        }

        private void BroadcastMessage(string action, string sendingUser, string recivingUser, string message)
        {
            var intent = new Intent(action);
            intent.PutExtra("Message", message);
            intent.PutExtra("SendingUser", sendingUser);
            intent.PutExtra("RecivingUser", recivingUser);

            SendBroadcast(intent);
        }

        private void BrodcastUpdateUserList()
        {
            // TransferUserListManager העדכון  נעשה דרך רשימה  

            var intent = new Intent("UpdateUserList");
            SendBroadcast(intent);
        }


        private void BrodcastOpenWebrtcPage(string remoteUser, string play, string audioOrVideo)
        {
            SignalRHub.CallIsActive = false;
            var intent = new Intent("OpenWebrtcPage");
            intent.PutExtra("RemoteUser", remoteUser);
            intent.PutExtra("Play", play);
            intent.PutExtra("AudioOrVideo", audioOrVideo);
            SendBroadcast(intent);
        }

        private void BrodcastAddNewUserToDataBaseResults(string user, string message)
        {

            var intent = new Intent("BrodcastAddNewUserToDataBaseResults");
            intent.PutExtra("User", user);
            intent.PutExtra("Message", message);
            SendBroadcast(intent);


        }

        //  קובץ ממתין להורדה
        private void BrodcastFileWatingToDownload(string recivingUserName, string sendingUserName, string fileUrl, string portraitOrlandscape, string fileType)
        {
            var intent = new Intent("FileWatingToDownload");
            intent.PutExtra("RecivingUserName", recivingUserName);
            intent.PutExtra("SendingUserName", sendingUserName);
            intent.PutExtra("FileUrl", fileUrl);
            intent.PutExtra("PortraitOrlandscape", portraitOrlandscape);
            intent.PutExtra("FileType", fileType);
            SendBroadcast(intent);
        }


        public void BrodcastWakeUp()
        {
            var intent = new Intent("ResetConection");
            SendBroadcast(intent);

        }


        private async Task ReconnectToSignalRHub()
        {
            await Task.Delay(5000); // Wait before retrying
            await ConnectToSignalRHub();
        }






        //===========================================================

        public override IBinder OnBind(Intent intent) => null;

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            // Run the asynchronous work separately so that OnStartCommand can return the expected type
            _ = StartSignalRConnectionAsync();

            // Ensure the service stays alive unless explicitly stopped
            return StartCommandResult.Sticky;
        }

        // Separate asynchronous method to handle SignalR connection
        private async Task StartSignalRConnectionAsync()
        {
            try
            {
                // Start the SignalR connection and await completion
                await SignalRHub.Connection.StartAsync();
                Console.WriteLine("SignalR connection started successfully.");
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the connection attempt
                Console.WriteLine($"Error starting SignalR connection: {ex.Message}");
            }
        }

        public override async void OnDestroy()
        {
            base.OnDestroy();
            try
            {

                // Stop the SignalR connection when the service is destroyed
                await SignalRHub.Connection.StopAsync();

                Console.WriteLine("SignalR connection stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping SignalR connection: {ex.Message}");
            }

            // ShowStopNotification();
            isServiceRunning = false;
        }

        //=======================================================

        private void ShowNotification(string title, string message)
        {
            if (MainActivity.MainactivtyFocoused || user_detailed.User_DetailedactivtyFocoused)
            {

                return; // אל תציג הודעה
            }



            var notificationIntent = new Intent(this, typeof(MainActivity));
            notificationIntent.PutExtra("NotificationMessage", message);
            notificationIntent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);

            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                notificationIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = new NotificationCompat.Builder(this, NotificationChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetSmallIcon(Resource.Drawable.cat) // Replace with your app's notification icon
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .Build();

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(NotificationId, notification);
        }

        private Notification CreateForegroundNotification(string title, string message)
        {
            var pendingIntent = PendingIntent.GetActivity(
                this,
                0,
                new Intent(this, typeof(MainActivity)),
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            return new NotificationCompat.Builder(this, NotificationChannelId)
                .SetContentTitle(title)
                .SetContentText(message)
                //.SetSmallIcon(Resource.Drawable.cat)
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .Build();
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    NotificationChannelId,
                    "SignalR Notifications",
                    NotificationImportance.High)
                {
                    Description = "Channel for SignalR messages"
                };

                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        private void ShowStopNotification()
        {
            var notification = new NotificationCompat.Builder(this, NotificationChannelId)
                .SetContentTitle("SignalR Service Stopped")
                .SetContentText("The background service has been stopped.")
                .SetSmallIcon(Resource.Drawable.cat) // Replace with your app's icon
                .SetAutoCancel(true)
                .Build();

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(NotificationId + 1, notification); // Use a unique ID for this notification
        }



        //=========    Keep Alive ====================
        public async Task StartKeepAliveAsync()
        {
            // Cancel any existing keep-alive task
            _keepAliveTokenSource?.Cancel();
            _keepAliveTokenSource = new CancellationTokenSource();

            var token = _keepAliveTokenSource.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        if (ServerIsAlive == true) BroadcastConnectionStatus("StatusUpdate", "Connected to server ->", "ImAlive");
                        ServerIsAlive = false;
                        if (SignalRHub.Connection != null && SignalRHub.Connection.State == HubConnectionState.Connected)
                        {

                            await SignalRHub.Connection.InvokeAsync("KeepAlive", MainActivity.localUserRegestrationName);

                        }
                        try
                        {
                            if (SignalRHub.Connection != null && SignalRHub.Connection.State == HubConnectionState.Disconnected)
                            {
                                BrodcastWakeUp();

                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during keep-alive: {ex.Message}");
                    }

                    // Wait for 30 seconds before the next keep-alive
                    await Task.Delay(TimeSpan.FromSeconds(20), token);
                }
            }, token);
        }






    }// the end 
}

