using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using System.Web.Hosting;
using System.Globalization;
using System.Web.SessionState;
using System.IO.Compression;

namespace Bee.Web
{
    sealed class MimeTypes
    {
        static Dictionary<string, string> mimeTypes;

        static MimeTypes()
        {
            mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            mimeTypes.Add("css", "text/css");
            mimeTypes.Add("gif", "image/gif");
            
            mimeTypes.Add("jpeg", "image/jpeg");
            mimeTypes.Add("jpe", "image/jpeg");
            mimeTypes.Add("jpg", "image/jpeg");
            mimeTypes.Add("js", "application/x-javascript");
            
            mimeTypes.Add("png", "image/png");
            
            mimeTypes.Add("xml", "text/xml");
            mimeTypes.Add("log", "text/css");

            mimeTypes.Add("doc", "application/msword");
            mimeTypes.Add("xls", "application/excel");
        }

        public static string GetMimeType(string fileName)
        {
            string result = null;
            int dot = fileName.LastIndexOf('.');

            if (dot != -1 && fileName.Length > dot + 1)
                mimeTypes.TryGetValue(fileName.Substring(dot + 1), out result);
            else
                mimeTypes.TryGetValue(fileName, out result);

            if (result == null)
                result = "application/octet-stream";

            return result;
        }
    }

    public class StaticFileHandler : IHttpHandler, IRequiresSessionState
    {
        private static DateTime dllLastModifiedDate;

        static StaticFileHandler()
        {
            Bee.Web.FxVirtualPathProvider.RegisterToHostingEnvironment(false);

            string physicalPath = typeof(BeeVirtualFile).Assembly.Location;
            FileInfo info = new FileInfo(physicalPath);
            if (info.Exists)
            {
                dllLastModifiedDate = new DateTime(info.LastWriteTime.Year, info.LastWriteTime.Month,
                info.LastWriteTime.Day, info.LastWriteTime.Hour, info.LastWriteTime.Minute, info.LastWriteTime.Second, 0);

                DateTime now = DateTime.Now;

                if (dllLastModifiedDate > now)
                {
                    dllLastModifiedDate = new DateTime(now.Ticks - (now.Ticks % 0x989680L));
                }
            }
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            string path = request.Path;
            string physicalPath = request.PhysicalPath;

            VirtualFile vf = null;
            string filePath = request.FilePath;
            if (HostingEnvironment.VirtualPathProvider.FileExists(filePath))
            {
                vf = HostingEnvironment.VirtualPathProvider.GetFile(filePath);
            }

            if (vf == null)
            {
                response.StatusCode = 404;
                response.End();
                return;
            }

            response.ContentType = MimeTypes.GetMimeType(physicalPath);
            DateTime now = DateTime.Now;
            DateTime lastModified = DateTime.Now;

            if (vf is BeeVirtualFile)
            {
                DateTime dt;
                if (DateTime.TryParse(request.Headers["If-Modified-Since"], out dt))
                {
                    if ((dllLastModifiedDate - dt).TotalSeconds < 20.0)
                    {
                        response.StatusCode = 304;
                        response.End();
                        return;
                    }
                }

                using (Stream stream = vf.Open())
                {
                    string etag = GenerateETag(context, dllLastModifiedDate, now);

                    response.AppendHeader("Last-Modified", FormatHttpDateTime(dllLastModifiedDate));
                    response.AppendHeader("ETag", etag);
                    response.AppendHeader("Expires", FormatHttpDateTime(DateTime.Now.AddMinutes(1)));
                    response.AppendHeader("Cache-Control", "public");

                     long length = stream.Length;
                     if (length > 0L)
                     {
                         //byte[] buffer = new byte[(int)length];
                         //int count = stream.Read(buffer, 0, (int)length);
                         //response.BinaryWrite(buffer);
                         //response.Flush();


                         response.AppendHeader("Content-encoding", "gzip");
                         using (GZipStream zipStream = new GZipStream(response.OutputStream, CompressionMode.Compress))
                         {
                             byte[] buffer = new byte[(int)length];
                             int count = stream.Read(buffer, 0, (int)length);
                             //                             response.BinaryWrite(buffer);
                             zipStream.Write(buffer, 0, (int)length);
                             response.Flush();
                         }
                     }
                }

            }
            else
            {

                FileInfo info = new FileInfo(physicalPath);
                lastModified = new DateTime(info.LastWriteTime.Year, info.LastWriteTime.Month,
                    info.LastWriteTime.Day, info.LastWriteTime.Hour, info.LastWriteTime.Minute, info.LastWriteTime.Second, 0);
                
                if (lastModified > now)
                {
                    lastModified = new DateTime(now.Ticks - (now.Ticks % 0x989680L));
                }

                DateTime dt;
                if (DateTime.TryParse(request.Headers["If-Modified-Since"], out dt))
                {
                    if ((lastModified - dt).TotalSeconds < 20.0)
                    {
                        response.StatusCode = 304;
                        response.End();
                        return;
                    }
                }
                
                try
                {
                    //response.TransmitFile(physicalPath);

                    Stream stream = File.OpenRead(physicalPath);
                    long length = stream.Length;

                    response.AppendHeader("Content-encoding", "gzip");
                    using (GZipStream zipStream = new GZipStream(response.OutputStream, CompressionMode.Compress))
                    {
                        byte[] buffer = new byte[(int)length];
                        int count = stream.Read(buffer, 0, (int)length);
                        //                             response.BinaryWrite(buffer);
                        zipStream.Write(buffer, 0, (int)length);
                    }

                    stream.Close();

                    //response.Flush();
                }
                catch (Exception)
                {
                    throw new HttpException(403, "Forbidden.");
                }

                string etag = GenerateETag(context, lastModified, now);

                response.AppendHeader("Last-Modified", FormatHttpDateTime(lastModified));
                response.AppendHeader("ETag", etag);
                response.AppendHeader("Expires", FormatHttpDateTime(DateTime.Now.AddMinutes(5)));
                response.AppendHeader("Cache-Control", "public");
            }

        }

        private static string FormatHttpDateTime(DateTime dt)
        {
            if ((dt < DateTime.MaxValue.AddDays(-1.0)) && (dt > DateTime.MinValue.AddDays(1.0)))
            {
                dt = dt.ToUniversalTime();
            }
            return dt.ToString("R", DateTimeFormatInfo.InvariantInfo);
        }

        private static string GenerateETag(HttpContext context, DateTime lastModified, DateTime now)
        {
            long num = lastModified.ToFileTime();
            long num2 = now.ToFileTime();
            string str = num.ToString("X8", CultureInfo.InvariantCulture);
            if ((num2 - num) <= 0x1c9c380L)
            {
                return ("W/\"" + str + "\"");
            }
            return ("\"" + str + "\"");
        }
    }
}
