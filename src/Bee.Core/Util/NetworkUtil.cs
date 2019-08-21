using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Runtime.InteropServices;
using System.Net.Mail;
using System.Configuration;
using System.Net.Configuration;
using Bee.Logging;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Bee.Net;
using System.Net.Sockets;
using System.Threading;

namespace Bee.Util
{
    /// <summary>
    /// The Util for Network.
    /// </summary>
    public static class NetworkUtil
    {
        private static readonly Regex emailValidRegex = new Regex(@"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");


        private static SmtpClient smtp = new SmtpClient();

        public static IPAddress[] GetLocalIP()
        {
            return (from item in Dns.GetHostAddresses(Dns.GetHostName())
                    where !item.IsIPv6SiteLocal && !item.IsIPv6LinkLocal
                    select item).ToArray();
        }

        public static int GetAvailablePort(int port)
        {
            int result = port;
            if (UsedPortList.Contains(port))
            {
                result = Enumerable.Range(8000, 50000).Except<int>(UsedPortList).First<int>();
            }

            return result;
        }


        public static List<int> UsedPortList
        {
            get
            {
                List<int> result = new List<int>();

                IPGlobalProperties iPGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

                List<int> tempList = iPGlobalProperties.GetActiveTcpConnections().Select(tc => tc.LocalEndPoint.Port).ToList();
                result.AddRange(tempList);

                tempList = iPGlobalProperties.GetActiveTcpListeners().Select(ip => ip.Port).ToList();
                result.AddRange(tempList);

                tempList = iPGlobalProperties.GetActiveUdpListeners().Select(ip => ip.Port).ToList();
                result.AddRange(tempList);

                result = result.Distinct().ToList();

                return result;
            }
        }

        public static bool IsPublicIPAddress(string ip)
        {
            if (ip.StartsWith("10."))
            {
                return false;
            }
            if (ip.StartsWith("172.") && (ip.Substring(6, 1) == "."))
            {
                int num = int.Parse(ip.Substring(4, 2));
                if ((0x10 <= num) && (num <= 0x1f))
                {
                    return false;
                }
            }
            if (ip.StartsWith("192.168."))
            {
                return false;
            }
            return true;
        }

        public static bool IsConnectedToInternet()
        {
            int description = 0;
            return InternetGetConnectedState(description, 0);
        }

        public static string GetLocalPublicIP()
        {
            IPAddress[] localIp = GetLocalIP();
            foreach (IPAddress address in localIp)
            {
                if (IsPublicIPAddress(address.ToString()))
                {
                    return address.ToString();
                }
            }
            return null;
        }

        public static bool SendMail(string toAddr, string subject, string content)
        {
            bool result = true;
            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.To.Add(toAddr);
                mailMessage.Subject = subject;
                mailMessage.Body = content;
                mailMessage.IsBodyHtml = true;

                smtp.Send(mailMessage);
            }
            catch (Exception e)
            {
                Logger.Error("Send Mail Error!", e);
                result = false;
            }

            return result;
        }


        [DllImport("wininet.dll")]
        private static extern bool InternetGetConnectedState(int Description, int ReservedValue);

        public static List<PingDetail> Ping(string host, int num)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            string data = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            List<PingDetail> list = new List<PingDetail>();

            for (int i = 0; i < num; i++)
            {
                try
                {
                    PingReply reply = pingSender.Send(host, timeout, buffer, options);

                    if (reply.Status == IPStatus.Success)
                    {
                        PingDetail details = new PingDetail();
                        details.Address = reply.Address.ToString();
                        details.Length = reply.Buffer.Length.ToString();
                        details.Time = reply.RoundtripTime.ToString();
                        details.TTL = reply.Options.Ttl.ToString();

                        list.Add(details);
                    }
                }
                catch
                {
                    break;
                }
            }

            return list;
        }

        public static EmailAddressStatus CheckMailAddressValid(string emailAddress, IPAddress dnsServerAddress, out string message)
        {
            EmailAddressStatus result = EmailAddressStatus.Unkown;
            message = string.Empty;

            if (emailValidRegex.IsMatch(emailAddress))
            {
                MXRecord[] records = DNSResolver.MXLookup(emailAddress.Substring(emailAddress.IndexOf("@") + 1), dnsServerAddress);

                if (records != null && records.Length > 0)
                {
                    foreach (MXRecord record in records)
                    {
                        Socket socket = null;
                        try
                        {
                            IPEndPoint remoteEP = new IPEndPoint(Dns.GetHostEntry(record.DomainName).AddressList[0], 25);
                            socket = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            socket.Connect(remoteEP);

                            message = Get_Response(socket);
                            if (GetResponseCode(message) == 220)
                            {
                                SendData(socket, "HELO {0}\r\n".FormatWith(record.DomainName));
                                message = Get_Response(socket);
                                if (GetResponseCode(message) == 250)
                                {
                                    SendData(socket, "MAIL FROM:<{0}>\r\n".FormatWith("test@163.com"));
                                    message = Get_Response(socket);
                                    if (GetResponseCode(message) == 250)
                                    {
                                        SendData(socket, "RCPT TO:<{0}>\r\n".FormatWith(emailAddress));
                                        message = Get_Response(socket);
                                        if (GetResponseCode(message) == 250)
                                        {
                                            return EmailAddressStatus.Valid;
                                        }
                                        if(message.IndexOf("denied") > 0)
                                        {
                                            SendData(socket, "QUIT\r\n");
                                            socket.Close();
                                            return EmailAddressStatus.DenyAccess;
                                        }
                                        return EmailAddressStatus.AccountNotExists;
                                    }
                                }
                            }
                        }
                        catch (TimeoutException)
                        {
                            result = EmailAddressStatus.Timeout;
                        }
                        catch (Exception e)
                        {
                            message = e.Message;
                        }
                        finally
                        {
                            if (socket != null && socket.Connected)
                            {
                                socket.Close();
                            }
                        }
                    }
                }
                else
                {
                    result = EmailAddressStatus.InvalidDomain;
                }
            }
            else
            {
                result = EmailAddressStatus.InvalidEmailFormat;
            }

            return result;
        }

        private static void SendData(Socket s, string msg)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(msg);
            s.Send(bytes, 0, bytes.Length, SocketFlags.None);
        }

        private static int GetResponseCode(Socket s)
        {
            byte[] buffer = new byte[0x400];
            int num = 0;
            while (s.Available == 0)
            {
                Thread.Sleep(100);
                num++;
                if (num > 30)
                {
                    s.Close();
                    throw new TimeoutException();
                }
            }
            s.Receive(buffer, 0, s.Available, SocketFlags.None);
            return Convert.ToInt32(Encoding.ASCII.GetString(buffer).Substring(0, 3));
        }

        private static int GetResponseCode(string message)
        {
            int result = 0;

            if (!string.IsNullOrEmpty(message) && message.Length >= 3)
            {
                int.TryParse(message.Substring(0, 3), out result);
            }

            return result; 
        }

        private static string Get_Response(Socket s)
        {
            byte[] buffer = new byte[0x400];
            int num = 0;
            while (s.Available == 0)
            {
                Thread.Sleep(100);
                num++;
                if (num > 50)
                {
                    s.Close();
                    throw new TimeoutException();
                }
            }
            s.Receive(buffer, 0, s.Available, SocketFlags.None);
            string str = Encoding.ASCII.GetString(buffer);
            if (str.IndexOf(Environment.NewLine) > 0)
            {
                return str.Substring(0, str.IndexOf(Environment.NewLine));
            }
            return str;
        }
    }

    public enum EmailAddressStatus
    {
        Valid,
        InvalidEmailFormat,
        InvalidDomain,
        AccountNotExists,
        Timeout,
        NoResponse,
        Unkown,
        DenyAccess
    }

    public class PingDetail
    {
        public string Address
        {
            set;
            get;
        }

        public string Length
        {
            set;
            get;
        }

        public string Time
        {
            set;
            get;
        }

        public string TTL
        {
            set;
            get;
        }
    }
}
