using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data;
using Bee.Core;
using System.Web;

namespace Bee.Util
{
    public static class StringUtil
    {
        private static readonly string AlphaNum = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly string ValidAlphaNum = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnpqrstuvwxyz";
        private static readonly Random random = new Random();

        private static readonly Regex FormatRegex = new Regex(@"@(?<name>[\w\.]+?)[^\w\.]+?");
        private static readonly Regex AbsolutePathRegex = new Regex(@"src=""(?<url>.*?)""");

        public static readonly Regex HexRegex = new Regex(@"^[0-9a-fA-F]+$");
        public static readonly Regex NumberRegex = new Regex(@"[0-9]+$");
        public static readonly Regex EmailRegex = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$", RegexOptions.IgnoreCase);
        public static readonly Regex UrlRegex = new Regex(@"^(https?|s?ftp):\/\/(((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:)*@)?(((\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])\.(\d|[1-9]\d|1\d\d|2[0-4]\d|25[0-5]))|((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?)(:\d*)?)(\/((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)+(\/(([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)*)*)?)?(\?((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|[\uE000-\uF8FF]|\/|\?)*)?(#((([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(%[\da-f]{2})|[!\$&'\(\)\*\+,;=]|:|@)|\/|\?)*)?$", RegexOptions.IgnoreCase);
        /// <summary>
        /// 匹配日期是否合法
        /// </summary>
        public static readonly Regex DateTimeRegex = new Regex(@"^(\d{4}[\/\-](0?[1-9]|1[0-2])[\/\-]((0?[1-9])|((1|2)[0-9])|30|31))|((0?[1-9]|1[0-2])[\/\-]((0?[1-9])|((1|2)[0-9])|30|31)[\/\-]\d{4})$");
        /// <summary>
        /// 检测密码复杂度是否达标：密码中必须包含字母、数字、特称字符，至少8个字符，最多16个字符。
        /// </summary>
        public static readonly Regex PasswordRegex = new Regex(@"^(?=.*\d)(?=.*[a-zA-Z])(?=.*[^a-zA-Z0-9]).{8,16}$");


        private static readonly Dictionary<char, char> CapitalMapping = null;

        private static int[] pyValue = new int[]
                {
                -20319,-20317,-20304,-20295,-20292,-20283,-20265,-20257,-20242,-20230,-20051,-20036,
                -20032,-20026,-20002,-19990,-19986,-19982,-19976,-19805,-19784,-19775,-19774,-19763,
                -19756,-19751,-19746,-19741,-19739,-19728,-19725,-19715,-19540,-19531,-19525,-19515,
                -19500,-19484,-19479,-19467,-19289,-19288,-19281,-19275,-19270,-19263,-19261,-19249,
                -19243,-19242,-19238,-19235,-19227,-19224,-19218,-19212,-19038,-19023,-19018,-19006,
                -19003,-18996,-18977,-18961,-18952,-18783,-18774,-18773,-18763,-18756,-18741,-18735,
                -18731,-18722,-18710,-18697,-18696,-18526,-18518,-18501,-18490,-18478,-18463,-18448,
                -18447,-18446,-18239,-18237,-18231,-18220,-18211,-18201,-18184,-18183, -18181,-18012,
                -17997,-17988,-17970,-17964,-17961,-17950,-17947,-17931,-17928,-17922,-17759,-17752,
                -17733,-17730,-17721,-17703,-17701,-17697,-17692,-17683,-17676,-17496,-17487,-17482,
                -17468,-17454,-17433,-17427,-17417,-17202,-17185,-16983,-16970,-16942,-16915,-16733,
                -16708,-16706,-16689,-16664,-16657,-16647,-16474,-16470,-16465,-16459,-16452,-16448,
                -16433,-16429,-16427,-16423,-16419,-16412,-16407,-16403,-16401,-16393,-16220,-16216,
                -16212,-16205,-16202,-16187,-16180,-16171,-16169,-16158,-16155,-15959,-15958,-15944,
                -15933,-15920,-15915,-15903,-15889,-15878,-15707,-15701,-15681,-15667,-15661,-15659,
                -15652,-15640,-15631,-15625,-15454,-15448,-15436,-15435,-15419,-15416,-15408,-15394,
                -15385,-15377,-15375,-15369,-15363,-15362,-15183,-15180,-15165,-15158,-15153,-15150,
                -15149,-15144,-15143,-15141,-15140,-15139,-15128,-15121,-15119,-15117,-15110,-15109,
                -14941,-14937,-14933,-14930,-14929,-14928,-14926,-14922,-14921,-14914,-14908,-14902,
                -14894,-14889,-14882,-14873,-14871,-14857,-14678,-14674,-14670,-14668,-14663,-14654,
                -14645,-14630,-14594,-14429,-14407,-14399,-14384,-14379,-14368,-14355,-14353,-14345,
                -14170,-14159,-14151,-14149,-14145,-14140,-14137,-14135,-14125,-14123,-14122,-14112,
                -14109,-14099,-14097,-14094,-14092,-14090,-14087,-14083,-13917,-13914,-13910,-13907,
                -13906,-13905,-13896,-13894,-13878,-13870,-13859,-13847,-13831,-13658,-13611,-13601,
                -13406,-13404,-13400,-13398,-13395,-13391,-13387,-13383,-13367,-13359,-13356,-13343,
                -13340,-13329,-13326,-13318,-13147,-13138,-13120,-13107,-13096,-13095,-13091,-13076,
                -13068,-13063,-13060,-12888,-12875,-12871,-12860,-12858,-12852,-12849,-12838,-12831,
                -12829,-12812,-12802,-12607,-12597,-12594,-12585,-12556,-12359,-12346,-12320,-12300,
                -12120,-12099,-12089,-12074,-12067,-12058,-12039,-11867,-11861,-11847,-11831,-11798,
                -11781,-11604,-11589,-11536,-11358,-11340,-11339,-11324,-11303,-11097,-11077,-11067,
                -11055,-11052,-11045,-11041,-11038,-11024,-11020,-11019,-11018,-11014,-10838,-10832,
                -10815,-10800,-10790,-10780,-10764,-10587,-10544,-10533,-10519,-10331,-10329,-10328,
                -10322,-10315,-10309,-10307,-10296,-10281,-10274,-10270,-10262,-10260,-10256,-10254
                };

        private static string[] pyName = new string[]
                {
                "A","Ai","An","Ang","Ao","Ba","Bai","Ban","Bang","Bao","Bei","Ben",
                "Beng","Bi","Bian","Biao","Bie","Bin","Bing","Bo","Bu","Ba","Cai","Can",
                "Cang","Cao","Ce","Ceng","Cha","Chai","Chan","Chang","Chao","Che","Chen","Cheng",
                "Chi","Chong","Chou","Chu","Chuai","Chuan","Chuang","Chui","Chun","Chuo","Ci","Cong",
                "Cou","Cu","Cuan","Cui","Cun","Cuo","Da","Dai","Dan","Dang","Dao","De",
                "Deng","Di","Dian","Diao","Die","Ding","Diu","Dong","Dou","Du","Duan","Dui",
                "Dun","Duo","E","En","Er","Fa","Fan","Fang","Fei","Fen","Feng","Fo",
                "Fou","Fu","Ga","Gai","Gan","Gang","Gao","Ge","Gei","Gen","Geng","Gong",
                "Gou","Gu","Gua","Guai","Guan","Guang","Gui","Gun","Guo","Ha","Hai","Han",
                "Hang","Hao","He","Hei","Hen","Heng","Hong","Hou","Hu","Hua","Huai","Huan",
                "Huang","Hui","Hun","Huo","Ji","Jia","Jian","Jiang","Jiao","Jie","Jin","Jing",
                "Jiong","Jiu","Ju","Juan","Jue","Jun","Ka","Kai","Kan","Kang","Kao","Ke",
                "Ken","Keng","Kong","Kou","Ku","Kua","Kuai","Kuan","Kuang","Kui","Kun","Kuo",
                "La","Lai","Lan","Lang","Lao","Le","Lei","Leng","Li","Lia","Lian","Liang",
                "Liao","Lie","Lin","Ling","Liu","Long","Lou","Lu","Lv","Luan","Lue","Lun",
                "Luo","Ma","Mai","Man","Mang","Mao","Me","Mei","Men","Meng","Mi","Mian",
                "Miao","Mie","Min","Ming","Miu","Mo","Mou","Mu","Na","Nai","Nan","Nang",
                "Nao","Ne","Nei","Nen","Neng","Ni","Nian","Niang","Niao","Nie","Nin","Ning",
                "Niu","Nong","Nu","Nv","Nuan","Nue","Nuo","O","Ou","Pa","Pai","Pan",
                "Pang","Pao","Pei","Pen","Peng","Pi","Pian","Piao","Pie","Pin","Ping","Po",
                "Pu","Qi","Qia","Qian","Qiang","Qiao","Qie","Qin","Qing","Qiong","Qiu","Qu",
                "Quan","Que","Qun","Ran","Rang","Rao","Re","Ren","Reng","Ri","Rong","Rou",
                "Ru","Ruan","Rui","Run","Ruo","Sa","Sai","San","Sang","Sao","Se","Sen",
                "Seng","Sha","Shai","Shan","Shang","Shao","She","Shen","Sheng","Shi","Shou","Shu",
                "Shua","Shuai","Shuan","Shuang","Shui","Shun","Shuo","Si","Song","Sou","Su","Suan",
                "Sui","Sun","Suo","Ta","Tai","Tan","Tang","Tao","Te","Teng","Ti","Tian",
                "Tiao","Tie","Ting","Tong","Tou","Tu","Tuan","Tui","Tun","Tuo","Wa","Wai",
                "Wan","Wang","Wei","Wen","Weng","Wo","Wu","Xi","Xia","Xian","Xiang","Xiao",
                "Xie","Xin","Xing","Xiong","Xiu","Xu","Xuan","Xue","Xun","Ya","Yan","Yang",
                "Yao","Ye","Yi","Yin","Ying","Yo","Yong","You","Yu","Yuan","Yue","Yun",
                "Za", "Zai","Zan","Zang","Zao","Ze","Zei","Zen","Zeng","Zha","Zhai","Zhan",
                "Zhang","Zhao","Zhe","Zhen","Zheng","Zhi","Zhong","Zhou","Zhu","Zhua","Zhuai","Zhuan",
                "Zhuang","Zhui","Zhun","Zhuo","Zi","Zong","Zou","Zu","Zuan","Zui","Zun","Zuo"
                };


        static StringUtil()
        {
            CapitalMapping = new Dictionary<char, char>();

            CapitalMapping.Add('（', '(');
            CapitalMapping.Add('）', ')');
        }

        /// <summary>
        /// Converts the chinese to the full spell.
        /// </summary>
        /// <param name="cnString">the chinese string.</param>
        /// <returns>the full spell of the chinese string.</returns>
        public static string GetFullSpell(string cnString)
        {
            // 匹配中文字符
            Regex regex = new Regex("^[\u4e00-\u9fa5]$");
            byte[] array = new byte[2];
            string pyString = "";
            int chrAsc = 0;
            int i1 = 0;
            int i2 = 0;
            char[] noWChar = cnString.ToCharArray();

            for (int j = 0; j < noWChar.Length; j++)
            {
                // 中文字符
                if (regex.IsMatch(noWChar[j].ToString()))
                {
                    array = System.Text.Encoding.GetEncoding("gb2312").GetBytes(noWChar[j].ToString());
                    i1 = (short)(array[0]);
                    i2 = (short)(array[1]);
                    chrAsc = i1 * 256 + i2 - 65536;
                    if (chrAsc > 0 && chrAsc < 160)
                    {
                        pyString += noWChar[j];
                    }
                    else
                    {
                        // 修正部分文字
                        if (chrAsc == -9254)  // 修正“圳”字
                            pyString += "Zhen";
                        else
                        {
                            for (int i = (pyValue.Length - 1); i >= 0; i--)
                            {
                                if (pyValue[i] <= chrAsc)
                                {
                                    pyString += pyName[i];
                                    break;
                                }
                            }
                        }
                    }
                }
                // 非中文字符
                else
                {
                    pyString += noWChar[j].ToString();
                }
            }
            return pyString;
        }

        public static string HtmlEncode(string value)
        {
            return HttpUtility.HtmlEncode(HttpUtility.HtmlDecode(value));
        }

        /// <summary>
        /// Gets the capitals of the chinese string.
        /// </summary>
        /// <param name="cnString">the chinese string.</param>
        /// <returns>the capitals of the chinese string.</returns>
        public static string GetCapital(string cnString)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char cnChar in cnString)
            {
                string temp = GetFullSpell(cnChar.ToString());
                if (temp != null && temp.Length > 0)
                {
                    builder.Append(temp[0]);
                }
            }

            return builder.ToString();
        }

        //private static string GetCapital2(string str)
        //{
        //    if (str.CompareTo("吖") < 0) return str.ToString();
        //    if (str.CompareTo("八") < 0) return "A";
        //    if (str.CompareTo("嚓") < 0) return "B";
        //    if (str.CompareTo("咑") < 0) return "C";
        //    if (str.CompareTo("妸") < 0) return "D";
        //    if (str.CompareTo("发") < 0) return "E";
        //    if (str.CompareTo("旮") < 0) return "F";
        //    if (str.CompareTo("铪") < 0) return "G";
        //    if (str.CompareTo("讥") < 0) return "H";
        //    if (str.CompareTo("咔") < 0) return "J";
        //    if (str.CompareTo("垃") < 0) return "K";
        //    if (str.CompareTo("嘸") < 0) return "L";
        //    if (str.CompareTo("拏") < 0) return "M";
        //    if (str.CompareTo("噢") < 0) return "N";
        //    if (str.CompareTo("妑") < 0) return "O";
        //    if (str.CompareTo("七") < 0) return "P";
        //    if (str.CompareTo("亽") < 0) return "Q";
        //    if (str.CompareTo("仨") < 0) return "R";
        //    if (str.CompareTo("他") < 0) return "S";
        //    if (str.CompareTo("哇") < 0) return "T";
        //    if (str.CompareTo("夕") < 0) return "W";
        //    if (str.CompareTo("丫") < 0) return "X";
        //    if (str.CompareTo("帀") < 0) return "Y";
        //    if (str.CompareTo("咗") < 0) return "Z";
        //    return str.ToString();
        //}

        public static string ConvertNumberToChinese(double x)
        {
            string s = x.ToString("#L#E#D#C#K#E#D#C#J#E#D#C#I#E#D#C#H#E#D#C#G#E#D#C#F#E#D#C#.0B0A");
            string d = Regex.Replace(s, @"((?<=-|^)[^1-9]*)|((?'z'0)[0A-E]*((?=[1-9])|(?'-z'(?=[F-L\.]|$))))|((?'b'[F-L])(?'z'0)[0A-L]*((?=[1-9])|(?'-z'(?=[\.]|$))))", "${b}${z}");
            return Regex.Replace(d, ".", m => "负元空零壹贰叁肆伍陆柒捌玖空空空空空空空分角拾佰仟萬億兆京垓秭穰"[m.Value[0] - '-'].ToString());
        }

        /// <summary>
        /// Gets the chinese finance number format.
        /// </summary>
        /// <param name="num">the value of the number.</param>
        /// <returns>the chinese finance number format.</returns>
        public static string ConvertNumberToChinese(string value)
        {
            //数字 数组  
            string[] Nums = new string[] { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖" };
            //位 数组  
            string[] Digits = new string[] { "", "拾", "佰", "仟" };
            //单位 数组  
            string[] Units = new string[] { "", "[万]", "[亿]", "[万亿]" };

            string result = ""; //返回值  
            int p = 0; //字符位置指针  
            int m = value.Length % 4; //取模  
            // 四位一组得到组数  
            int k = (m > 0 ? value.Length / 4 + 1 : value.Length / 4);
            // 外层循环在所有组中循环  
            // 从左到右 高位到低位 四位一组 逐组处理  
            // 每组最后加上一个单位: "[万亿]","[亿]","[万]"  
            for (int i = k; i > 0; i--)
            {
                int L = 4;
                if (i == k && m != 0)
                {
                    L = m;
                }
                // 得到一组四位数 最高位组有可能不足四位  
                string s = value.Substring(p, L);
                int l = s.Length;
                // 内层循环在该组中的每一位数上循环 从左到右 高位到低位  
                for (int j = 0; j < l; j++)
                {
                    //处理改组中的每一位数加上所在位: "仟","佰","拾",""(个)  
                    int n = Convert.ToInt32(s.Substring(j, 1));
                    if (n == 0)
                    {
                        if (j < l - 1
                            && Convert.ToInt32(s.Substring(j + 1, 1)) > 0 //后一位(右低) 
                            && !result.EndsWith(Nums[n]))
                        {
                            result += Nums[n];
                        }
                    }
                    else
                    {
                        //处理 1013 一千零"十三", 1113 一千一百"一十三"  
                        if (!(n == 1 && (result.EndsWith(Nums[0]) | result.Length == 0) && j == l - 2))
                        {
                            result += Nums[n];
                        }
                        result += Digits[l - j - 1];
                    }
                }
                p += L;
                // 每组最后加上一个单位: [万],[亿] 等  
                if (i < k) //不是最高位的一组  
                {
                    if (Convert.ToInt32(s) != 0)
                    {
                        //如果所有 4 位不全是 0 则加上单位 [万],[亿] 等  
                        result += Units[i - 1];
                    }
                }
                else
                {
                    //处理最高位的一组,最后必须加上单位  
                    result += Units[i - 1];
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the random string in the alpha number.
        /// </summary>
        /// <param name="length">the length of the random string.</param>
        /// <returns>the random string in the alpha number</returns>
        public static string GetRandomString(int length)
        {
            string result = string.Empty;

            for (int i = 0; i < length; i++)
            {
                string temp = ValidAlphaNum[random.Next(60)].ToString();
                result += temp;
            }

            return result;
        }

        public static string GetRandomStringOnlyNumber(int length)
        {
            string result = string.Empty;

            for (int i = 0; i < length; i++)
            {
                string temp = random.Next(10).ToString();
                result += temp;
            }

            return result;
        }

        public static string AbSolutePath(this string content)
        {
            ThrowExceptionUtil.ArgumentNotNull(content, "content");

            string replaceString = @"src=""{0}$1""".FormatWith(HttpContextUtil.EnterUrl);

            return AbsolutePathRegex.Replace(content, replaceString);
        }

        public static uint IPStringToUInt32(string ipV4Address)
        {
            if (string.IsNullOrEmpty(ipV4Address))
            {
                return 0;
            }
            string[] strArray = ipV4Address.Split(new char[] { '.' });
            if ((strArray == null) || (strArray.Length != 4))
            {
                throw new ApplicationException("ip 地址格式错误！");
            }
            return BitConverter.ToUInt32(new byte[] { Convert.ToByte(strArray[3]), Convert.ToByte(strArray[2]), Convert.ToByte(strArray[1]), Convert.ToByte(strArray[0]) }, 0);
        }

        public static string UInt32ToIpV4String(uint ip)
        {
            string[] strArray = new string[] { Convert.ToString((uint)((ip & -16777216) >> 0x18)), Convert.ToString((uint)((ip & 0xff0000) >> 0x10)), Convert.ToString((uint)((ip & 0xff00) >> 8)), Convert.ToString((uint)(ip & 0xff)) };
            return string.Format("{0}.{1}.{2}.{3}", new object[] { strArray[0], strArray[1], strArray[2], strArray[3] });
        }



        /// <summary>
        /// Provided to fomat string using the razor format.
        /// </summary>
        /// <param name="format">string format</param>
        /// <param name="obj">the instance.</param>
        /// <returns>the converted string.</returns>
        public static string RazorFormat(this string format, object obj)
        {
            ThrowExceptionUtil.ArgumentNotNull(format, "format");
            ThrowExceptionUtil.ArgumentNotNull(obj, "obj");
            string result = string.Empty;

            List<string> propertyList = new List<string>();
            Match match = FormatRegex.Match(format);
            while (match.Success)
            {
                propertyList.Add(match.Groups["name"].Value);
                match = match.NextMatch();
            }

            result = format;

            if (obj is DataRow)
            {
                DataRow rowItem = obj as DataRow;
                foreach (string item in propertyList)
                {
                    if (rowItem.Table.Columns.Contains(item))
                    {
                        result = result.Replace("@" + item, rowItem[item].ToString());
                    }
                }
            }
            else if (obj is BeeDataAdapter)
            {
                BeeDataAdapter dataAdapter = obj as BeeDataAdapter;
                foreach (string item in propertyList)
                {
                    result = result.Replace("@" + item, dataAdapter.Format(item));
                }
            }
            else
            {
                IEntityProxy entityProxy = EntityProxyManager.Instance.GetEntityProxyFromType(obj.GetType());
                foreach (string item in propertyList)
                {
                    object propertyValue = entityProxy.GetPropertyValue(obj, item);
                    result = result.Replace("@" + item, propertyValue == null ? string.Empty : propertyValue.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Provided the extended method of string to format.
        /// </summary>
        /// <param name="format">the format string.</param>
        /// <param name="args">the arguments of the format stirng.</param>
        /// <returns>the result.</returns>
        public static string FormatWith(this string format, params object[] args)
        {
            ThrowExceptionUtil.ArgumentNotNull(format, "format");
            return format.FormatWith(CultureInfo.CurrentCulture, args);
        }

        /// <summary>
        /// Provided the extended method of string to format.
        /// </summary>
        /// <param name="format">the format string.</param>
        /// <param name="provider">the formating mechanism.</param>
        /// <param name="args">the arguments of the format stirng.</param>
        /// <returns>the result.</returns>
        public static string FormatWith(this string format, IFormatProvider provider, params object[] args)
        {
            ThrowExceptionUtil.ArgumentNotNull(format, "format");
            return string.Format(provider, format, args);

            
        }

        public static string GetFirstItem(this string itemsValue, params char[] splitChars)
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(itemsValue))
            {
                string[] array = itemsValue.Split(splitChars);
                if (array.Length > 0)
                {
                    result = array[0];
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the related url.
        /// </summary>
        /// <param name="url">the url.</param>
        /// <param name="basePath">the base path needed to be cut.</param>
        /// <returns>the related url.</returns>
        internal static string MakeUrlRelative(string url, string basePath)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "~/";
            }
            if (!((basePath != null) && basePath.StartsWith("/")))
            {
                basePath = "/" + basePath;
            }
            if (url.StartsWith("http://", true, null))
            {
                Uri uri = new Uri(url);
                url = uri.PathAndQuery;
            }
            else if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }
            if (basePath == "/")
            {
                return ("~" + url);
            }
            basePath = basePath.ToLower();
            string str = url.ToLower();
            url = url.Substring(str.IndexOf(basePath) + basePath.Length);
            if (url.StartsWith("/"))
            {
                url = "~" + url;
            }
            else
            {
                url = "~/" + url;
            }
            return url;
        }

        public static string Escape(string str)
        {
            string str2 = "0123456789ABCDEF";
            int length = str.Length;
            StringBuilder builder = new StringBuilder(length * 2);
            int num3 = -1;
            while (++num3 < length)
            {
                char ch = str[num3];
                int num2 = ch;
                // 不在 A~Z， a~z, 0~9
                if (((( num2 < 65) || (num2 > 90)) && (( num2 < 97) || (num2 > 122))) && ((num2 < 48) || (num2 > 57)))
                {
                    switch (ch)
                    {
                        case '@':
                        case '*':
                        case '_':
                        case '+':
                        case '-':
                        case '.':
                        case '/':
                            builder.Append(ch);
                            continue;
                    }
                    builder.Append('%');
                    if (num2 < 0x100)
                    {
                        builder.Append(str2[num2 / 0x10]);
                        ch = str2[num2 % 0x10];
                    }
                    else
                    {
                        builder.Append('u');
                        builder.Append(str2[(num2 >> 12) % 0x10]);
                        builder.Append(str2[(num2 >> 8) % 0x10]);
                        builder.Append(str2[(num2 >> 4) % 0x10]);
                        ch = str2[num2 % 0x10];
                    }
                }
                builder.Append(ch);
            }
            return builder.ToString();

        }

        public static string unescape(string str)
        {
            int length = str.Length;
            StringBuilder builder = new StringBuilder(length);
            int index = -1;
            while (++index < length)
            {
                char ch = str[index];
                if (ch == '%')
                {
                    int num2;
                    int num3;
                    int num4;
                    int num5;
                    if (((((index + 5) < length) && (str[index + 1] == 'u')) && (((num2 = HexToInt(str[index + 2])) != -1) && ((num3 = HexToInt(str[index + 3])) != -1))) && (((num4 = HexToInt(str[index + 4])) != -1) && ((num5 = HexToInt(str[index + 5])) != -1)))
                    {
                        ch = (char)((((num2 << 12) + (num3 << 8)) + (num4 << 4)) + num5);
                        index += 5;
                    }
                    else if ((((index + 2) < length) && ((num2 = HexToInt(str[index + 1])) != -1)) && ((num3 = HexToInt(str[index + 2])) != -1))
                    {
                        ch = (char)((num2 << 4) + num3);
                        index += 2;
                    }
                }
                builder.Append(ch);
            }
            return builder.ToString();
        }

        private static int HexToInt(char h)
        {
            if ((h >= '0') && (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }

        public static bool IsUrl(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            string pattern = @"^(http|https|ftp|rtsp|mms):(\/\/|\\\\)[A-Za-z0-9%\-_@]+\.[A-Za-z0-9%\-_@]+[A-Za-z0-9\.\/=\?%\-&_~`@:\+!;]*$";
            return Regex.IsMatch(str, pattern, RegexOptions.IgnoreCase);
        }

        public static bool IsEmail(string str)
        {
            return Regex.IsMatch(str, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        ///   <summary>   
        ///   去除HTML标记   
        ///   </summary>   
        ///   <param   name="NoHTML">包括HTML的源码   </param>   
        ///   <returns>已经去除后的文字</returns>   
        public static string RemoveHTML(this string Htmlstring)
        {
            //删除脚本   
            Htmlstring = Regex.Replace(Htmlstring, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML   
            Htmlstring = Regex.Replace(Htmlstring, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"<!--.*", "", RegexOptions.IgnoreCase);

            Htmlstring = Regex.Replace(Htmlstring, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(nbsp|#160);", "   ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, @"&#(\d+);", "", RegexOptions.IgnoreCase);

            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring.Replace("\r\n", "");
            Htmlstring = HttpContext.Current.Server.HtmlEncode(Htmlstring).Trim();

            return Htmlstring;
        }
        /// <summary>
        /// 过滤js脚本
        /// </summary>
        /// <param name="strFromText"></param>
        /// <returns></returns>
        public static string RemoveScript(this string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<script[\s\S]+</script *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex2 = new System.Text.RegularExpressions.Regex(@" href *= *[\s\S]*script *:", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex3 = new System.Text.RegularExpressions.Regex(@" on[\s\S]*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex4 = new System.Text.RegularExpressions.Regex(@"<iframe[\s\S]+</iframe *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex5 = new System.Text.RegularExpressions.Regex(@"<frameset[\s\S]+</frameset *>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex1.Replace(html, ""); //过滤<script></script>标记
            html = regex2.Replace(html, ""); //过滤href=javascript: (<A>) 属性
            html = regex3.Replace(html, " _disibledevent="); //过滤其它控件的on...事件
            html = regex4.Replace(html, ""); //过滤iframe
            html = regex5.Replace(html, ""); //过滤frameset
            return html;
        }
        /// <summary>
        /// 替换页面标签
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string RemovePageTag(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            System.Text.RegularExpressions.Regex regex0 = new System.Text.RegularExpressions.Regex(@"<!DOCTYPE[^>]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex1 = new System.Text.RegularExpressions.Regex(@"<html\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex2 = new System.Text.RegularExpressions.Regex(@"<head[\s\S]+</head\s*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex3 = new System.Text.RegularExpressions.Regex(@"<body\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex4 = new System.Text.RegularExpressions.Regex(@"<form\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            System.Text.RegularExpressions.Regex regex5 = new System.Text.RegularExpressions.Regex(@"</(form|body|head|html)>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            html = regex0.Replace(html, ""); //过滤<html>标记
            html = regex1.Replace(html, "<html\u3000 "); //过滤<html>标记
            html = regex2.Replace(html, ""); //过滤<head>属性
            html = regex3.Replace(html, "<body\u3000 "); //过滤<body>属性
            html = regex4.Replace(html, "<form\u3000 "); //过滤<form>属性
            html = regex5.Replace(html, "</$1\u3000>"); //过滤</html></body></head></form>属性
            return html;
        }

        /// <summary>
        /// 取得html中的图片
        /// </summary>
        /// <param name="HTMLStr"></param>
        /// <returns></returns>
        public static string GetImg(string text)
        {
            string str = string.Empty;
            Regex r = new Regex(@"<img\s+[^>]*\s*src\s*=\s*([']?)(?<url>\S+)'?[^>]*>", //注意这里的(?<url>\S+)是按正则表达式中的组来处理的，下面的代码中用使用到，也可以更改成其它的HTML标签，以同样的方法获得内容！
            RegexOptions.Compiled);
            Match m = r.Match(text.ToLower());
            if (m.Success)
                str = m.Result("${url}").Replace("\"", "").Replace("'", "");
            return str;
        }
        /// <summary>
        /// 取得html中的所有图片
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string[] GetImgs(string text)
        {
            List<string> imgs = new List<string>();
            string pat = @"<img\s+[^>]*\s*src\s*=\s*([']?)(?<url>\S+)'?[^>]*>";
            Regex r = new Regex(pat, RegexOptions.Compiled);
            Match m = r.Match(text.ToLower());
            while (m.Success)
            {
                imgs.Add(m.Result("${url}").Replace("\"", "").Replace("'", ""));
                m = m.NextMatch();
            }
            return imgs.ToArray();
        }

        public static string Transform(string input, Regex pattern, Converter<Match, string> fnReplace)
        {
            int startIndex = 0;
            StringBuilder builder = new StringBuilder();
            foreach (Match match in pattern.Matches(input))
            {
                builder.Append(input, startIndex, match.Index - startIndex);
                string str = fnReplace(match);
                builder.Append(str);
                startIndex = match.Index + match.Length;
            }
            builder.Append(input, startIndex, input.Length - startIndex);
            return builder.ToString();
        }

 

    }
}
