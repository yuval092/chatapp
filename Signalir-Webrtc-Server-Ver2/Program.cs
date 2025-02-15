
using System.Net.Sockets;
using System.Net;
using Signalir_Webrtc_Server;
using static Signalir_Webrtc_Server.EchoHub;


var builder = WebApplication.CreateBuilder(args);


var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";  // Default to 5000 if no PORT environment variable
builder.WebHost.UseUrls($"https://0.0.0.0:{port}");






builder.Services.AddControllers();

builder.Services.AddSignalR();

var app = builder.Build();


// Helper method to get the local IP address
string GetLocalIPAddress()
{
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == AddressFamily.InterNetwork) // Ensure it's an IPv4 address
        {
            return ip.ToString(); // Return the first found IPv4 address
        }
    }
    return "Unknown IP";
}

// Helper method to extract port from the application's URLs
int GetPortFromUrls(IEnumerable<string> urls)
{
    foreach (var url in urls)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Port; // Return the port part of the URL
        }
    }
    return 0; // Default to 0 if no port is found
}



// Print server IP and port when the application starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    string localIpAddress = GetLocalIPAddress();

    // Get the port from the application's URLs
    int port = GetPortFromUrls(app.Urls);

    FileUploadController.serverIpAdress = localIpAddress;
    FileUploadController.serverPort = port.ToString();

    // Print the server's IP address and port
    Console.WriteLine("==================================================================");
    Console.WriteLine($"Server is listening on IP address: {localIpAddress}, Port: {port}");
    Console.WriteLine($"Type to Conect    https://:{localIpAddress}:{port}/chat.html ");
    Console.WriteLine("==================================================================");
});



// Configure the HTTPS request pipeline.
// הקוד בודק האם היישום לא פועל בסביבת פיתוח
if (!app.Environment.IsDevelopment()) 
{
    app.UseHttpsRedirection(); // HTTPS מבטיחה תקשורת מאובטחת בין המשתמש לשרת בסביבת ייצור על ידי פרוטוקול 
   
    app.UseExceptionHandler("/Error");
    // כאשר נוצרת שגיאה, במקום להציג פרטי שגיאה מפורטים (כפי שעושים בסביבת פיתוח), המשתמש יופנה לנתיב /Error, שם ניתן להציג דף שגיאה מותאם אישית.
    // מאפשר שימוש ב TRY ו - CATCH לטיפול בשגיאות
}




app.UseStaticFiles(); // WWWROOT בתוכנית המקומית HTML חיפוש קבצי 

app.UseRouting(); //HTML מאפשר הפניות לדפי 

//app.UseAuthorization();

app.MapControllers(); // מאפשרת פעולת מיפוי
app.MapHub<EchoHub>("/echoHub"); // HECOHUB.CS ועושה מיפוי לקובץ שנקרא  HECOHUB מקשר בין ה
app.MapFallbackToFile("index.html"); // Serves `wwwroot/index.html` by default

SqlDataBase.DeleteDataBase();
SqlDataBase.CreateDatabase();

// SqlDataBase.InsertUser("Test1", "11111111", "1234567890");
// SqlDataBase.InsertUser("Test2", "22222222", "1234567890");
// SqlDataBase.InsertUser("Test3", "33333333", "1234567890");
// SqlDataBase.UpdateUserListFromDataBase();




app.Run();
