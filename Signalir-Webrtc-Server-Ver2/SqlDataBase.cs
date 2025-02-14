namespace Signalir_Webrtc_Server
{
    using System; // שימוש בספרייה למבני נתונים בסיסיים כמו Console
    using System.Data.SQLite; // שימוש בספריית SQLite לניהול מסדי נתונים
    using static Signalir_Webrtc_Server.EchoHub; // שימוש במחלקה EchoHub באופן סטטי

    public class SqlDataBase
    {
        // מחרוזת חיבור למסד הנתונים SQLite
        private const string connectionString = "Data Source=users.db;Version=3;";

        // יצירת מסד הנתונים והטבלה
        public static void CreateDatabase()
        {
            using (var connection = new SQLiteConnection(connectionString)) // פתיחת חיבור למסד הנתונים
            {
                connection.Open(); // פתיחת החיבור

                // שאילתה ליצירת טבלה בשם Users אם היא לא קיימת
                string createTableQuery = "CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY, UserName TEXT, Phone TEXT , PassWord TEXT ,ConnectionID TEXT)";
                using (var cmd = new SQLiteCommand(createTableQuery, connection)) // הכנת פקודת SQL לביצוע
                {
                    cmd.ExecuteNonQuery(); // ביצוע השאילתה
                }

                connection.Close(); // סגירת החיבור למסד הנתונים
            }
        }

        // הכנסה של משתמש חדש לטבלת Users
        public static void InsertUser(string userName, string phone, string password)
        {
            using (var connection = new SQLiteConnection(connectionString)) // פתיחת חיבור למסד הנתונים
            {
                connection.Open(); // פתיחת החיבור

                // שאילתה להוספת משתמש חדש עם שם, טלפון, סיסמה ומזהה חיבור ריק
                string insertQuery = "INSERT INTO Users (UserName, Phone , PassWord ,ConnectionID ) VALUES (@UserName, @Phone ,@PassWord ,@ConnectionID)";
                using (var cmd = new SQLiteCommand(insertQuery, connection)) // הכנת פקודת SQL לביצוע
                {
                    string ConnectionID = "Empty"; // הגדרת מזהה חיבור כ"Empty"
                    cmd.Parameters.AddWithValue("@UserName", userName); // הוספת ערך לשם משתמש
                    cmd.Parameters.AddWithValue("@Phone", phone); // הוספת ערך לטלפון
                    cmd.Parameters.AddWithValue("@PassWord", password); // הוספת ערך לסיסמה
                    cmd.Parameters.AddWithValue("@ConnectionID", ConnectionID); // הוספת מזהה חיבור ריק

                    cmd.ExecuteNonQuery(); // ביצוע השאילתה
                }

                connection.Close(); // סגירת החיבור למסד הנתונים
            }
        }

        // שליפת כל המשתמשים ממסד הנתונים
        public static void ReadUsers()
        {
            using (var connection = new SQLiteConnection(connectionString)) // פתיחת חיבור למסד הנתונים
            {
                connection.Open(); // פתיחת החיבור

                string selectQuery = "SELECT * FROM Users"; // שאילתה לשליפת כל הרשומות מהטבלה Users
                using (var cmd = new SQLiteCommand(selectQuery, connection)) // הכנת פקודת SQL לביצוע
                {
                    using (var reader = cmd.ExecuteReader()) // קריאת התוצאות מהשאילתה
                    {
                        while (reader.Read()) // מעבר על כל רשומה שנמצאה
                        {
                            // הדפסת פרטי המשתמש לקונסול
                            Console.WriteLine($"ID: {reader["Id"]}, UserName: {reader["UserName"]}, Phone: {reader["Phone"]}");
                        }
                    }
                }

                connection.Close(); // סגירת החיבור למסד הנתונים
            }
        }

        // עדכון רשימת המשתמשים מתוך מסד הנתונים
        public static void UpdateUserListFromDataBase()
        {
            using (var connection = new SQLiteConnection(connectionString)) // פתיחת חיבור למסד הנתונים
            {
                connection.Open(); // פתיחת החיבור

                string selectQuery = "SELECT * FROM Users"; // שאילתה לשליפת כל הרשומות מהטבלה Users
                using (var cmd = new SQLiteCommand(selectQuery, connection)) // הכנת פקודת SQL לביצוע
                {
                    using (var reader = cmd.ExecuteReader()) // קריאת התוצאות מהשאילתה
                    {
                        while (reader.Read()) // מעבר על כל רשומה שנמצאה
                        {
                            // הדפסת פרטי המשתמש לקונסול
                            Console.WriteLine($"ID: {reader["Id"]}, UserName: {reader["UserName"]}, Phone: {reader["Phone"]}");
                            // הוספת המשתמש לרשימת כל המשתמשים (UserList)
                            string userName = reader["UserName"]?.ToString() ?? string.Empty;
                            string connectionId = reader["ConnectionID"]?.ToString() ?? string.Empty;
                            AllUseres.UserList.Add(new User { ConnectionId = connectionId, UserName = userName });
                        }
                    }
                }

                connection.Close(); // סגירת החיבור למסד הנתונים
            }
        }

        // בדיקה אם משתמש קיים לפי שם משתמש
        public static bool DoesUserExist(string userName)
        {
            using (var connection = new SQLiteConnection(connectionString)) // פתיחת חיבור למסד הנתונים
            {
                connection.Open(); // פתיחת החיבור

                // שאילתה לספירת מספר המשתמשים עם שם המשתמש המבוקש
                string query = "SELECT COUNT(1) FROM Users WHERE UserName = @UserName";

                using (var cmd = new SQLiteCommand(query, connection)) // הכנת פקודת SQL לביצוע
                {
                    cmd.Parameters.AddWithValue("@UserName", userName); // הוספת שם המשתמש לשאילתה

                    int count = Convert.ToInt32(cmd.ExecuteScalar()); // קבלת התוצאה כמספר שלם

                    if (count > 0) return true; // החזרת true אם יש לפחות משתמש אחד
                    else return false; // החזרת false אם אין משתמשים עם שם כזה
                }
            }
        }

        // מחיקת מסד הנתונים כולו
        public static void DeleteDataBase()
        {
            string databaseFileName = "users.db"; // שם קובץ מסד הנתונים
            if (File.Exists(databaseFileName)) // בדיקה אם קובץ המסד קיים
            {
                File.Delete(databaseFileName); // מחיקת קובץ המסד
                Console.WriteLine("Database deleted successfully."); // הדפסת הודעה שהמסד נמחק בהצלחה
            }
        }
    }
}
