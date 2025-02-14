using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.IO;
using Android.Widget;
using Android.Content;


using Android.Database;
using Android.Provider;
using Android.Net;
using Android.App;
using Java.IO;

namespace Signalir_ChatApp
{
    public class FileUtiles
    {

        //  הורד קובץ מהשרת
        public static async Task <string> SaveFileToDownloadsAsync(string recivingUserName, string sendingUserName, string fileUrl)
        {
            try
            {
              
                string fileName = fileUrl.Substring(fileUrl.LastIndexOf('/') + 1);

                //  Get the Downloads directory
                string downloadsPath = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, "MyChat", "ReceivedData");

                //  Create the directory if it doesn't exist
                if (!Directory.Exists(downloadsPath))
                {
                    Directory.CreateDirectory(downloadsPath);
                }

                //  Full file path
               // string fileDate = DateTime.Now.ToString("yyMMdd_HHmmss");
               // fileName = fileDate + "_" + fileName;

                string filePath = Path.Combine(downloadsPath, fileName);

                // HTTPS  למטרות בדיקה
                var handler = new HttpClientHandler
                {
                    // Bypass SSL validation for testing - always returns true regardless of certificate validity
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };

                using (var httpClient = new HttpClient(handler))
                {
                    var fileBytes = await httpClient.GetByteArrayAsync(fileUrl);

                    //  Write the file
                    System.IO.File.WriteAllBytes(filePath, fileBytes);
                    Toast.MakeText(Android.App.Application.Context, $"File saved to: {filePath}", ToastLength.Long).Show();
                    
                }
                return filePath;
            }
            catch (Exception ex)
            {
                Toast.MakeText(Android.App.Application.Context, $"Error: {ex.Message}", ToastLength.Long).Show();
                return null;
            }
        }

       
        public static string GetFileName(Android.Content.Context context, Android.Net.Uri uri)
        {
            string fileName = null;

            // Check if the URI scheme is 'content'
            if (uri.Scheme.Equals("content"))
            {
                // Use ContentResolver to query metadata
                using (var cursor = context.ContentResolver.Query(uri, null, null, null, null))
                {
                    if (cursor != null && cursor.MoveToFirst())
                    {
                        // Try to get the file name based on the URI column (MediaStore or other methods)
                        int columnIndex = cursor.GetColumnIndex(Android.Provider.MediaStore.IMediaColumns.DisplayName);
                        if (columnIndex >= 0)
                        {
                            fileName = cursor.GetString(columnIndex);
                        }
                    }
                }
            }

            // If no file name is found, use the last segment of the URI path
            return fileName ?? uri.LastPathSegment;
        }

      


        // העלה קובץ לשרת
        public static async Task<bool> UploadFileUsingStream(Android.Content.Context context, Android.Net.Uri uri, string fileName, string senderName, string receiverName)
        {
            try
            {
                using (var stream = context.ContentResolver.OpenInputStream(uri))
                {
                    var handler = new HttpClientHandler
                    {

                        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                    };


                    var client = new HttpClient(handler);

                    var content = new MultipartFormDataContent();
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    content.Add(streamContent, "file", fileName);

                    content.Add(new StringContent(senderName), "senderName");
                    content.Add(new StringContent(receiverName), "receiverName");

                    string appName = Application.Context.Resources.GetString(Resource.String.app_name);

                    var prefs = Application.Context.GetSharedPreferences(appName, FileCreationMode.Private);
                    string savedServerIpAdress = prefs.GetString("IpAdress", "NoServerIp");
                    string connectionIp = "";


                    if ((savedServerIpAdress != "NoServerIp") && (savedServerIpAdress != null))
                    {
                        savedServerIpAdress = prefs.GetString("IpAdress", "NoServerIp");
                        connectionIp = "https://" + savedServerIpAdress + $"/api/fileupload/upload";
                    }
                    else
                    {
                        connectionIp = $"https://farkash-amit.tplinkdns.com:5000/api/fileupload/upload";


                    }

                    //  var response = await client.PostAsync("https://192.168.0.111:5000/api/fileupload/upload", content);

                    var response = await client.PostAsync(connectionIp, content);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Toast.MakeText(context, $"Upload failed: {ex.Message}", ToastLength.Long).Show();
                return false;
            }

        }


        public static async Task<string> SaveFileToOutgoingDirectoryAsync(Context context, Android.Net.Uri fileUri)
        {
            var fileName = GetFileName(context, fileUri) ?? "unknown_file";

            var targetDirectory = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath + "/MyChat/Outgoing";

            
            if (!System.IO.Directory.Exists(targetDirectory))
            {
                System.IO.Directory.CreateDirectory(targetDirectory);
            }

            
            var targetFilePath = System.IO.Path.Combine(targetDirectory, fileName);

            try
            {

                if (System.IO.File.Exists(targetFilePath))
                {
                    
                    System.IO.File.Delete(targetFilePath);
                }

                await Task.Delay(3000);

                using (var inputStream = context.ContentResolver.OpenInputStream(fileUri))


                //   using (var outputStream = System.IO.File.Create(targetFilePath))
                //   {
                //       await inputStream.CopyToAsync(outputStream);
                //   }

                using (var outputStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                {
                    await inputStream.CopyToAsync(outputStream);
                }



                return targetFilePath;
            }
            catch (Exception ex)
            {
                                
                return null;

            }
        }

        public static string GetImageOrientation(string imagePath)
        {
            // Decode the image file without loading it into memory
            Android.Graphics.BitmapFactory.Options options = new Android.Graphics.BitmapFactory.Options
            {
                InJustDecodeBounds = true // Only load image bounds (dimensions)
            };
            Android.Graphics.BitmapFactory.DecodeFile(imagePath, options);

            // Compare dimensions
            if (options.OutWidth > options.OutHeight)
            {
                return "Landscape";
            }
            else if (options.OutHeight > options.OutWidth)
            {
                return "Portrait";
            }
            return null;
        }

    }

}