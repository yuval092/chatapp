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

    public class EchoHub : Hub
    {
        // Store connected users
        public static readonly ConcurrentDictionary<string, string> connectedUsers = new();
        public class User // מחלקה בשם 
        {
            public string? ConnectionId { get; set; }
            public string? UserName { get; set; }
        }

        // //   רשימה שמכילה את כול המשתמשים הרשומים 
        // //   מחוברים ולא מחוברים
        // public static class AllUsers
        // {
            
        //     public static List<User> UserList = new List<User>();
        // }


        public class NewOfferData //WebRTC שמועברת במהלך תהליך (offer) משמשת לאחסון נתונים הקשורים להצעה 
        {
            public object? offer { get; set; }
            public string? remoteUserId { get; set; }

        }

        public  async Task FileRecived()   // ומודיעה לו שהשרת בחיים יחד עם מחרוזת כפרמטר נוסף JKJ שולחת הודעה ללקוח ספציפי עם מזהה חיבור
        {
            await Clients.Client("jkj").SendAsync("ServerIsAlive", "jhjhj");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// מתודות של SignalR
        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //=========================================================================
        // הסר את המשתמש כאשר הוא מתנתק ושדר את רשימת המשתמשים המעודכנת
        //=========================================================================
        
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

            // מחפש את המשתמש ברשימת AllUsers.UserList בהתאם למזהה החיבור שלו
            // var userToUpdate = AllUsers.UserList.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            // אם המשתמש נמצא ברשימה, מסיר אותו
            // if (userToUpdate != null) AllUsers.UserList.Remove(userToUpdate);

            // שידור רשימת המשתמשים המעודכנת ללקוחות המחוברים
            await UpdateUserList();

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

        public async Task<string> LoginUser(string userName, string password) // רישום משתמש חדש למערכת כאשר הוא מתחבר לשרת
        {
            var connectionId = Context.ConnectionId; // הנוכחי websocket ה  Signalir שומרת את מזהה החיבור של המשתמש הנוכחי שנוצר על ידי     

            // בדוק אם המשתמש קיים במסד הנתונים, כלומר אם הוא נרשם בעבר.
            // במידה ואינו נרשם בעבר, נסרב לתת לו להתחבר ונעדכן את הלקוח.
            if (!SqlDataBase.DoesUserAndPasswordExist(userName, password))
            {
                Console.WriteLine($"User {userName} doesn't exist in Database --> LOGIN FAILED");
                return "UserDoesNotExist";
            }

            // מוסיפה את המשתמש למסד הנתונים של המשתמשים המחוברים כעת
            connectedUsers[connectionId] = userName;
            Console.WriteLine($"Login Successful - Username: {userName}, connectionId: {connectionId}");

            // שידור את רשימת המשתמשים המעודכנת לכל הלקוחות
            await UpdateUserList();

            // החזר תשובה ללקוח שהכל הצליח
            return "Success";
        }

        // ============ קבל זיהוי משתמש מרוחק לפי שם ==============================
        //==========================================================================

        public async Task<string, List<User>> GetStatusFromServer()
        {
            string username = "NoLogin";
            var connectionId = Context.ConnectionId;
            if (connectedUsers.TryGetValue(connectionId, out string value))
            {
                username = value;
            }

            List<User> usersList = new List<User>();
            foreach (var user in connectedUsers)
            {
                if (user != null && ! string.IsNullOrEmpty(user.Key) && ! string.IsNullOrEmpty(user.Value))
                {
                    usersList.Add(new User { ConnectionId = user.Key, UserName = user.Value });
                }
            }

            return (username, usersList);
        }

        //==========================================================================================
        //======          Light SQL        בסיס הנתונים  
        //==========================================================================================

        //מוסיפה משתמש חדש לבסיס הנתונים או מחזירה הודעה אם המשתמש כבר קיים
        public async Task<string> RegisterNewUser(string username, string phonenumber, string password)
        {
            // מקבל את מזהה החיבור הנוכחי
            string userConnectionId = Context.ConnectionId;

            // בודק אם המשתמש כבר קיים במערכת
            if (SqlDataBase.DoesUserExist(username))
            {
                // אם המשתמש כבר קיים, שולח הודעה ללקוח שהמשתמש כבר קיים במערכת
                return "UserExists";
            }
            else
            {
                // אם המשתמש לא קיים, מוסיף אותו לבסיס הנתונים
                SqlDataBase.InsertUser(username, phonenumber, password);

                // שולח הודעה ללקוח שההרשמה בוצעה בהצלחה
                return "Success";
            }
        }

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

        // Helper method to retrieve the user's IP address
        private string? GetIpAddress()
        {
            // מקבל את ההקשר של ה-HTTP מהקשר הנוכחי של ה-SignalR
            var httpContext = Context.GetHttpContext();

            // מחזיר את כתובת ה-IP המרוחקת של הלקוח, אם קיים מידע זה
            return httpContext?.Connection.RemoteIpAddress?.ToString();
        }

        //שליחת הודעה ללקוח ספציפי לפי מזהה החיבור שלו
        // sending
        public async Task SendMessageToClient(string connectionId, string sendingUser ,string recivingUser ,  string message)
        {
            Console.WriteLine($"M E O W: {connectionId} {sendingUser} {recivingUser}");
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", sendingUser,recivingUser , message);
        }
    }
}
