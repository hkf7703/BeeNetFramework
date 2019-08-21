using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bee.Net.WebSocket
{
    internal class WebSocketHandlerFactory
    {
        public static IWebSocketHandler BuildHandler(WebSocketHttpRequest request, Action<string> onMessage, Action onClose, Action<byte[]> onBinary)
        {
            var version = GetVersion(request);

            switch (version)
            {
                case "76":
                    return Draft76Handler.Create(request, onMessage);
                case "7":
                case "8":
                case "13":
                    return Hybi13Handler.Create(request, onMessage, onClose, onBinary);
            }

            throw new WebSocketException(WebSocketStatusCodes.UnsupportedDataType);
        }

        public static string GetVersion(WebSocketHttpRequest request)
        {
            string version;
            if (request.Headers.TryGetValue("Sec-WebSocket-Version", out version))
                return version;

            if (request.Headers.TryGetValue("Sec-WebSocket-Draft", out version))
                return version;

            if (request.Headers.ContainsKey("Sec-WebSocket-Key1"))
                return "76";

            return "75";
        }
    }

    public class WebSocketException : Exception
    {
        public WebSocketException(ushort statusCode)
            : base()
        {
            StatusCode = statusCode;
        }

        public WebSocketException(ushort statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public WebSocketException(ushort statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public ushort StatusCode { get; private set; }
    }

    internal static class WebSocketStatusCodes
    {
        public const ushort NormalClosure = 1000;
        public const ushort GoingAway = 1001;
        public const ushort ProtocolError = 1002;
        public const ushort UnsupportedDataType = 1003;
        public const ushort NoStatusReceived = 1005;
        public const ushort AbnormalClosure = 1006;
        public const ushort InvalidFramePayloadData = 1007;
        public const ushort PolicyViolation = 1008;
        public const ushort MessageTooBig = 1009;
        public const ushort MandatoryExt = 1010;
        public const ushort InternalServerError = 1011;
        public const ushort TLSHandshake = 1015;

        public const ushort ApplicationError = 3000;

        public static ushort[] ValidCloseCodes = new[]{
            NormalClosure, GoingAway, ProtocolError, UnsupportedDataType,
            InvalidFramePayloadData, PolicyViolation, MessageTooBig,
            MandatoryExt, InternalServerError
        };
    }

    internal class ReadState
    {
        public ReadState()
        {
            Data = new List<byte>();
        }
        public List<byte> Data { get; private set; }
        public FrameType? FrameType { get; set; }
        public void Clear()
        {
            Data.Clear();
            FrameType = null;
        }
    }

    public enum FrameType : byte
    {
        Continuation,
        Text,
        Binary,
        Close = 8,
        Ping = 9,
        Pong = 10,
    }

    internal static class IntExtensions
    {
        public static byte[] ToBigEndianBytes<T>(this int source)
        {
            byte[] bytes;

            var type = typeof(T);
            if (type == typeof(ushort))
                bytes = BitConverter.GetBytes((ushort)source);
            else if (type == typeof(ulong))
                bytes = BitConverter.GetBytes((ulong)source);
            else if (type == typeof(int))
                bytes = BitConverter.GetBytes(source);
            else
                throw new InvalidCastException("Cannot be cast to T");

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        public static int ToLittleEndianInt(this byte[] source)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(source);

            if (source.Length == 2)
                return BitConverter.ToUInt16(source, 0);

            if (source.Length == 8)
                return (int)BitConverter.ToUInt64(source, 0);

            throw new ArgumentException("Unsupported Size");
        }
    }

    public class SubProtocolNegotiationFailureException : Exception
    {
        public SubProtocolNegotiationFailureException() : base() { }

        public SubProtocolNegotiationFailureException(string message) : base(message) { }

        public SubProtocolNegotiationFailureException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal static class SubProtocolNegotiator
    {
        public static string Negotiate(IEnumerable<string> server, IEnumerable<string> client)
        {
            if (!server.Any() || !client.Any())
            {
                return null;
            }

            var matches = client.Intersect(server);
            if (!matches.Any())
            {
                throw new SubProtocolNegotiationFailureException("Unable to negotiate a subprotocol");
            }
            return matches.First();
        }
    }

    internal interface IWebSocketConnectionInfo
    {
        string SubProtocol { get; }
        string Origin { get; }
        string Host { get; }
        string Path { get; }
        string ClientIpAddress { get; }
        int ClientPort { get; }
        IDictionary<string, string> Cookies { get; }
        Guid Id { get; }
        string NegotiatedSubProtocol { get; }
    }

    internal class WebSocketConnectionInfo : IWebSocketConnectionInfo
    {
        const string CookiePattern = @"((;\s)*(?<cookie_name>[^=]+)=(?<cookie_value>[^\;]+))+";
        private static readonly Regex CookieRegex = new Regex(CookiePattern, RegexOptions.Compiled);

        public static WebSocketConnectionInfo Create(WebSocketHttpRequest request, string clientIp, int clientPort, string negotiatedSubprotocol)
        {
            var info = new WebSocketConnectionInfo
            {
                Origin = request["Origin"] ?? request["Sec-WebSocket-Origin"],
                Host = request["Host"],
                SubProtocol = request["Sec-WebSocket-Protocol"],
                Path = request.Path,
                ClientIpAddress = clientIp,
                ClientPort = clientPort,
                NegotiatedSubProtocol = negotiatedSubprotocol
            };
            var cookieHeader = request["Cookie"];

            if (cookieHeader != null)
            {
                var match = CookieRegex.Match(cookieHeader);
                var fields = match.Groups["cookie_name"].Captures;
                var values = match.Groups["cookie_value"].Captures;
                for (var i = 0; i < fields.Count; i++)
                {
                    var name = fields[i].ToString();
                    var value = values[i].ToString();
                    info.Cookies[name] = value;
                }
            }

            return info;
        }


        WebSocketConnectionInfo()
        {
            Cookies = new Dictionary<string, string>();
            Id = Guid.NewGuid();
        }

        public string NegotiatedSubProtocol { get; private set; }
        public string SubProtocol { get; private set; }
        public string Origin { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }
        public string ClientIpAddress { get; set; }
        public int ClientPort { get; set; }
        public Guid Id { get; set; }

        public IDictionary<string, string> Cookies { get; private set; }
    }
}
