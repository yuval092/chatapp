using Android.Content;
using Android.Database.Sqlite;
using System.Collections.Generic;

namespace Signalir_ChatApp
{



    public class DatabaseHelper : SQLiteOpenHelper
    {
        private const string chatDatabaseName = "ChatApp.db";
        private const int DatabaseVersion = 1;

        public DatabaseHelper(Context context)
            : base(context, chatDatabaseName, null, DatabaseVersion) { }

        // Called when the database is first created
        public override void OnCreate(SQLiteDatabase db)
        {
            db.ExecSQL(@"CREATE TABLE Messages (
                        MessageId INTEGER PRIMARY KEY AUTOINCREMENT, 
                        sendingUserName TEXT NOT NULL, 
                        MessageType TEXT NOT NULL, 
                        recivingUserName TEXT NOT NULL, 
                        MessageText TEXT NOT NULL, 
                        MessageTypeOptions TEXT NOT NULL, 
                        FilePath TEXT NOT NULL, 
                        PortraitOrlandscape TEXT NOT NULL, 
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP);");
        }


        public List<Dictionary<string, string>> GetMessagesForUsers(string user1, string user2)
        {
            var messages = new List<Dictionary<string, string>>();
            var db = this.ReadableDatabase;

            // Query to select all messages where either user1 sent to user2 or user2 sent to user1, sorted by Timestamp
            string query = @"
              SELECT * FROM Messages 
              WHERE (sendingUserName = ? AND recivingUserName = ?) 
              OR (sendingUserName = ? AND recivingUserName = ?) 
              ORDER BY Timestamp ASC;";

            var cursor = db.RawQuery(query, new string[] { user1, user2, user2, user1 });

            // Process the result set
            while (cursor.MoveToNext())
            {
                var message = new Dictionary<string, string>
        {
            { "MessageId", cursor.GetInt(cursor.GetColumnIndexOrThrow("MessageId")).ToString() },
            { "sendingUserName", cursor.GetString(cursor.GetColumnIndexOrThrow("sendingUserName")) },
            { "MessageType", cursor.GetString(cursor.GetColumnIndexOrThrow("MessageType")) },
            { "recivingUserName", cursor.GetString(cursor.GetColumnIndexOrThrow("recivingUserName")) },
            { "MessageText", cursor.GetString(cursor.GetColumnIndexOrThrow("MessageText")) },
            { "MessageTypeOptions", cursor.GetString(cursor.GetColumnIndexOrThrow("MessageTypeOptions")) },
            { "FilePath", cursor.GetString(cursor.GetColumnIndexOrThrow("FilePath")) },
            { "PortraitOrlandscape", cursor.GetString(cursor.GetColumnIndexOrThrow("PortraitOrlandscape")) },
            { "Timestamp", cursor.GetString(cursor.GetColumnIndexOrThrow("Timestamp")) }
            
        };

                messages.Add(message);
            }

            cursor.Close();
            db.Close();

            return messages;
        }



        public List<string> GetUsersFromDatabase()
        {
            var db = this.ReadableDatabase;

            var query = @"
            SELECT DISTINCT sendingUserName AS UserName
            FROM Messages
            UNION
            SELECT DISTINCT recivingUserName AS UserName
            FROM Messages;
        ";

            var cursor = db.RawQuery(query, null);
            var uniqueUsernames = new List<string>();

            while (cursor.MoveToNext())
            {
                uniqueUsernames.Add(cursor.GetString(cursor.GetColumnIndexOrThrow("UserName")));
            }

            cursor.Close();
            db.Close();
            return uniqueUsernames;
        }



        // Called when the database version changes
        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            db.ExecSQL("DROP TABLE IF EXISTS Messages;");
            OnCreate(db);
        }
    }



}