using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Browser.CustomTabs;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageViews.Photo;
using AndroidX.Lifecycle;
using System.Net.Http;





namespace Signalir_ChatApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, WindowSoftInputMode = SoftInput.AdjustResize)]
    public class MainActivity : AppCompatActivity

    {
        public DatabaseHelper usersListdDbHelper { get; set; }
        const int RequestStoragePermissionId = 1;
        private TextView conectionText;
        private ListView listView;
        private ConectionAdapter adapter;
        // private TextView remoteUserID;
        public static string localUserRegestrationName;
        private List<ImageView> lst;

        public string selectedRemoteUserName;
        public string RemoteWebRtcUserId;
        public static bool MainactivtyFocoused = false;
        public static bool LocalUserExistOnServer = false;
        List<ConnectedUsersList> conectedusers = new List<ConnectedUsersList> { };
        private static BroadcastReceiver broadcastReceiver;
        public static bool serviceStarted = false;

        public static Intent signalRServiceIntent;

        //   TaskCompletionSource<string>   אשר מסתיים רק כאשר קורה אירוע TASK יוצרים 
        //                     במקרה שלנו כאשר הגיע תשובה מהשרת העם המשתמש קיים או לא 
        private TaskCompletionSource<string> responseCompletionSource;

        ImageView iv;
        Button btn;
        Button galleryButton;



        bool mExternalStorageAvailable = false;
        bool mExternalStorageWriteable = false;
        private const int REQUEST_IMAGE_CAPTURE = 0;
        private const int REQUEST_IMAGE_GALLERY = 1;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Handle intent extras if the activity was launched via notification
            HandleIntent(Intent);

            NewRequestPermissions();

            conectionText = FindViewById<TextView>(Resource.Id.connectionText);


            // Initialize ListView

            listView = FindViewById<ListView>(Resource.Id.userListView);
            listView.SetBackgroundColor(Android.Graphics.Color.LightCyan);
            btn = (Button)FindViewById(Resource.Id.takePhotoButton);
            iv = (ImageView)FindViewById(Resource.Id.profileImage);
            galleryButton = (Button)FindViewById(Resource.Id.galleryButton1);
            
           
            btn.Click += Btn_Click;




            adapter = new ConectionAdapter(this, conectedusers);
            listView.Adapter = adapter;
            // מחברים את ה-Adapter של ה-ListView לרשימת המשתמשים המחוברים

            if (serviceStarted == false)
            {
                // בודקים אם השירות כבר התחיל, אם לא - מפעילים אותו

                signalRServiceIntent = new Intent(this, typeof(SignalRService));
                // יוצרים Intent שמטרתו להפעיל את שירות ה-SignalR

                StartService(signalRServiceIntent);
                // מפעילים את שירות ה-SignalR

                broadcastReceiver = new SignalRBroadcastReceiver(this);
                // יוצרים אובייקט של SignalRBroadcastReceiver להאזנה לברודקאסטים

                // נרשמים למגוון IntentFilters לקבלת הודעות שקשורות לסטטוס ה-SignalR
                RegisterReceiver(broadcastReceiver, new IntentFilter("StatusUpdate"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("ResetConection"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("ReceiveMessage"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("UpdateUserList"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("getRemoteUserIdFromServer"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("OpenWebrtcPage"));
                RegisterReceiver(broadcastReceiver, new IntentFilter("FileWatingToDownload"));

                serviceStarted = true;
                // מציינים שהשירות הופעל פעם אחת בלבד
            }

            usersListdDbHelper = new DatabaseHelper(Android.App.Application.Context);
            // יוצרים אובייקט עוזר לחיבור ל-SQLite לצורך ניהול משתמשים

            galleryButton.Click += (sender, e) =>
            {
                // כאשר לוחצים על כפתור הגלריה, פותחים את הגלריה כדי לבחור תמונה
                Intent pickPhoto = new Intent(Intent.ActionPick, Android.Provider.MediaStore.Images.Media.ExternalContentUri);
                StartActivityForResult(pickPhoto, REQUEST_IMAGE_GALLERY);
            };

            // כאשר בוחרים משתמש מהרשימה
            listView.ItemClick += async (s, e) =>
            {
                var conection = conectedusers[e.Position];
                // מקבלים את המשתמש שנבחר לפי המיקום שלו ברשימה

                selectedRemoteUserName = conection.Name;
                // שומרים את שם המשתמש שנבחר

                var intent = new Intent(this, typeof(user_detailed));
                // יוצרים Intent למסך פרטי המשתמש

                intent.PutExtra("localUserName", localUserRegestrationName);
                // מעבירים את שם המשתמש המקומי ל-Intent

                intent.PutExtra("remoteUserName", conection.Name);
                // מעבירים את שם המשתמש המרוחק ל-Intent

                StartActivity(intent);
                // פותחים את מסך פרטי המשתמש
            };

            iv.Click += (sender, e) =>
            {
                // כאשר לוחצים על התמונה של הפרופיל, היא תוצג בגודל מלא

                ImageView profileImageView = FindViewById<ImageView>(Resource.Id.profileImage);
                // מחפשים את ImageView שמציג את התמונה בפרופיל

                var prefs = GetSharedPreferences("MyAppPreferences", FileCreationMode.Private);
                // טוענים את התמונה השמורה מ-SharedPreferences

                string encodedImage = prefs.GetString("profile_image", null);
                // מקבלים את התמונה המוצפנת ב-Base64

                if (!string.IsNullOrEmpty(encodedImage))
                {
                    // אם יש תמונה שמורה, ממירים אותה מ-Base64 ל-Bitmap
                    byte[] imageBytes = Convert.FromBase64String(encodedImage);
                    Android.Graphics.Bitmap decodedBitmap = Android.Graphics.BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

                    // שומרים את התמונה כקובץ זמני בתיקייה
                    string tempImagePath = SaveImageToSpecificFolder(decodedBitmap);

                    // מציגים את התמונה בגודל מלא
                    ShowFullScalePhoto(this, tempImagePath);
                }
            };

            LoadImageFromPreferences();
            // טוענים את התמונה מפרטי התוכנה

            ReadUserNamefromDataBase();
            // טוענים את שם המשתמש מהבסיס נתונים
    }

            private string SaveImageToSpecificFolder(Android.Graphics.Bitmap bitmap)
            {
                // פונקציה לשמירת תמונה בתיקייה מוגדרת

                string myChatFolderPath = System.IO.Path.Combine(
                    Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,
                    "MyChat", "ProfilePictures");
                // מגדירים את הנתיב לתיקייה בתוך תיקיית ההורדות

                if (!Directory.Exists(myChatFolderPath))
                {
                    // אם התיקייה לא קיימת, יוצרים אותה
                    Directory.CreateDirectory(myChatFolderPath);
                }

                string bitmapHash = GetImageHash(bitmap);
                // יוצרים Hash ייחודי לתמונה

                string timestamp = DateTime.Now.ToString("HHmmss");
                // יוצרים Timestamp ייחודי לקובץ לפי שעה, דקות ושניות

                string fileName = $"profile_{timestamp}.jpg";
                // יוצרים שם ייחודי לקובץ

                string filePath = System.IO.Path.Combine(myChatFolderPath, fileName);
                // מגדירים את הנתיב המלא לקובץ

                if (!File.Exists(filePath))
                {
                    // אם הקובץ לא קיים, שומרים אותו כ-JPG
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 100, stream);
                    }
                }

                return filePath;
                // מחזירים את הנתיב של התמונה שנשמרה
            }

            private string GetImageHash(Android.Graphics.Bitmap bitmap)
            {
                // פונקציה ליצירת Hash לתמונה

                using (var stream = new MemoryStream())
                {
                    bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                    // דוחסים את התמונה לפורמט PNG

                    byte[] imageBytes = stream.ToArray();
                    // ממירים את התמונה למערך בתים

                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] hashBytes = sha256.ComputeHash(imageBytes);
                        // יוצרים Hash לתמונה בעזרת SHA-256

                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                        // ממירים את ה-Hash ל-String
                    }
                }
            }

            public static void ShowFullScalePhoto(Activity activity, string imagePath)
            {
                // פונקציה להצגת תמונה בגודל מלא במסך

                var fragmentTransaction = activity.FragmentManager.BeginTransaction();
                var fragment = new FullScreenImageFragment(imagePath);
                // יוצרים את פרגמנט התמונה בגודל מלא

                fragmentTransaction.Replace(Android.Resource.Id.Content, fragment);
                fragmentTransaction.AddToBackStack(null);
                fragmentTransaction.Commit();
                // מציגים את התמונה בגודל מלא בפרגמנט
            }

            public static async Task<string> SaveFileToDownloadsAsync(string recivingUserName, string sendingUserName, string fileUrl)
            {
                // פונקציה לשמירת קובץ מהאינטרנט לתיקיית ההורדות

                try
                {
                    string fileName = fileUrl.Substring(fileUrl.LastIndexOf('/') + 1);
                    // מקבלים את שם הקובץ מתוך כתובת ה-URL

                    string downloadsPath = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "MyChat", "Profile");
                    // מגדירים את הנתיב לתיקיית ההורדות בתוך תיקיית "MyChat"

                    if (!Directory.Exists(downloadsPath))
                    {
                        // אם התיקייה לא קיימת, יוצרים אותה
                        Directory.CreateDirectory(downloadsPath);
                    }

                    string filePath = System.IO.Path.Combine(downloadsPath, fileName);
                    // מגדירים את הנתיב המלא לקובץ

                    var handler = new HttpClientHandler
                    {
                        // מבטלים את בדיקת האישורים של השרת לצורך בדיקות
                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };

                    using (var httpClient = new HttpClient(handler))
                    {
                        var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);
                        // מורידים את הקובץ מהשרת

                        System.IO.File.WriteAllBytes(filePath, fileBytes);
                        // שומרים את הקובץ בתיקייה

                        Toast.MakeText(Android.App.Application.Context, $"File saved to: {filePath}", ToastLength.Long).Show();
                        // מציגים הודעה שהקובץ נשמר
                    }

                    return filePath;
                    // מחזירים את הנתיב של הקובץ שנשמר
                }
                catch (Exception ex)
                {
                    Toast.MakeText(Android.App.Application.Context, $"Error: {ex.Message}", ToastLength.Long).Show();
                    // מציגים הודעת שגיאה אם הייתה בעיה בשמירת הקובץ
                    return null;
                }
            }

            private void Btn_Click(object sender, System.EventArgs e)
            {
                // כאשר לוחצים על הכפתור, מבקשים הרשאות למצלמה ולזיכרון אם הן לא קיימות
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted ||
                    ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted ||
                    ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
                {
                    // אם ההרשאות אינן קיימות, מבקשים אותן מהמשתמש
                    ActivityCompat.RequestPermissions(this, new String[]
                    {
            Manifest.Permission.Camera,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.ReadExternalStorage
                }, 0);
            }
            else // if we already have the premissions
            {
                
                StartCamera();
            }
            
        }

        
        private void StartCamera() // allow start the camera
        {
            setPermissions();
            Intent intent = new Intent(Android.Provider.MediaStore.ActionImageCapture);
            StartActivityForResult(intent, 0);
        }
        public void setPermissions()
        {
            string state = Android.OS.Environment.ExternalStorageState;
            if (Android.OS.Environment.MediaMounted.Equals(state))
            {
                // We can read and write the media
                mExternalStorageAvailable = mExternalStorageWriteable = true;
                
            }
            else if (Android.OS.Environment.MediaMountedReadOnly.Equals(state))
            {
                // We can only read the media
                mExternalStorageAvailable = true;
                mExternalStorageWriteable = false;
                
            }
            else
            {
                // Something else is wrong. We can neither read nor write
                mExternalStorageAvailable = mExternalStorageWriteable = false;
                Toast.MakeText(this, "Something else is wrong. We can neither read nor write", ToastLength.Long).Show();
            }
        }


        // תוצאה של בקשת ההרשאות
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            // פונקציה שמופעלת לאחר בקשת הרשאות מהמשתמש

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == 0 && grantResults.Length > 0 && grantResults[0] == Permission.Granted)
            {
                // אם ההרשאה למצלמה התקבלה, מפעילים את הפונקציה שמתחילה את המצלמה
                StartCamera();
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // פונקציה שמופעלת כאשר מתקבלת תוצאה מפעולה, כמו פתיחת מצלמה או גלריה

            base.OnActivityResult(requestCode, resultCode, data);

            // טיפול בתמונה שנלכדה במצלמה
            if (requestCode == 0 && resultCode == Result.Ok && data != null)
            {
                // מקבלים את התמונה מהמצלמה
                Android.Graphics.Bitmap bitmap = (Android.Graphics.Bitmap)data.Extras.Get("data");

                // ממירים את התמונה לתמונה עגולה
                Android.Graphics.Bitmap circularBitmap = GetCircularBitmap(bitmap);

                // מציגים את התמונה העגולה ב-ImageView
                ImageView profileImageView = FindViewById<ImageView>(Resource.Id.profileImage);
                profileImageView.SetImageBitmap(circularBitmap);

                // שומרים את התמונה ב-SharedPreferences
                SaveImageToPreferences(circularBitmap);
            }
            // טיפול בבחירת תמונה מהגלריה
            else if (requestCode == REQUEST_IMAGE_GALLERY && resultCode == Result.Ok && data != null)
            {
                // מקבלים את ה-URI של התמונה שנבחרה מהגלריה
                Android.Net.Uri selectedImageUri = data.Data;

                // ממירים את ה-URI של התמונה ל-Bitmap
                ImageView profileImageView = FindViewById<ImageView>(Resource.Id.profileImage);
                Android.Graphics.Bitmap bitmap = MediaStore.Images.Media.GetBitmap(ContentResolver, selectedImageUri);

                // ממירים את התמונה לתמונה עגולה
                Android.Graphics.Bitmap circularBitmap = GetCircularBitmap(bitmap);

                // מציגים את התמונה העגולה ב-ImageView
                profileImageView.SetImageBitmap(circularBitmap);

                // שומרים את התמונה העגולה ב-SharedPreferences
                SaveImageToPreferences(circularBitmap);
            }
        }

        private Bitmap GetCircularBitmap(Android.Graphics.Bitmap bitmap)
        {
            // פונקציה שממירה תמונה ל-Bitmap עגול

            int width = bitmap.Width;
            int height = bitmap.Height;
            int minEdge = Math.Min(width, height);

            // יצירת Bitmap חדש שיכיל את התמונה העגולה
            Android.Graphics.Bitmap outputBitmap = Android.Graphics.Bitmap.CreateBitmap(minEdge, minEdge, Android.Graphics.Bitmap.Config.Argb8888);

            Android.Graphics.Canvas canvas = new Android.Graphics.Canvas(outputBitmap);

            // יצירת Paint עבור התמונה
            Android.Graphics.Paint paint = new Android.Graphics.Paint();
            paint.AntiAlias = true;

            // הגדרת אזור החיתוך לצורת עיגול
            Android.Graphics.Rect rect = new Android.Graphics.Rect(0, 0, minEdge, minEdge);
            Android.Graphics.RectF rectF = new Android.Graphics.RectF(rect);

            float radius = minEdge / 2f;

            // ציור עיגול
            canvas.DrawARGB(0, 0, 0, 0);
            paint.SetStyle(Android.Graphics.Paint.Style.Fill);
            canvas.DrawCircle(minEdge / 2f, minEdge / 2f, radius, paint);

            // הגדרת מצב החיתוך (שומר רק את מה שבתוך העיגול)
            paint.SetXfermode(new Android.Graphics.PorterDuffXfermode(Android.Graphics.PorterDuff.Mode.SrcIn));

            // ציור התמונה המקורית בתוך העיגול
            canvas.DrawBitmap(bitmap, null, rect, paint);

            return outputBitmap;
        }

        private void SaveImageToPreferences(Android.Graphics.Bitmap bitmap)
        {
            // פונקציה לשמירת התמונה ב-SharedPreferences

            string imagePath = System.IO.Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "MyChat", "ProfilePictures");

            // יצירת התיקייה אם היא לא קיימת
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
            // המרת התמונה לפורמט Base64
            using (var stream = new MemoryStream())
            {
                bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
                byte[] imageBytes = stream.ToArray();
                string encodedImage = Convert.ToBase64String(imageBytes);

                // שמירת התמונה ב-SharedPreferences
                var prefs = GetSharedPreferences("MyAppPreferences", FileCreationMode.Private);
                var editor = prefs.Edit();
                editor.PutString("profile_image", encodedImage);
                editor.Apply(); // שמירה ב-SharedPreferences
            }
        }

        private void LoadImageFromPreferences()
        {
            // פונקציה לטעינת תמונה מ-SharedPreferences והצגתה ב-ImageView

            var prefs = GetSharedPreferences("MyAppPreferences", FileCreationMode.Private);
            string encodedImage = prefs.GetString("profile_image", null);

            if (!string.IsNullOrEmpty(encodedImage))
            {
                // המרת התמונה חזרה מ-Base64 ל-Bitmap
                byte[] imageBytes = Convert.FromBase64String(encodedImage);
                Android.Graphics.Bitmap decodedBitmap = Android.Graphics.BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

                // המרת התמונה לצורת עיגול
                Android.Graphics.Bitmap circularBitmap = GetCircularBitmap(decodedBitmap);

                // הצגת התמונה ב-ImageView
                ImageView profileImageView = FindViewById<ImageView>(Resource.Id.profileImage);
                profileImageView.SetImageBitmap(circularBitmap);
            }
        }


        //======================================================================
        //    הוספת תפריט
        //======================================================================


        public override bool OnCreateOptionsMenu(IMenu menu)
        {

            MenuInflater.Inflate(Resource.Menu.mainMenu, menu);
            return true;
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_registration:

                    var newregIntent = new Intent(this, typeof(NewUserRegistration));

                    StartActivity(newregIntent);

                    return true;
                case Resource.Id.menu_serverip:

                    var newServerIntent = new Intent(this, typeof(ipAdress));

                    StartActivity(newServerIntent);


                    return true;
                case Resource.Id.menu_login:
                    Intent intent = new Intent(this, typeof(LoginActivity));
                    StartActivity(intent);
                    return true;
                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        //======================================================================================


        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            //  מקבל הודעות על הפעלה או הפעלה מחדש של הסרוויס
            HandleIntent(intent);
        }

        private void HandleIntent(Intent intent)
        {
            if (intent != null && intent.Extras != null)
            {
                // Example: Retrieve the message sent via the notification
                string message = intent.GetStringExtra("NotificationMessage");

                if (!string.IsNullOrEmpty(message))
                {

                    Toast.MakeText(this, $"Received: {message}", ToastLength.Short).Show();
                }
            }
        }


        public async Task ResetService(Intent signalRServiceIntent)
        {

            FindViewById<TextView>(Resource.Id.connectionText).SetBackgroundColor(Android.Graphics.Color.LightBlue);
            StopService(signalRServiceIntent);
            await Task.Delay(1000);
            StartService(signalRServiceIntent);


            Intent intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            StartActivity(intent);

            Finish();

        }




        protected async override void OnResume()
        {
            base.OnResume();
            //  Main Activty  got Foocous 
            MainactivtyFocoused = true;


            string appName = Resources.GetString(Resource.String.app_name);

            var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);

            //  טען את שם המשתמש ו כתובת איפי שך השרת מהפרפרנס
            string savedUserName = prefs.GetString("UserName", "NotRegistration");
            string savedServerIpAdress = prefs.GetString("IpAdress", "NoServerIp");



            if ((savedUserName != "NotRegistration") && (savedUserName != null))
            {
                localUserRegestrationName = prefs.GetString("UserName", "NotRegistration");
            }

            if ((savedServerIpAdress != "NoServerIp") && (savedServerIpAdress != null))
            {
                SignalRHub.remoteServerIp = savedServerIpAdress;
            }
            else SignalRHub.remoteServerIp = "farkash-amit.tplinkdns.com:5000";


            if (SignalRHub.Connection != null)
            {

                //    הסרוויס מחובר לשרת
                if (SignalRHub.Connection.State == HubConnectionState.Connected)

                {


                    string connectionId = SignalRHub.Connection.ConnectionId;

                    try
                    {

                        // שמחכה לסיום הארוע TASK הפעלת ה 
                        responseCompletionSource = new TaskCompletionSource<string>();

                        //   broadcast receiver  רישום 
                        var receiver = new SignalRBroadcastReceiver(this);
                        Application.Context.RegisterReceiver(receiver, new IntentFilter("getRemoteUserIdFromServer"));

                        LocalUserExistOnServer = true;
                        await SignalRHub.Connection.InvokeAsync("getRemoteUserId", localUserRegestrationName);

                        string answer = await responseCompletionSource.Task;

                        // מבטלים הרשמה לאירוע
                        Application.Context.UnregisterReceiver(receiver);



                        if (answer == "UserNotExist")
                        {
                            await SignalRHub.Connection.InvokeAsync("RegisterUser", localUserRegestrationName);

                        }
                        //  if (LocalUserExistOnServer == true)
                        if (answer == "UserExist")
                        {
                            await SignalRHub.Connection.InvokeAsync("UpdateUserList");

                            UpdateConnectedUserList();
                            UpdateConectionStatus(connectionId);
                        }

                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine(e);
                    }


                }

            }
        }



        protected override void OnPause()
        {
            // Main Activtty lost focous
            base.OnPause();
            MainactivtyFocoused = false;


        }




        // צריך לבקש הרשאות באפליקציה והחל מאנדראיד 13 יש שינוי
        private void NewRequestPermissions()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
            {
                ActivityCompat.RequestPermissions(this, new string[]
                {
                  Manifest.Permission.ReadMediaImages,
                  Manifest.Permission.ReadMediaVideo,
                  Manifest.Permission.ReadMediaAudio
                }, RequestStoragePermissionId);
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new string[]
                {
                  Manifest.Permission.ReadExternalStorage
                }, RequestStoragePermissionId);
            }
        }


        /*
                public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
                {
                    Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                }

        */

        public void OpenWebPageWithCustomTab(string url)
        {
            var customTabsIntent = new CustomTabsIntent.Builder().Build();
            customTabsIntent.LaunchUrl(this, Android.Net.Uri.Parse(url));

        }
        // openRemoteWebPage קריאה לפונקציה מרוחקת בשרת בשם  
        public async Task openRemoteWebPage(string localUserName, string remoteUserName, string play)
        {
            await SignalRHub.Connection.InvokeAsync("openRemoteWebPage", localUserName, remoteUserName, play);

        }

        //     עדכון הטלפון התחבר לשרת
        public void UpdateConectionStatus(string connectionId)
        {
            RunOnUiThread(async () =>
            {


                conectionText = FindViewById<TextView>(Resource.Id.connectionText);
                conectionText.Text = localUserRegestrationName + " OnLine";

                conectionText.SetBackgroundColor(Android.Graphics.Color.LightGreen);


                try
                {
                    LocalUserExistOnServer = true;

                    // שמחכה לסיום הארוע TASK הפעלת ה 
                    responseCompletionSource = new TaskCompletionSource<string>();

                    //   broadcast receiver  רישום 
                    var receiver = new SignalRBroadcastReceiver(this);
                    Application.Context.RegisterReceiver(receiver, new IntentFilter("getRemoteUserIdFromServer"));

                    await SignalRHub.Connection.InvokeAsync("getRemoteUserId", localUserRegestrationName);


                    string answer = await responseCompletionSource.Task;

                    // מבטלים הרשמה לאירוע
                    Application.Context.UnregisterReceiver(receiver);

                    if (answer == "UserNotExist" && localUserRegestrationName != null)

                    // if (LocalUserExistOnServer == false)
                    {
                        await SignalRHub.Connection.InvokeAsync("RegisterUser", localUserRegestrationName);

                        conectionText = FindViewById<TextView>(Resource.Id.connectionText);
                        conectionText.Text = " " + localUserRegestrationName + "   Is Online ";
                        conectionText.SetBackgroundColor(Android.Graphics.Color.LightGreen);


                    }


                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e);
                }







            });
        }

        //   עדכון משתמשים מחוברים
        public void UpdateConnectedUserList()
        {
            RunOnUiThread(() =>
            {
                var currentUserList = TransferUserListManager.ConnectedUsers;

                var tempCurrentUserList = currentUserList;



                foreach (var user in conectedusers)
                {
                    string tempusername = user.Name;
                    string tempconnectionid = user.ConectioId;
                    var foundUser = tempCurrentUserList.FirstOrDefault(u => u.UserName == tempusername);
                    //  משתמש קיים
                    if (foundUser != null)
                    {

                    }
                    // משתמש לא קיים
                    else
                    {
                        ConnectedUser TempConecteduser = new ConnectedUser
                        {
                            UserName = tempusername,
                            ConnectionId = tempconnectionid
                        };
                        tempCurrentUserList.Add(TempConecteduser);
                    }
                }

                currentUserList = tempCurrentUserList;

                conectedusers.Clear();

                foreach (var user in currentUserList)
                {
                    string connectionId = user.ConnectionId ?? "Unknown ID";
                    string userName = user.UserName ?? "Unknown User";

                    var newConection = new ConnectedUsersList(userName, connectionId);
                    //   "webrtc"   נבדוק ששם המשתמש לא מתחיל עם
                    //   wenrtc      שם משתמש ששמור ללקוח וידאו
                    if (!userName.StartsWith("webrtc")) conectedusers.Add(newConection);


                }


                adapter.NotifyDataSetChanged();

            });
        }

        //     הצג הודעה מתקבלת
        public void UpdateRecivedMessage(string sendingUserName, string recivingUserName, string message)
        {


            //  IF  השתמשנו  בפעמיים   כי יותר פשוט לדבג

            /*
            if (MainactivtyFocoused == true ) {

                var db = usersListdDbHelper.WritableDatabase;
                ContentValues values = new ContentValues();
                values.Put("sendingUserName", sendingUserName);
                values.Put("MessageType", "recive");
                values.Put("recivingUserName", recivingUserName);
                values.Put("MessageText", message);
                db.Insert("Messages", null, values);
                db.Close();

            } */


            if (user_detailed.User_DetailedactivtyFocoused && selectedRemoteUserName == sendingUserName)
            {


            }
            else
            {
                var db = usersListdDbHelper.WritableDatabase;
                ContentValues values = new ContentValues();
                values.Put("sendingUserName", sendingUserName);
                values.Put("MessageType", "recive");
                values.Put("recivingUserName", recivingUserName);
                values.Put("MessageText", message);
                values.Put("MessageTypeOptions", "Message");
                values.Put("FilePath", "");
                values.Put("PortraitOrlandscape", "");
                db.Insert("Messages", null, values);
                db.Close();

            }


        }

        //==============       נקרא את המשתמשים הקימים בבסיס הנתונים  =====
        public void ReadUserNamefromDataBase()
        {
            var usersInDb = usersListdDbHelper.GetUsersFromDatabase();


            RunOnUiThread(() =>
            {
                conectedusers.Clear();
                foreach (var username in usersInDb)
                {
                    string connectionId = "NoId";
                    string userName = username;
                    var newConection = new ConnectedUsersList(userName, connectionId);
                    conectedusers.Add(newConection);
                    adapter.NotifyDataSetChanged();

                }


            });

            // TransferUserListManager ניצור רשימה חדשה ונעביר לרשימת
            List<ConnectedUser> newList = conectedusers
           .Select(user => new ConnectedUser
           {
               UserName = user.Name,
               ConnectionId = user.ConectioId
           }).ToList();

            TransferUserListManager.UpdateUserList(newList);
        }

        //=================================================================

        public async void DownloadFile(string recivingUserName, string sendingUserName, string fileUrl, string portraitOrlandscape, string filetype)
        {
            if (!user_detailed.User_DetailedactivtyFocoused)
            {
                string filepath = await FileUtiles.SaveFileToDownloadsAsync(recivingUserName, sendingUserName, fileUrl);
                if (filepath != null)
                {
                    var db = usersListdDbHelper.WritableDatabase;
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

            }
        }


        public void OpenWebRtcPage(string remoteUser, string play, string audioOrVideo)
        {

            // UI -כדי למנוע תקיעה של ה  UI פונקצית עזר של אנדרואיד - להריץ את הפקודות ב
            RunOnUiThread(() => 
            {
                string userPrams1 = "webrtc-" + localUserRegestrationName;
                string userPrams2 = "webrtc-" + remoteUser;


                if (localUserRegestrationName != remoteUser)
                {
                    if (play == "Answer")
                    {

                        Intent intent = new Intent(this, typeof(VideoCallDialogActivity));
                        intent.PutExtra("Message", remoteUser);
                        intent.PutExtra("VideoOrAudio", audioOrVideo);
                        VideoCallDialogActivity.ResultCallback = HandleDialogResult;
                        StartActivity(intent);




                    }
                    
                    if (play == "Call")
                    {

                        // לביצוע השיחה פותחים דף אינטרנט המצוי בשרת המרוחק
                        // מעבירים לדף האינטרנט את הפרמטרים בשורת הכתובת
                        // param1 = localUserName 
                        //param2 = remoteUserName
                        // param3 = autoCall // מורה לדף אינטרנט ליצור קשר עם הדף המרוחק
                        // param4 = audioOrVideo 
                        //   string url = $"https://farkash-amit.tplinkdns.com:5000/webrtcchat.html#param1={userPrams1}&param2={userPrams2}&param3=Call";
                        string url = $"https://" + SignalRHub.remoteServerIp + $"/test.html#param1={userPrams1}&param2={userPrams2}&param3=Call&param4={audioOrVideo}";
                        

                        OpenWebPageWithCustomTab(url);

                    }

                }
                else { Android.Widget.Toast.MakeText(this, $"you can", Android.Widget.ToastLength.Short).Show(); }
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
                    //   string url = $"https://farkash-amit.tplinkdns.com:5000/webrtcchat.html#param1={userPrams1}&param2={userPrams2}&param3=Answer";
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





        //=====================================================================
        //     BroadcastReceiver  יורש ממחלקה  SignalBrodcastReciver  מימוש 
        //               טיפול בהודעות שמשודרות דרך הסרוויס 
        //=====================================================================


        private class SignalRBroadcastReceiver : BroadcastReceiver
        {
            //  הנוכחי כדי לאפשר גישה   activty חייבים להעביר את ה   
            //  MainActivty  למחלקה הראשית 

            private readonly MainActivity contextAactivity;

            public SignalRBroadcastReceiver(MainActivity activity)
            {
                contextAactivity = activity;
            }
            public override void OnReceive(Context context, Android.Content.Intent intent)
            {
                string action = intent.Action;

                switch (action)
                {

                    case "StatusUpdate":
                        string message = intent.GetStringExtra("Message");
                        string connectionId = intent.GetStringExtra("ConnectionId");

                        contextAactivity.UpdateConectionStatus(connectionId);

                        break;


                    case "ResetConection":
                        if (MainactivtyFocoused == true)
                        {
                            //    הסימן מסמן  _  שאנו לא רוצים לחכות לתשובה 
                            _ = contextAactivity.ResetService(signalRServiceIntent);
                        }
                        break;

                    case "ReceiveMessage":
                        message = intent.GetStringExtra("Message");
                        string sendingUserName = intent.GetStringExtra("SendingUser");
                        string recivingUserName = intent.GetStringExtra("RecivingUser");

                        contextAactivity.UpdateRecivedMessage(sendingUserName, recivingUserName, message);




                        break;

                    case "UpdateUserList":

                        contextAactivity.UpdateConnectedUserList();

                        break;

                    case "getRemoteUserIdFromServer":
                        string userName = intent.GetStringExtra("UserName");
                        string userId = intent.GetStringExtra("UserId");
                        if (userName != "UserNotExist")
                        {
                            LocalUserExistOnServer = true;
                            // contextAactivity.UpdateRemoteUserId(userName, userId);
                            contextAactivity.responseCompletionSource?.TrySetResult("UserExist");
                        }
                        else
                        {
                            LocalUserExistOnServer = false;
                            contextAactivity.responseCompletionSource?.TrySetResult("UserNotExist");

                        }




                        break;

                    case "OpenWebrtcPage":
                        if (MainactivtyFocoused == true)
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


        //==============================================








    }

}