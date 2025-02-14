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

        //   רשימה שמכילה את כול המשתמשים הרשומים 
        //   מחוברים ולא מחוברים
        public static class AllUseres
        {
            
            public static List<User> UserList = new List<User>();
        }


        public class NewOfferData //WebRTC שמועברת במהלך תהליך (offer) משמשת לאחסון נתונים הקשורים להצעה 
        {
            public object? offer { get; set; }
            public string? remoteUserId { get; set; }

        }


        public  async Task FileRecived()   // ומודיעה לו שהשרת בחיים יחד עם מחרוזת כפרמטר נוסף JKJ שולחת הודעה ללקוח ספציפי עם מזהה חיבור
        {
          
            await Clients.Client("jkj").SendAsync("ServerIsAlive", "jhjhj");
        }

        public async Task UpdateUserList() //מעדכנת את רשימת המשתמשים המחוברים
        {
            // עם מזהי החיבור ושמות המשתמש שלהם User יצירת רשימה של אובייקטים מטיפוס 
            
            foreach (var user in connectedUsers) //עוברת על כל הזוגות במבנה הנתונים שמכיל רק את המשתמשים המחוברים בזמן אמת
            {
                //UserToUpdate תואם לשם המשתמש הנוכחי מתוך מסד הנתונים של מחוברים בזמן אמת, אם ימצא שם כזה הוא ישמר במשתנה  UserName חיפוש ברשימת המשתמשים הכללית עבור משתמש ששמו  
                var userToUpdate = AllUseres.UserList.FirstOrDefault(u => u.UserName == user.Value); 

                // בדיקה האם המשתנה ריק, כלומר המשתנה אינו קיים ברשימת המשתמשים מה שאומר שאינו נרשם בעבר. אם המשתנה ריק יתווסף המשתמש  לרשימה כמשתמש חדש
                if (userToUpdate == null)
                    AllUseres.UserList.Add(new User { ConnectionId = user.Key, UserName = user.Value });

                // אם המשתנה אינו ריק דבר שאומר כי המשתמש רשום אז מעדכנים את מזהה החיבור של המשתמש הקיים כדי לוודא שהוא תואם את מזהה החיבור הנוכחי
                if(userToUpdate != null)
                {
                    userToUpdate.ConnectionId = user.Key;

                }
               
            }

            // Log the user list to the console
            Console.WriteLine("=====================");
            foreach (var user in AllUseres.UserList) // לולאה שעוברת על כל אובייקט ברשימה הכללית
            {
                //משאיר מרווח של 15 תווים עבור שם המשתמש כך שהפלט ייראה מסודר בקונסול user.UserName.PadRight(15)
                //מדפיס לקונסול את שם המשתמש ואת מזהה החיבור שלו
                Console.WriteLine($"User: {user.UserName.PadRight(15)} Connection ID: {user.ConnectionId}");
                
            }

            // שידור רשימת המשתמשים המעודכנת לכל הלקוחות
            // שולחת לכל המשתמשים אירוע עם שני פרמטרים : (1) מחרוזת לציין שמערכת השרת שידרה את העדכון, (2) רשימה המעודכנת של כל המשתמשים
            await Clients.All.SendAsync("UpdateUserList", "System", AllUseres.UserList);

        }


        // Helper method to retrieve the user's IP address
        private string? GetIpAddress()
        {
            // מקבל את ההקשר של ה-HTTP מהקשר הנוכחי של ה-SignalR
            var httpContext = Context.GetHttpContext();

            // מחזיר את כתובת ה-IP המרוחקת של הלקוח, אם קיים מידע זה
            return httpContext?.Connection.RemoteIpAddress?.ToString();
        }

        public async Task KeepAlive(string userName) //שולחת הודעה ללקוח הנוכחי (לפי מזהה החיבור) שהשרת פעיל ומכילה את שם המשתמש שלו
        {
            // מדפיס בהודעה בקונסול שהמשתמש שולח הודעת KeepAlive
            Console.WriteLine(userName + " -> KeeepAlive");

            // מאחזר את מזהה החיבור של הלקוח הנוכחי
            var connectionId = Context.ConnectionId;

            // שולח הודעה ללקוח הנוכחי (באמצעות מזהה החיבור שלו) שהשרת פעיל עם שם המשתמש
            await Clients.Client(connectionId).SendAsync("ServerIsAlive", userName);
        }

        //==============================================================================//
        // connectedUsers - רושמת משתמש חדש כשהוא מתחבר על ידי שמירת מזהה החיבור שלו ב  //
        // שלו לקונסול IPמדפיסה את שם המשתמש וה                                         //
        // משדרת את רשימת המשתמשים המעודכנת לכל הלקוחות                                 //
        //==============================================================================//
        public async Task RegisterUser(string userName) // רישום משתמש חדש למערכת כאשר הוא מתחבר לשרת
        {
            var connectionId = Context.ConnectionId; // הנוכחי websocket ה  Signalir שומרת את מזהה החיבור של המשתמש הנוכחי שנוצר על ידי     

            Console.WriteLine($"Conection ID o -> {connectionId}"); // מדפיסה למסוף את מזהה החיבור כדי שניתן יהיה לעקוב אחרי החיבור של המשתמש בהודעות השרת

            // של המשתמש המחובר IP מקבלת את כתובת ה   
            var ipAddress = GetIpAddress();


            // שלו כדי לאשר את החיבור ומידע מזהה על המשתמש IP מדפיסה למסוף את שם המשתמש וכתובת ה 
            Console.WriteLine($"User {userName} connected with IP address: {ipAddress}");

            // מוסיפה את המשתמש למסד הנתונים של המשתמשים המחוברים כעת
            connectedUsers[connectionId] = userName;

            //מייצרת הודעה זמנית שמשלבת את שם המשתמש
            var tempMessage = "Hello " + userName;

            //מדפיסה למסוף את ההודעה שהשרת היה אמור לשלוח למשתמש המחובר, כדי לעקוב אחר ההודעה שנשלחה
            Console.WriteLine($"Sending message to -> {connectionId}: {tempMessage}");





            // שידור את רשימת המשתמשים המעודכנת לכל הלקוחות
            await UpdateUserList();
        }

        //שליחת הודעה ללקוח ספציפי לפי מזהה החיבור שלו
        // sending
        public async Task SendMessageToClient(string connectionId, string sendingUser ,string recivingUser ,  string message)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", sendingUser,recivingUser , message);
        }

        //=========================================================================
        // הסר את המשתמש כאשר הוא מתנתק ושדר את רשימת המשתמשים המעודכנת
        //=========================================================================
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // הסרת המשתמש מרשימת המשתמשים המחוברים (TryRemove מסיר את המשתמש אם מזהה החיבור נמצא במילון)
            connectedUsers.TryRemove(Context.ConnectionId, out _);

            // מחפש את המשתמש ברשימת AllUseres.UserList בהתאם למזהה החיבור שלו
            var userToUpdate = AllUseres.UserList.FirstOrDefault(u => u.ConnectionId == Context.ConnectionId);

            // אם המשתמש נמצא ברשימה, מסיר אותו
            if (userToUpdate != null) AllUseres.UserList.Remove(userToUpdate);

            // שידור רשימת המשתמשים המעודכנת ללקוחות המחוברים
            await UpdateUserList();

            // קריאה לפונקציה הבסיסית שמבצעת פעולות נוספות במידת הצורך כשמשתמש מתנתק
            await base.OnDisconnectedAsync(exception);

            
        }



        // ============ קבל זיהוי משתמש מרוחק לפי שם ==============================
        //==========================================================================

        public async Task getRemoteUserId(string remoteUserId)
        {
            // מחפש את המשתמש המרוחק במילון המשתמשים המחוברים (היכן שהמפתח הוא מזהה חיבור והערך הוא שם המשתמש)
            var connection = connectedUsers.FirstOrDefault(pair => pair.Value == remoteUserId);

            // בודק אם לא נמצא משתמש עם שם המשתמש המרוחק
            if (connection.Equals(default(KeyValuePair<string, string>)))
            {
                // מדפיס למסוף הודעה שמשתמש לא קיים
                Console.WriteLine("User Not Exist");

                // מקבל את מזהה החיבור של המשתמש ששלח את הבקשה
                var connectionId = Context.ConnectionId;

                // שולח הודעה ללקוח שאומרת שהמשתמש לא קיים
                await Clients.Client(connectionId).SendAsync("getRemoteUserIdFromServer", "UserNotExist", "UserNotExist");
            }
            else
            {
                // מקבל את מזהה החיבור של המשתמש ששלח את הבקשה
                var connectionId = Context.ConnectionId;

                // שולח ללקוח את שם המשתמש המרוחק ומזהה החיבור שלו
                await Clients.Client(connectionId).SendAsync("getRemoteUserIdFromServer", connection.Value, connection.Key);
            }
        }

        //==========================================================================================
        //======          Light SQL        בסיס הנתונים  
        //==========================================================================================

        //מוסיפה משתמש חדש לבסיס הנתונים או מחזירה הודעה אם המשתמש כבר קיים
        public async Task AddNewUserToDataBase(string user, string phonenumber, string password)
        {
            // מקבל את מזהה החיבור הנוכחי
            string userConnectionId = Context.ConnectionId;

            // בודק אם המשתמש כבר קיים במערכת
            if (SqlDataBase.DoesUserExist(user))
            {
                // אם המשתמש כבר קיים, שולח הודעה ללקוח שהמשתמש כבר קיים במערכת
                await Clients.Client(userConnectionId).SendAsync("AddNewUserToDataBaseResults", user, "UserAllreadyExist");
            }
            else
            {
                // אם המשתמש לא קיים, מוסיף אותו לבסיס הנתונים
                SqlDataBase.InsertUser(user, phonenumber, password);

                // שולח הודעה ללקוח שההרשמה בוצעה בהצלחה
                await Clients.Client(userConnectionId).SendAsync("AddNewUserToDataBaseResults", user, "UserSuccessfullyRegistered");
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



    }
}
