/*
 * 创建人：hukaifeng (hkf7703@163.com)

 * 最后更新：2015/6/16 10:30:02 
 * 功能说明： DNSResolver类
 * 
 
 * 主要类、属性，成员及其功能
    1. 
 * 历史修改记录：
	1 hukaifeng, 2015/6/16 10:30:02 ,  1.0.0.0, create   
	2 

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections;
using System.Text.RegularExpressions;
using Bee.Core;
using System.Net.Sockets;
using Bee.Caching;

namespace Bee.Net
{
    /// <summary>
    /// Logical representation of a pointer, but in fact a byte array reference and a position in it. This
    /// is used to read logical units (bytes, shorts, integers, domain names etc.) from a byte array, keeping
    /// the pointer updated and pointing to the next record. This type of Pointer can be considered the logical
    /// equivalent of an (unsigned char*) in C++
    /// </summary>
    internal class Pointer
    {
        // a pointer is a reference to the message and an index
        private byte[] _message;
        private int _position;

        // pointers can only be created by passing on an existing message
        public Pointer(byte[] message, int position)
        {
            _message = message;
            _position = position;
        }

        /// <summary>
        /// Shallow copy function
        /// </summary>
        /// <returns></returns>
        public Pointer Copy()
        {
            return new Pointer(_message, _position);
        }

        /// <summary>
        /// Adjust the pointers position within the message
        /// </summary>
        /// <param name="position">new position in the message</param>
        public void SetPosition(int position)
        {
            _position = position;
        }

        /// <summary>
        /// Overloads the + operator to allow advancing the pointer by so many bytes
        /// </summary>
        /// <param name="pointer">the initial pointer</param>
        /// <param name="offset">the offset to add to the pointer in bytes</param>
        /// <returns>a reference to a new pointer moved forward by offset bytes</returns>
        public static Pointer operator +(Pointer pointer, int offset)
        {
            return new Pointer(pointer._message, pointer._position + offset);
        }

        /// <summary>
        /// Reads a single byte at the current pointer, does not advance pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public byte Peek()
        {
            return _message[_position];
        }

        /// <summary>
        /// Reads a single byte at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public byte ReadByte()
        {
            return _message[_position++];
        }

        /// <summary>
        /// Reads two bytes to form a short at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public short ReadShort()
        {
            return (short)(ReadByte() << 8 | ReadByte());
        }

        /// <summary>
        /// Reads four bytes to form a int at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public int ReadInt()
        {
            return (ushort)ReadShort() << 16 | (ushort)ReadShort();
        }

        /// <summary>
        /// Reads a single byte as a char at the current pointer, advancing pointer
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public char ReadChar()
        {
            return (char)ReadByte();
        }

        /// <summary>
        /// Reads a domain name from the byte array. The method by which this works is described
        /// in RFC1035 - 4.1.4. Essentially to minimise the size of the message, if part of a domain
        /// name already been seen in the message, rather than repeating it, a pointer to the existing
        /// definition is used. Each word in a domain name is a label, and is preceded by its length
        /// 
        /// eg. bigdevelopments.co.uk
        /// 
        /// is [15] (size of bigdevelopments) + "bigdevelopments"
        ///    [2]  "co"
        ///    [2]  "uk"
        ///    [1]  0 (NULL)
        /// </summary>
        /// <returns>the byte at the pointer</returns>
        public string ReadDomain()
        {
            StringBuilder domain = new StringBuilder();
            int length = 0;

            // get  the length of the first label
            while ((length = ReadByte()) != 0)
            {
                // top 2 bits set denotes domain name compression and to reference elsewhere
                if ((length & 0xc0) == 0xc0)
                {
                    // work out the existing domain name, copy this pointer
                    Pointer newPointer = Copy();

                    // and move it to where specified here
                    newPointer.SetPosition((length & 0x3f) << 8 | ReadByte());

                    // repeat call recursively
                    domain.Append(newPointer.ReadDomain());
                    return domain.ToString();
                }

                // if not using compression, copy a char at a time to the domain name
                while (length > 0)
                {
                    domain.Append(ReadChar());
                    length--;
                }

                // if size of next label isn't null (end of domain name) add a period ready for next label
                if (Peek() != 0) domain.Append('.');
            }

            // and return
            return domain.ToString();
        }
    }

    /// <summary>
	/// A simple base class for the different ResourceRecords, ANAME, MX, SOA, NS etc.
	/// </summary>
	public abstract class RecordBase
	{
		// no implementation
	}

    /// <summary>
	/// ANAME Resource Record (RR) (RFC1035 3.4.1)
	/// </summary>
	public class ANameRecord : RecordBase
	{
		// An ANAME records consists simply of an IP address
		internal IPAddress _ipAddress;

		// expose this IP address r/o to the world
		public IPAddress IPAddress
		{
			get { return _ipAddress; }
		}

		/// <summary>
		/// Constructs an ANAME record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal ANameRecord(Pointer pointer)
		{
			byte b1 = pointer.ReadByte();
			byte b2 = pointer.ReadByte();
			byte b3 = pointer.ReadByte();
			byte b4 = pointer.ReadByte();

			// this next line's not brilliant - couldn't find a better way though
			_ipAddress = IPAddress.Parse(string.Format("{0}.{1}.{2}.{3}", b1, b2, b3, b4));
		}

		public override string ToString()
		{
			return _ipAddress.ToString();
		}
	}
    /// <summary>
	/// An MX (Mail Exchanger) Resource Record (RR) (RFC1035 3.3.9)
	/// </summary>
	[Serializable]
	public class MXRecord : RecordBase, IComparable
	{
		// an MX record is a domain name and an integer preference
		private readonly string		_domainName;
		private readonly int		_preference;

		// expose these fields public read/only
		public string DomainName	{ get { return _domainName; }}
		public int Preference		{ get { return _preference; }}
				
		/// <summary>
		/// Constructs an MX record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal MXRecord(Pointer pointer)
		{
			_preference = pointer.ReadShort();
			_domainName = pointer.ReadDomain();
		}

		public override string ToString()
		{
			return string.Format("Mail Server = {0}, Preference = {1}", _domainName, _preference.ToString());
		}

		#region IComparable Members

		/// <summary>
		/// Implements the IComparable interface so that we can sort the MX records by their
		/// lowest preference
		/// </summary>
		/// <param name="other">the other MxRecord to compare against</param>
		/// <returns>1, 0, -1</returns>
		public int CompareTo(object obj)
		{
			MXRecord mxOther = (MXRecord)obj;

			// we want to be able to sort them by preference
			if (mxOther._preference < _preference) return 1;
			if (mxOther._preference > _preference) return -1;
			
			// order mail servers of same preference by name
			return -mxOther._domainName.CompareTo(_domainName);
		}

		public static bool operator==(MXRecord record1, MXRecord record2)
		{
			if (record1 == null) throw new ArgumentNullException("record1");

			return record1.Equals(record2);
		}
	
		public static bool operator!=(MXRecord record1, MXRecord record2)
		{
			return !(record1 == record2);
		}
/*
		public static bool operator<(MXRecord record1, MXRecord record2)
		{
			if (record1._preference > record2._preference) return false;
			if (record1._domainName > record2._domainName) return false;
			return false;
		}

		public static bool operator>(MXRecord record1, MXRecord record2)
		{
			if (record1._preference < record2._preference) return false;
			if (record1._domainName < record2._domainName) return false;
			return false;
		}
*/

		public override bool Equals(object obj)
		{
			// this object isn't null
			if (obj == null) return false;

			// must be of same type
			if (this.GetType() != obj.GetType()) return false;

			MXRecord mxOther = (MXRecord)obj;

			// preference must match
			if (mxOther._preference != _preference) return false;
			
			// and so must the domain name
			if (mxOther._domainName != _domainName) return false;

			// its a match
			return true;
		}

		public override int GetHashCode()
		{
			return _preference;
		}

		#endregion
	}

    /// <summary>
	/// A Name Server Resource Record (RR) (RFC1035 3.3.11)
	/// </summary>
	public class NSRecord : RecordBase
	{
		// the fields exposed outside the assembly
		private readonly string		_domainName;

		// expose this domain name address r/o to the world
		public string DomainName	{ get { return _domainName; }}
				
		/// <summary>
		/// Constructs a NS record by reading bytes from a return message
		/// </summary>
		/// <param name="pointer">A logical pointer to the bytes holding the record</param>
		internal NSRecord(Pointer pointer)
		{
			_domainName = pointer.ReadDomain();
		}

		public override string ToString()
		{
			return _domainName;
		}
	}

    /// <summary>
    /// An SOA Resource Record (RR) (RFC1035 3.3.13)
    /// </summary>
    public class SoaRecord : RecordBase
    {
        // these fields constitute an SOA RR
        private readonly string _primaryNameServer;
        private readonly string _responsibleMailAddress;
        private readonly int _serial;
        private readonly int _refresh;
        private readonly int _retry;
        private readonly int _expire;
        private readonly int _defaultTtl;

        // expose these fields public read/only
        public string PrimaryNameServer { get { return _primaryNameServer; } }
        public string ResponsibleMailAddress { get { return _responsibleMailAddress; } }
        public int Serial { get { return _serial; } }
        public int Refresh { get { return _refresh; } }
        public int Retry { get { return _retry; } }
        public int Expire { get { return _expire; } }
        public int DefaultTtl { get { return _defaultTtl; } }

        /// <summary>
        /// Constructs an SOA record by reading bytes from a return message
        /// </summary>
        /// <param name="pointer">A logical pointer to the bytes holding the record</param>
        internal SoaRecord(Pointer pointer)
        {
            // read all fields RFC1035 3.3.13
            _primaryNameServer = pointer.ReadDomain();
            _responsibleMailAddress = pointer.ReadDomain();
            _serial = pointer.ReadInt();
            _refresh = pointer.ReadInt();
            _retry = pointer.ReadInt();
            _expire = pointer.ReadInt();
            _defaultTtl = pointer.ReadInt();
        }

        public override string ToString()
        {
            return string.Format("primary name server = {0}\nresponsible mail addr = {1}\nserial  = {2}\nrefresh = {3}\nretry   = {4}\nexpire  = {5}\ndefault TTL = {6}",
                _primaryNameServer,
                _responsibleMailAddress,
                _serial.ToString(),
                _refresh.ToString(),
                _retry.ToString(),
                _expire.ToString(),
                _defaultTtl.ToString());
        }
    }

    /// <summary>
	/// Represents a Resource Record as detailed in RFC1035 4.1.3
	/// </summary>
	[Serializable]
	public class ResourceRecord
	{
		// private, constructor initialised fields
		private readonly string		_domain;
		private readonly DnsType	_dnsType;
		private readonly DnsClass	_dnsClass;
		private readonly int		_Ttl;
		private readonly RecordBase	_record;

		// read only properties applicable for all records
		public string		Domain		{ get { return _domain;		}}
		public DnsType		Type		{ get { return _dnsType;	}}
		public DnsClass		Class		{ get { return _dnsClass;	}}
		public int			Ttl			{ get { return _Ttl;		}}
		public RecordBase	Record		{ get { return _record;		}}

		/// <summary>
		/// Construct a resource record from a pointer to a byte array
		/// </summary>
		/// <param name="pointer">the position in the byte array of the record</param>
		internal ResourceRecord(Pointer pointer)
		{
			// extract the domain, question type, question class and Ttl
			_domain = pointer.ReadDomain();
			_dnsType = (DnsType)pointer.ReadShort();
			_dnsClass = (DnsClass)pointer.ReadShort();
			_Ttl = pointer.ReadInt();

			// the next short is the record length, we only use it for unrecognised record types
			int recordLength = pointer.ReadShort();

			// and create the appropriate RDATA record based on the dnsType
			switch (_dnsType)
			{
				case DnsType.NS:	_record = new NSRecord(pointer);	break;
				case DnsType.MX:	_record = new MXRecord(pointer);	break;
				case DnsType.ANAME:	_record = new ANameRecord(pointer);	break;
				case DnsType.SOA:	_record = new SoaRecord(pointer);	break;
				default:
				{
					// move the pointer over this unrecognised record
					pointer += recordLength;
					break;
				}
			}
		}
	}

	// Answers, Name Servers and Additional Records all share the same RR format
	[Serializable]
	public class Answer : ResourceRecord
	{
		internal Answer(Pointer pointer) : base(pointer) {}
	}

	[Serializable]
	public class NameServer : ResourceRecord
	{
		internal NameServer(Pointer pointer) : base(pointer) {}
	}

	[Serializable]
	public class AdditionalRecord : ResourceRecord
	{
		internal AdditionalRecord(Pointer pointer) : base(pointer) {}
	}

    /// <summary>
	/// The DNS TYPE (RFC1035 3.2.2/3) - 4 types are currently supported. Also, I know that this
	/// enumeration goes against naming guidelines, but I have done this as an ANAME is most
	/// definetely an 'ANAME' and not an 'Aname'
	/// </summary>
	public enum DnsType
	{
		None = 0, ANAME = 1, NS = 2, SOA = 6, MX = 15
	}

	/// <summary>
	/// The DNS CLASS (RFC1035 3.2.4/5)
	/// Internet will be the one we'll be using (IN), the others are for completeness
	/// </summary>
	public enum DnsClass
	{
		None = 0, IN = 1, CS = 2, CH = 3, HS = 4
	}

	/// <summary>
	/// (RFC1035 4.1.1) These are the return codes the server can send back
	/// </summary>
	public enum ReturnCode
	{
		Success = 0,
		FormatError = 1,
		ServerFailure = 2,
		NameError = 3,
		NotImplemented = 4,
		Refused = 5,
		Other = 6
	}

	/// <summary>
	/// (RFC1035 4.1.1) These are the Query Types which apply to all questions in a request
	/// </summary>
	public enum Opcode
	{
		StandardQuery = 0,
		InverseQuerty = 1,
		StatusRequest = 2,
		Reserverd3 = 3,
		Reserverd4 = 4,
		Reserverd5 = 5,
		Reserverd6 = 6,
		Reserverd7 = 7,
		Reserverd8 = 8,
		Reserverd9 = 9,
		Reserverd10 = 10,
		Reserverd11 = 11,
		Reserverd12 = 12,
		Reserverd13 = 13,
		Reserverd14 = 14,
		Reserverd15 = 15,
	}

    /// <summary>
    /// A Request logically consists of a number of questions to ask the DNS Server. Create a request and
    /// add questions to it, then pass the request to Resolver.Lookup to query the DNS Server. It is important
    /// to note that many DNS Servers DO NOT SUPPORT MORE THAN 1 QUESTION PER REQUEST, and it is advised that
    /// you only add one question to a request. If not ensure you check Response.ReturnCode to see what the
    /// server has to say about it.
    /// </summary>
    public class Request
    {
        // A request is a series of questions, an 'opcode' (RFC1035 4.1.1) and a flag to denote
        // whether recursion is required (don't ask..., just assume it is)
        private ArrayList _questions;
        private bool _recursionDesired;
        private Opcode _opCode;

        public bool RecursionDesired
        {
            get { return _recursionDesired; }
            set { _recursionDesired = value; }
        }

        public Opcode Opcode
        {
            get { return _opCode; }
            set { _opCode = value; }
        }

        /// <summary>
        /// Construct this object with the default values and create an ArrayList to hold
        /// the questions as they are added
        /// </summary>
        public Request()
        {
            // default for a request is that recursion is desired and using standard query
            _recursionDesired = true;
            _opCode = Opcode.StandardQuery;

            // create an expandable list of questions
            _questions = new ArrayList();

        }

        /// <summary>
        /// Adds a question to the request to be sent to the DNS server.
        /// </summary>
        /// <param name="question">The question to add to the request</param>
        public void AddQuestion(Question question)
        {
            // abandon if null
            if (question == null) throw new ArgumentNullException("question");

            // add this question to our collection
            _questions.Add(question);
        }

        /// <summary>
        /// Convert this request into a byte array ready to send direct to the DNS server
        /// </summary>
        /// <returns></returns>
        public byte[] GetMessage()
        {
            // construct a message for this request. This will be a byte array but we're using
            // an arraylist as we don't know how big it will be
            ArrayList data = new ArrayList();

            // the id of this message - this will be filled in by the resolver
            data.Add((byte)0);
            data.Add((byte)0);

            // write the bitfields
            data.Add((byte)(((byte)_opCode << 3) | (_recursionDesired ? 0x01 : 0)));
            data.Add((byte)0);

            // tell it how many questions
            unchecked
            {
                data.Add((byte)(_questions.Count >> 8));
                data.Add((byte)_questions.Count);
            }

            // the are no requests, name servers or additional records in a request
            data.Add((byte)0); data.Add((byte)0);
            data.Add((byte)0); data.Add((byte)0);
            data.Add((byte)0); data.Add((byte)0);

            // that's the header done - now add the questions
            foreach (Question question in _questions)
            {
                AddDomain(data, question.Domain);
                unchecked
                {
                    data.Add((byte)0);
                    data.Add((byte)question.Type);
                    data.Add((byte)0);
                    data.Add((byte)question.Class);
                }
            }

            // and convert that to an array
            byte[] message = new byte[data.Count];
            data.CopyTo(message);
            return message;
        }

        /// <summary>
        /// Adds a domain name to the ArrayList of bytes. This implementation does not use
        /// the domain name compression used in the class Pointer - maybe it should.
        /// </summary>
        /// <param name="data">The ArrayList representing the byte array message</param>
        /// <param name="domainName">the domain name to encode and add to the array</param>
        private static void AddDomain(ArrayList data, string domainName)
        {
            int position = 0;
            int length = 0;

            // start from the beginning and go to the end
            while (position < domainName.Length)
            {
                // look for a period, after where we are
                length = domainName.IndexOf('.', position) - position;

                // if there isn't one then this labels length is to the end of the string
                if (length < 0) length = domainName.Length - position;

                // add the length
                data.Add((byte)length);

                // copy a char at a time to the array
                while (length-- > 0)
                {
                    data.Add((byte)domainName[position++]);
                }

                // step over '.'
                position++;
            }

            // end of domain names
            data.Add((byte)0);
        }
    }

    /// <summary>
    /// A Response is a logical representation of the byte data returned from a DNS query
    /// </summary>
    public class Response
    {
        // these are fields we're interested in from the message
        private readonly ReturnCode _returnCode;
        private readonly bool _authoritativeAnswer;
        private readonly bool _recursionAvailable;
        private readonly bool _truncated;
        private readonly Question[] _questions;
        private readonly Answer[] _answers;
        private readonly NameServer[] _nameServers;
        private readonly AdditionalRecord[] _additionalRecords;

        // these fields are readonly outside the assembly - use r/o properties
        public ReturnCode ReturnCode { get { return _returnCode; } }
        public bool AuthoritativeAnswer { get { return _authoritativeAnswer; } }
        public bool RecursionAvailable { get { return _recursionAvailable; } }
        public bool MessageTruncated { get { return _truncated; } }
        public Question[] Questions { get { return _questions; } }
        public Answer[] Answers { get { return _answers; } }
        public NameServer[] NameServers { get { return _nameServers; } }
        public AdditionalRecord[] AdditionalRecords { get { return _additionalRecords; } }

        /// <summary>
        /// Construct a Response object from the supplied byte array
        /// </summary>
        /// <param name="message">a byte array returned from a DNS server query</param>
        internal Response(byte[] message)
        {
            // the bit flags are in bytes 2 and 3
            byte flags1 = message[2];
            byte flags2 = message[3];

            // get return code from lowest 4 bits of byte 3
            int returnCode = flags2 & 15;

            // if its in the reserved section, set to other
            if (returnCode > 6) returnCode = 6;
            _returnCode = (ReturnCode)returnCode;

            // other bit flags
            _authoritativeAnswer = ((flags1 & 4) != 0);
            _recursionAvailable = ((flags2 & 128) != 0);
            _truncated = ((flags1 & 2) != 0);

            // create the arrays of response objects
            _questions = new Question[GetShort(message, 4)];
            _answers = new Answer[GetShort(message, 6)];
            _nameServers = new NameServer[GetShort(message, 8)];
            _additionalRecords = new AdditionalRecord[GetShort(message, 10)];

            // need a pointer to do this, position just after the header
            Pointer pointer = new Pointer(message, 12);

            // and now populate them, they always follow this order
            for (int index = 0; index < _questions.Length; index++)
            {
                try
                {
                    // try to build a quesion from the response
                    _questions[index] = new Question(pointer);
                }
                catch (Exception ex)
                {
                    // something grim has happened, we can't continue
                    throw new CoreException("Invlaid Response", ex);
                }
            }
            for (int index = 0; index < _answers.Length; index++)
            {
                _answers[index] = new Answer(pointer);
            }
            for (int index = 0; index < _nameServers.Length; index++)
            {
                _nameServers[index] = new NameServer(pointer);
            }
            for (int index = 0; index < _additionalRecords.Length; index++)
            {
                _additionalRecords[index] = new AdditionalRecord(pointer);
            }
        }

        /// <summary>
        /// Convert 2 bytes to a short. It would have been nice to use BitConverter for this,
        /// it however reads the bytes in the wrong order (at least on Windows)
        /// </summary>
        /// <param name="message">byte array to look in</param>
        /// <param name="position">position to look at</param>
        /// <returns>short representation of the two bytes</returns>
        private static short GetShort(byte[] message, int position)
        {
            return (short)(message[position] << 8 | message[position + 1]);
        }
    }

    /// <summary>
    /// Represents a DNS Question, comprising of a domain to query, the type of query (QTYPE) and the class
    /// of query (QCLASS). This class is an encapsulation of these three things, and extensive argument checking
    /// in the constructor as this may well be created outside the assembly (public protection)
    /// </summary>
    [Serializable]
    public class Question
    {
        // A question is these three things combined
        private readonly string _domain;
        private readonly DnsType _dnsType;
        private readonly DnsClass _dnsClass;

        // expose them read/only to the world
        public string Domain { get { return _domain; } }
        public DnsType Type { get { return _dnsType; } }
        public DnsClass Class { get { return _dnsClass; } }

        /// <summary>
        /// Construct the question from parameters, checking for safety
        /// </summary>
        /// <param name="domain">the domain name to query eg. bigdevelopments.co.uk</param>
        /// <param name="dnsType">the QTYPE of query eg. DnsType.MX</param>
        /// <param name="dnsClass">the CLASS of query, invariably DnsClass.IN</param>
        public Question(string domain, DnsType dnsType, DnsClass dnsClass)
        {
            // check the input parameters
            if (domain == null) throw new ArgumentNullException("domain");

            // do a sanity check on the domain name to make sure its legal
            if (domain.Length == 0 || domain.Length > 255 || !Regex.IsMatch(domain, @"^[a-z|A-Z|0-9|\-|_]{1,63}(\.[a-z|A-Z|0-9|-|_]{1,63})+$"))
            {
                // domain names can't be bigger tan 255 chars, and individal labels can't be bigger than 63 chars
                throw new ArgumentException("The supplied domain name was not in the correct form", "domain");
            }

            // sanity check the DnsType parameter
            if (!Enum.IsDefined(typeof(DnsType), dnsType) || dnsType == DnsType.None)
            {
                throw new ArgumentOutOfRangeException("dnsType", "Not a valid value");
            }

            // sanity check the DnsClass parameter
            if (!Enum.IsDefined(typeof(DnsClass), dnsClass) || dnsClass == DnsClass.None)
            {
                throw new ArgumentOutOfRangeException("dnsClass", "Not a valid value");
            }

            // just remember the values
            _domain = domain;
            _dnsType = dnsType;
            _dnsClass = dnsClass;
        }

        /// <summary>
        /// Construct the question reading from a DNS Server response. Consult RFC1035 4.1.2
        /// for byte-wise details of this structure in byte array form
        /// </summary>
        /// <param name="pointer">a logical pointer to the Question in byte array form</param>
        internal Question(Pointer pointer)
        {
            // extract from the message
            _domain = pointer.ReadDomain();
            _dnsType = (DnsType)pointer.ReadShort();
            _dnsClass = (DnsClass)pointer.ReadShort();
        }
    }

    public sealed class DNSResolver
    {
        const int _dnsPort = 53;
        const int _udpRetryAttempts = 2;
        static int _uniqueId;

        /// <summary>
		/// Private constructor - this static class should never be instantiated
		/// </summary>
        private DNSResolver()
		{
			// no implementation
		}	

		/// <summary>
		/// Shorthand form to make MX querying easier, essentially wraps up the retreival
		/// of the MX records, and sorts them by preference
		/// </summary>
		/// <param name="domain">domain name to retreive MX RRs for</param>
		/// <param name="dnsServer">the server we're going to ask</param>
		/// <returns>An array of MXRecords</returns>
		public static MXRecord[] MXLookup(string domain, IPAddress dnsServer)
		{
			// check the inputs
			if (domain == null) throw new ArgumentNullException("domain");
			if (dnsServer == null)  throw new ArgumentNullException("dnsServer");

            return CacheManager.Instance.GetEntity<MXRecord[], string>("Bee.DNSCache", domain, TimeSpan.FromHours(12), domainPara =>
                {
                    // create a request for this
                    Request request = new Request();

                    // add one question - the MX IN lookup for the supplied domain
                    request.AddQuestion(new Question(domainPara, DnsType.MX, DnsClass.IN));

                    // fire it off
                    Response response = Lookup(request, dnsServer);

                    // if we didn't get a response, then return null
                    if (response == null) return null;

                    // create a growable array of MX records
                    ArrayList resourceRecords = new ArrayList();

                    // add each of the answers to the array
                    foreach (Answer answer in response.Answers)
                    {
                        // if the answer is an MX record
                        if (answer.Record.GetType() == typeof(MXRecord))
                        {
                            // add it to our array
                            resourceRecords.Add(answer.Record);
                        }
                    }

                    // create array of MX records
                    MXRecord[] mxRecords = new MXRecord[resourceRecords.Count];

                    // copy from the array list
                    resourceRecords.CopyTo(mxRecords);

                    // sort into lowest preference order
                    Array.Sort(mxRecords);

                    // and return
                    return mxRecords;
                });
		}

		/// <summary>
		/// The principal look up function, which sends a request message to the given
		/// DNS server and collects a response. This implementation re-sends the message
		/// via UDP up to two times in the event of no response/packet loss
		/// </summary>
		/// <param name="request">The logical request to send to the server</param>
		/// <param name="dnsServer">The IP address of the DNS server we are querying</param>
		/// <returns>The logical response from the DNS server or null if no response</returns>
		public static Response Lookup(Request request, IPAddress dnsServer)
		{
			// check the inputs
			if (request == null) throw new ArgumentNullException("request");
			if (dnsServer == null) throw new ArgumentNullException("dnsServer");
			
			// We will not catch exceptions here, rather just refer them to the caller

			// create an end point to communicate with
			IPEndPoint server = new IPEndPoint(dnsServer, _dnsPort);
		
			// get the message
			byte[] requestMessage = request.GetMessage();

			// send the request and get the response
			byte[] responseMessage = UdpTransfer(server, requestMessage);

			// and populate a response object from that and return it
			return new Response(responseMessage);
		}

		private static byte[] UdpTransfer(IPEndPoint server, byte[] requestMessage)
		{
			// UDP can fail - if it does try again keeping track of how many attempts we've made
			int attempts = 0;

			// try repeatedly in case of failure
			while (attempts <= _udpRetryAttempts)
			{
				// firstly, uniquely mark this request with an id
				unchecked
				{
					// substitute in an id unique to this lookup, the request has no idea about this
					requestMessage[0] = (byte)(_uniqueId >> 8);
					requestMessage[1] = (byte)_uniqueId;
				}

				// we'll be send and receiving a UDP packet
				Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			
				// we will wait at most 1 second for a dns reply
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000);

				// send it off to the server
				socket.SendTo(requestMessage, requestMessage.Length, SocketFlags.None, server);
		
				// RFC1035 states that the maximum size of a UDP datagram is 512 octets (bytes)
				byte[] responseMessage = new byte[512];

				try
				{
					// wait for a response upto 1 second
					socket.Receive(responseMessage);

					// make sure the message returned is ours
					if (responseMessage[0] == requestMessage[0] && responseMessage[1] == requestMessage[1])
					{
						// its a valid response - return it, this is our successful exit point
						return responseMessage;
					}
				}
				catch (SocketException)
				{
					// failure - we better try again, but remember how many attempts
					attempts++;
				}
				finally
				{
					// increase the unique id
					_uniqueId++;

					// close the socket
					socket.Close();
				}
			}
		
			// the operation has failed, this is our unsuccessful exit point
			throw new CoreException("no response!");
		}
    }
}
