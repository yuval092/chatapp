using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Media;
using System.Collections.Generic;




namespace Signalir_ChatApp
{


    public class ConnectedUser
    {


        public string ConnectionId { get; set; }
        public string UserName { get; set; }


    }



    public class ConnectedUsersList
    {
        public string Name { get; set; }
        public string ConectioId { get; set; }

        public ConnectedUsersList(string name, string conectioid)
        {
            Name = name;
            ConectioId = conectioid;
        }


    }



    public class TransferUserListManager
    {

        public static List<ConnectedUser> ConnectedUsers { get; } = new List<ConnectedUser>();


        public static void AddUser(ConnectedUser user)
        {
            ConnectedUsers.Add(user);
        }

        public static void ClearUsers()
        {
            ConnectedUsers.Clear();
        }

        public static void UpdateUserList(List<ConnectedUser> users)
        {
            ConnectedUsers.Clear();
            ConnectedUsers.AddRange(users);
        }
    }

    public class ChatMessage
    {
        public string Text { get; set; }
        public string Time { get; set; }
        public bool IsIncoming { get; set; }  // true for incoming messages, false for outgoing
        public string FilePath { get; set; }
        public string PortraitOrlandscape { get; set; }
        public string MessageTypeOptions { get; set; }   //posible oprions:  "Photo" "Voice"  "File"  "Message"

    }




    public class AudioPlayer
    {
        private MediaPlayer _mediaPlayer;

        public void PlayAudio(string filename)
        {
            if (_mediaPlayer == null)
            {
                _mediaPlayer = new MediaPlayer();
                AssetFileDescriptor fd = Android.App.Application.Context.Assets.OpenFd("ring.mp3");
                _mediaPlayer.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
                _mediaPlayer.Prepare();
            }
            _mediaPlayer.Start();
        }

        public void StopAudio()
        {
            if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Release();
                _mediaPlayer = null;
            }
        }


    }

    
        

    

}