namespace Signalir_Webrtc_Server
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SignalR;
    using System.IO;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadController : ControllerBase
    {
        // משתנים סטטיים לאחסון כתובת IP ופורט של השרת
        public static string serverIpAdress = "";
        public static string serverPort = "";

        // ספריית ההעלאות בה יאוחסנו קבצים
        private readonly string _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        // אובייקט HubContext כדי לשלוח הודעות בין הלקוחות דרך ה-SignalR Hub
        private readonly IHubContext<EchoHub> hubcontext;

        // בנאי שמקבל HubContext ומוודא שהוא לא null
        public FileUploadController(IHubContext<EchoHub> hubContext)
        {
            hubcontext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        }

        // פעולת העלאת קובץ יחיד
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromForm] string senderName, [FromForm] string receiverName)
        {
            // בדיקה אם קובץ לא נשלח או שהגודל שלו הוא 0
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded."); // החזרת הודעת שגיאה
            }

            // יצירת ספריית ההעלאות אם היא לא קיימת
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }

            // יצירת שם קובץ חדש המבוסס על התאריך והשעה הנוכחיים
            string newFileNmae = DateTime.Now.ToString("yyMMdd_HHmmss") + "_" + file.FileName;
            var filePath = Path.Combine(_uploadDirectory, newFileNmae);

            // שמירת הקובץ בשרת
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // אם הקובץ נשמר בהצלחה בשרת
            if (System.IO.File.Exists(filePath))
            {
                // יצירת כתובת URL לקובץ על סמך כתובת IP ופורט של השרת
                string serverLocalIp = "https://" + serverIpAdress + ":" + serverPort;

                // שליחת הודעה ללקוח המקבל אם הוא מחובר
                var connection = EchoHub.connectedUsers.FirstOrDefault(pair => pair.Value == receiverName);
                if (connection.Value != null && connection.Key != null)
                {
                    // יצירת URL להורדת הקובץ
                    string fileUrl = serverLocalIp + $"/api/FileUpload/download/{newFileNmae}";

                    // זיהוי אם התמונה היא פורטרייט או לנדסקייפ
                    string portraitOrlandscape = "Landscape";
                    if (ImagePortraitOrLandscape(filePath)) portraitOrlandscape = "Portrait";

                    // שליחת הודעה ללקוח דרך ה-SignalR עם פרטי הקובץ
                    await hubcontext.Clients.Client(connection.Key).SendAsync("FileWaitingForDownload",
                        receiverName, senderName, fileUrl, portraitOrlandscape, "Photo");
                }

                // הדפסת שם המשתמש ופרטי ההעלאה לשרת
                Console.WriteLine(connection.Key + " " + newFileNmae + " uploaded successfully");
            }

            return Ok(new { Message = "File uploaded successfully." });
        }

        // פעולת העלאת קובץ במקטעים (chunks)
        [HttpPost("uploadChunk")]
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, [FromForm] string fileName, [FromForm] int chunkIndex)
        {
            // בדיקה אם קובץ או שם קובץ לא נשלחו
            if (chunk == null || string.IsNullOrEmpty(fileName))
            {
                return BadRequest("Invalid file chunk or file name."); // החזרת הודעת שגיאה
            }

            // יצירת ספריית ההעלאות אם היא לא קיימת
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }

            var filePath = Path.Combine(_uploadDirectory, fileName);

            // הוספת המקטע לקובץ קיים
            using (var stream = new FileStream(filePath, FileMode.Append))
            {
                await chunk.CopyToAsync(stream);
            }
            Console.WriteLine("Chunk File uploaded successfully");
            return Ok(new { Message = $"Chunk {chunkIndex} uploaded successfully." });
        }

        // פעולת הורדת קובץ על ידי לקוח
        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            var filePath = Path.Combine(_uploadDirectory, fileName);

            // בדיקה אם הקובץ קיים
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "File not found." }); // החזרת הודעת שגיאה אם הקובץ לא קיים
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/octet-stream"; // ייצוג כללי של קובץ (כגון תמונה)
            return File(fileBytes, contentType, fileName); // החזרת הקובץ להורדה ללקוח
        }

        // פונקציה לבדיקת כיוון התמונה (פורטרייט או לנדסקייפ)
        public bool ImagePortraitOrLandscape(string filepath)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load(filepath); // טוען את התמונה מהקובץ

                // מחזירה true אם התמונה היא פורטרייט
                return image.Height > image.Width;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false; // מחזיר false אם התמונה לא נטענה בהצלחה
            }
        }
    }
}
