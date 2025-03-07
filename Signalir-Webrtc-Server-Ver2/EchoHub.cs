namespace Signalir_Webrtc_Server
{
    using Microsoft.AspNetCore.SignalR;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Data.SQLite;
    using System.Security.Cryptography.X509Certificates;
    using static Signalir_Webrtc_Server.EchoHub;
    public class User
    {
        public string? ConnectionId { get; set; }
        public string? UserName { get; set; }
    }

    public class SyncWithServerData
    {
        public string? MyUserName { get; set; }                 // מה הוא שם המשתמש אליו אני מחובר. יכול להיות ריק.
        public List<string>? ConnectedUserNames { get; set; }   // רשימת כל המשתמשים המחוברים למערכת
    }

    public class NewOfferData //WebRTC שמועברת במהלך תהליך (offer) משמשת לאחסון נתונים הקשורים להצעה 
    {
        public object? offer { get; set; }
        public string? remoteUserId { get; set; }

    }

    public class EchoHub : Hub
    {
        public static readonly ConcurrentDictionary<string, string> connectedUsers = new();

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// מתודות של SignalR
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public override async Task OnConnectedAsync()
        {
            var ipAddress = GetIpAddress();
            var connectionId = Context.ConnectionId;
            Console.WriteLine($"[New Connection] - connectionId: {connectionId}, IP: {ipAddress}");

            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // הסרת המשתמש מרשימת המשתמשים המחוברים (TryRemove מסיר את המשתמש אם מזהה החיבור נמצא במילון)
            connectedUsers.TryRemove(Context.ConnectionId, out _);

            // שידור רשימת המשתמשים המעודכנת ללקוחות המחוברים
            await UpdateUserList();     // Do we want this??

            // קריאה לפונקציה הבסיסית שמבצעת פעולות נוספות במידת הצורך כשמשתמש מתנתק
            await base.OnDisconnectedAsync(exception);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// מתודות שנקראות מהלקוח
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task KeepAlive(string userName) //שולחת הודעה ללקוח הנוכחי (לפי מזהה החיבור) שהשרת פעיל ומכילה את שם המשתמש שלו
        {
            // מדפיס בהודעה בקונסול שהמשתמש שולח הודעת KeepAlive
            Console.WriteLine(userName + " -> KeeepAlive");

            // מאחזר את מזהה החיבור של הלקוח הנוכחי
            var connectionId = Context.ConnectionId;

            // שולח הודעה ללקוח הנוכחי (באמצעות מזהה החיבור שלו) שהשרת פעיל עם שם המשתמש
            await Clients.Client(connectionId).SendAsync("ServerIsAlive", userName);
        }

        public async Task<string> RegisterUser(string UserName, string phoneNumber, string password)
        {
            string connectionId = Context.ConnectionId;

            // 1. האם המשתמש כבר נרשם בעבר למערכת?
            if (SqlDataBase.DoesUserExist(UserName))
            {
                Console.WriteLine($"User {UserName} already exists --> REGISTER FAILED");
                return "UserExists";
            }

            // 2. הוסף את המשתמש החדש אל מסד הנתונים
            SqlDataBase.InsertUser(UserName, phoneNumber, password);

            Console.WriteLine($"Register Successful - UserName: {UserName}, connectionId: {connectionId}");
            return "Success";
        }

        public async Task<string> LoginUser(string userName, string password)
        {
            var connectionId = Context.ConnectionId;

            // 1. האם שם המשתמש והסיסמה נמצאים במסד הנתונים?
            if (!SqlDataBase.DoesUserAndPasswordExist(userName, password))
            {
                Console.WriteLine($"User {userName} doesn't exist in Database --> LOGIN FAILED");
                return "UserDoesNotExist";
            }

            // 2. האם אחד מהטלפונים האחרים כבר התחבר למשתמש זה?
            if (connectedUsers.Values.Contains(userName))
            {
                Console.WriteLine($"User {userName} is already connected in another device --> LOGIN FAILED");
                return "UserAlreadyConnected";
            }

            // 3. ההתחברות הצליחה. עדכן את רשימת המשתמשים המחוברים בהתאם
            connectedUsers[connectionId] = userName;

            // 4. שלח לכל הלקוחות את רשימת המשתמשים המעודכנת
            await UpdateUserList();

            Console.WriteLine($"Login Successful - Username: {userName}, connectionId: {connectionId}");
            return "Success";
        }

        public async Task<string, List<User>> SyncWithServer()
        {
            var connectionId = Context.ConnectionId;

            // 1. האם הלקוח הנוכחי נמצא ברשימת המתמשים המחוברים?
            //    במידה וכן - קח את שם המשתמש אליו הוא מחובר.
            string userName = null;
            if (connectedUsers.TryGetValue(connectionId, out string value))
            {
                userName = value;
            }

            // 2. תשמור את רשימת כל המשתמשים המחוברים כעת.
            //    אל תשמור את המשתמש שהלקוח מחובר אליו.
            List<string> usersList = new List<string>();
            foreach (var user in connectedUsers)
            {
                if (user != null && !string.IsNullOrEmpty(user.Value) && user.Value != userName)
                {
                    usersList.Add(user.Value);
                }
            }

            // 3. החזר את התשובה ללקוח
            SyncWithServerData result = new SyncWithServerData();
            result.ConnectedUserNames = usersList;
            result.MyUserName = userName;

            Console.WriteLine($"SyncWithServer Successful - Username: {result.MyUserName}");
            return result;
        }

        public async Task<string> SendMessageToUser(string sendingUserName ,string receivingUserName, string message)
        {
            var connectionId = Context.ConnectionId;

            // 1. תוודא שהלקוח מחובר למשתמש, ושמשתמש זה תואם לשם משתמש השולח (sendingUserName)
            if (connectedUsers.TryGetValue(connectionId, out string value))
            {
                if (sendingUserName != value)
                {
                    Console.WriteLine($"User {sendingUserName} doesn't belong to current client --> SEND MESSAGE FAILED");
                    return "BadSendingUserName";
                }
            }
            else
            {
                Console.WriteLine($"Please login before sending a message --> SEND MESSAGE FAILED");
                return "SendingUserNameDidntLogin";
            }

            // 2. תוודא ששם המשתמש אליו שולחים מחובר כעת למערכת, כלומר מישהו התחבר אליו
            if (!connectedUsers.Values.Contains(receivingUserName))
            {
                Console.WriteLine($"Receiving User {receivingUserName} isn't connected --> SEND MESSAGE FAILED");
                return "ReceivingUserNameDidntLogin";
            }

            // 3. תוודא שההודעה שרוצים לשלוח אינה ריקה
            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine($"Can't send an empty or null message --> SEND MESSAGE FAILED");
                return "MessageCantBeEmpty";
            }

            // 4. שלח את ההודעה אל היעד
            await Clients.Client(connectionId).SendAsync("ReceiveMessageFromUser", sendingUserName, receivingUserName, message);

            Console.WriteLine($"SendMessageToUser Successful - SendingUserName: {sendingUserName}, ReceivingUserName: {receivingUserName}");
            return "Success";
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// מתודות שהשרת שולח ללקוח
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task UpdateUserList()
        {
            List<User> usersList = new List<User>();
            foreach (var user in connectedUsers)
            {
                if (user != null && ! string.IsNullOrEmpty(user.Key) && ! string.IsNullOrEmpty(user.Value))
                {
                    usersList.Add(new User { ConnectionId = user.Key, UserName = user.Value });
                }
            }

            // Log the user list to the console
            Console.WriteLine("================ Current Users ================");
            foreach (var user in usersList) // לולאה שעוברת על כל אובייקט ברשימה הכללית
            {
                //משאיר מרווח של 15 תווים עבור שם המשתמש כך שהפלט ייראה מסודר בקונסול user.UserName.PadRight(15)
                //מדפיס לקונסול את שם המשתמש ואת מזהה החיבור שלו
                Console.WriteLine($"User: {user.UserName.PadRight(15)} Connection ID: {user.ConnectionId}");
            }

            // שידור רשימת המשתמשים המעודכנת לכל הלקוחות
            await Clients.All.SendAsync("UpdateUserList", usersList);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// WEB-RTC
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task SendCandidate(object data, string remoteUserId)
        {
            // שולח את המידע (ICE candidate) ללקוח עם מזהה החיבור של המשתמש המרוחק
            await Clients.Client(remoteUserId).SendAsync("ReceiveCandidate", data);
        }

        public async Task Offer(object data, string remoteUserId)
        {
            // יוצר אובייקט שמכיל את ההצעה החדשה וכולל את מזהה המשתמש המרוחק ואת הנתונים
            var tempnewOfferData = new NewOfferData
            {
                remoteUserId = Context.ConnectionId,
                offer = data
            };

            // שולח את ההצעה למשתמש המרוחק
            await Clients.Client(remoteUserId).SendAsync("offer", tempnewOfferData);
        }

        public async Task answer(object answer, string remoteUserId)
        {
            // שולח את התשובה ללקוח עם מזהה החיבור של המשתמש המרוחק
            await Clients.Client(remoteUserId).SendAsync("answer", answer);
        }

        public async Task endCall(string remoteUserId)
        {
            // שולח הודעה למשתמש המרוחק שהשיחה הסתיימה
            await Clients.Client(remoteUserId).SendAsync("RemoteCallEnded");
        }

        public async Task GetWebRtcUserId(string localUserName, string remoteUserName)
        {
            int count = 0;

            // לולאה שמנסה עד 20 פעמים למצוא את שני המשתמשים ברשימת המשתמשים המחוברים
            for (; count < 20; count++)
            {
                var localConnection = connectedUsers.FirstOrDefault(pair => pair.Value == localUserName);
                var remoteConnection = connectedUsers.FirstOrDefault(pair => pair.Value == remoteUserName);

                // בודק אם שני המשתמשים נמצאים ברשימת המשתמשים המחוברים
                if (!localConnection.Equals(default(KeyValuePair<string, string>)) &&
                    !remoteConnection.Equals(default(KeyValuePair<string, string>)))
                {
                    string localId = "";
                    string remoteId = "";

                    // מוצא את מזהה החיבור של המשתמש המקומי
                    var connection = connectedUsers.FirstOrDefault(pair => pair.Value == localUserName);
                    if (!connection.Equals(default(KeyValuePair<string, string>)))
                    {
                        localId = connection.Key;
                    }
                    else
                    {
                        Console.WriteLine("User Not Exist");
                    }

                    // מוצא את מזהה החיבור של המשתמש המרוחק
                    connection = connectedUsers.FirstOrDefault(pair => pair.Value == remoteUserName);
                    if (!connection.Equals(default(KeyValuePair<string, string>)))
                    {
                        remoteId = connection.Key;
                    }
                    else
                    {
                        Console.WriteLine("User Not Exist");
                    }

                    // שולח לשני המשתמשים את מזהה ה-WebRTC
                    await Clients.Client(localId).SendAsync("WebRtcIdfromServer", localId, remoteId);

                    count = 20; // עוצר את הלולאה לאחר שהמשתמשים נמצאו
                }
                else
                {
                    // ממתין חצי שנייה לפני ניסיון נוסף
                    await Task.Delay(500);
                }
            }
        }

        //מחזירה את המזהה חיבור של המשתמש שאליו מעוניינים להתקשר
        public async Task openRemoteWebPage(string localUserName, string remoteUserName, string play, string audioOrVideo)
        {
            // מחפש את המשתמש המקומי במילון המחוברים לפי שם המשתמש ברשימה של המשתמשים המחוברים כעת
            var connection = connectedUsers.FirstOrDefault(pair => pair.Value == localUserName);

            // בודק אם נמצא חיבור מתאים. אם לא, מדפיס למסוף שהמשתמש לא קיים
            if (connection.Equals(default(KeyValuePair<string, string>)))
            {
                Console.WriteLine("User Not Exist");
            }
            else
            {
                // אם נמצא חיבור מתאים, מדפיס למסוף את שם המשתמש המקומי ואת מזהה החיבור שלו.
                Console.WriteLine($"Found ->  {localUserName}: {connection.Key}");

                // מזהה החיבור של המשתמש המקומי נשמר במשתנה כדי לשלוח אליו הודעה בהמשך
                string connectionId = connection.Key;

                // שולחת הודעה ללקוח עם מזהה החיבור, ההודעה כוללת את הפרמטרים שנשלחו לפונקציה
                await Clients.Client(connectionId).SendAsync("OpenWebrtcPage", remoteUserName, play, audioOrVideo);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// מתודות עזר
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // Helper method to retrieve the user's IP address
        private string? GetIpAddress()
        {
            // מקבל את ההקשר של ה-HTTP מהקשר הנוכחי של ה-SignalR
            var httpContext = Context.GetHttpContext();

            // מחזיר את כתובת ה-IP המרוחקת של הלקוח, אם קיים מידע זה
            return httpContext?.Connection.RemoteIpAddress?.ToString();
        }
    }
}
