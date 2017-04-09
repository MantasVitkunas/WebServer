using System;
using System.Configuration;

namespace WebServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = int.Parse(ConfigurationManager.AppSettings["Port"]);
            bool logAllRequests = bool.Parse(ConfigurationManager.AppSettings["LogAllRequests"]);
            string localPoint = ConfigurationManager.AppSettings["LocalPoint"];

            string accessPoint = string.Format("http://localhost:{0}/", port);

            if (System.IO.Directory.Exists(localPoint))
            {
                WebServerEngine ws = new WebServerEngine(accessPoint, localPoint, logAllRequests);
                ws.Run();
                Console.WriteLine("Server has started. Press any key to quit.");
                Console.ReadKey();
                ws.Stop();
            }
            else
            {
                Console.WriteLine(string.Format("Path \"{0}\" is invalid. Server was not started. Press any key to quit.", localPoint));
                Console.ReadKey();
            }
        }
    }
}
