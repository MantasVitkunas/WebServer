using System;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
namespace WebServer
{
    class WebServerEngine
    {
        #region Objects for internal use
        private enum ResponseStatus
        {
            OK = 200,
            NotFound = 404,
            Unsuported = 415
        }

        private class ParsedResponse
        {
            public ResponseStatus ResponseStatus { get; set; }
            public byte[] ResponseBytes { get; set; }
            public string MimeType { get; set; }

            public ParsedResponse()
            {
                ResponseStatus = ResponseStatus.OK;
                ResponseBytes = Encoding.UTF8.GetBytes("Server is up and running.");
                MimeType = "text/html";
            }
        }
        #endregion

        private readonly HttpListener listener = new HttpListener();
        private readonly bool logAllRequests;
        private readonly string rootPath;

        public WebServerEngine(string accessPoint, string rootPath, bool logAllRequests)
        {
            listener.Prefixes.Add(accessPoint);
            this.logAllRequests = logAllRequests;
            this.rootPath = rootPath;

            listener.Start();
        }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                         {
                             HttpListenerContext context = c as HttpListenerContext;
                             try
                             {
                                 if (this.logAllRequests)
                                 {
                                     Console.WriteLine(string.Format("{0} \tRequest \t{1} \t{2}", DateTime.Now, context.Request.UserHostName, context.Request.Url));
                                 }
                                 ParsedResponse response = HandleRequest(context.Request);

                                 context.Response.ContentType = response.MimeType;
                                 context.Response.StatusCode = (int)response.ResponseStatus;
                                 context.Response.ContentLength64 = response.ResponseBytes.Length;
                                 context.Response.OutputStream.Write(response.ResponseBytes, 0, response.ResponseBytes.Length);
                             }
                             catch (Exception e)
                             {
                                 Console.WriteLine(string.Format("{0} \tException \t{1}", DateTime.Now, e.Message));
                             }
                             finally
                             {
                                 context.Response.OutputStream.Close();
                             }
                         }, listener.GetContext());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(string.Format("{0} \tException \t{1}", DateTime.Now, e.Message));
                }
            });
        }

        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }

        /// <summary>
        /// Handles single requests
        /// </summary>
        /// <param name="request">Request entity</param>
        /// <returns></returns>
        private ParsedResponse HandleRequest(HttpListenerRequest request)
        {
            string requestPath = WebUtility.UrlDecode( request.Url.AbsolutePath);
            if (requestPath.EndsWith("/"))
            {
                requestPath = string.Format("{0}Index.html", requestPath);
            }

            //remove leading slash for path.combine to work
            requestPath = requestPath.Substring(1, requestPath.Length - 1);

            requestPath = Path.GetFullPath(Path.Combine(rootPath, requestPath));

            if (File.Exists(requestPath))
            {
                string mimeType = GetMimeType(Path.GetExtension(requestPath));
                if (mimeType.Length == 0)
                {
                    // unsupported file type
                    return new ParsedResponse()
                    {
                        ResponseStatus = ResponseStatus.Unsuported,
                        ResponseBytes = File.ReadAllBytes(string.Format("{0}/415.html", rootPath))
                    };
                }
                else
                {
                    return new ParsedResponse()
                    {
                        MimeType = mimeType,
                        ResponseStatus = ResponseStatus.OK,
                        ResponseBytes = File.ReadAllBytes(requestPath)
                    };
                }

            }
            else
            {
                return new ParsedResponse()
                {
                    ResponseStatus = ResponseStatus.NotFound,
                    ResponseBytes = File.ReadAllBytes(string.Format("{0}/404.html", rootPath))
                };
            }
        }

        /// <summary>
        /// Returns MIME type for supported file types
        /// </summary>
        /// <param name="extension">File type extension</param>
        /// <returns></returns>
        private string GetMimeType(string extension)
        {
            extension = extension.Substring(1, extension.Length - 1);
            switch (extension)
            {
                case "html":
                case "css":
                    return string.Format("text/{0}", extension);

                case "js":
                    return "application/x-javascript";

                case "txt":
                    return "text/plain";

                case "jpg":
                case "jpeg":
                    return "image/jpeg";

                case "gif":
                case "png":
                    return string.Format("image/{0}", extension);

                default:
                    return "";
            }
        }
    }
}
