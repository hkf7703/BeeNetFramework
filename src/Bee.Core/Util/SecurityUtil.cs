using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Bee.Util
{
    public static class SecurityUtil
    {

        public static string Sha1EncrptS(string input)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            var sha1Arr = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder enText = new StringBuilder();
            foreach (var b in sha1Arr)
            {
                enText.AppendFormat("{0:x2}", b);
            }

            return enText.ToString();
        }

        /// <summary>
        /// Encryption via MD5.
        /// </summary>
        /// <param name="input">the value needed to be encrypted.</param>
        /// <returns>the encrypted string.</returns>
        public static string MD5EncryptS(string input, bool lowerCase = false)
        {
            var source = Encoding.UTF8.GetBytes(input);
            using (var md5Hash = MD5.Create())
            {
                return md5Hash.ComputeHash(source).ToHex(lowerCase);
            }
        }

        /// <summary>
        /// Encryption via DES. this can be decrypted.
        /// </summary>
        /// <param name="value">the string need to be encrypted.</param>
        /// <param name="key">the key for encryption.</param>
        /// <returns>the encrypted string.</returns>
        public static string EncryptS(string value, string key)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key), string.Empty, "should not be empty or null");

            DESCryptoServiceProvider des = new DESCryptoServiceProvider(); //把字符串放到byte数组中 

            //byte[] inputByteArray = Encoding.Default.GetBytes(pToEncrypt);
            byte[] inputByteArray = Encoding.UTF8.GetBytes(value);

            des.Key = Encoding.UTF8.GetBytes(key); //建立加密对象的密钥和偏移量
            des.IV = Encoding.UTF8.GetBytes(key);   //原文使用ASCIIEncoding.ASCII方法的GetBytes方法 
            using (MemoryStream ms = new MemoryStream())     //使得输入密码必须输入英文文本
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                }

                StringBuilder ret = new StringBuilder();
                foreach (byte b in ms.ToArray())
                {
                    ret.AppendFormat("{0:X2}", b);
                }
                ret.ToString();
                return ret.ToString();
            }
        }

        /// <summary>
        /// Decryption via DES.
        /// </summary>
        /// <param name="value">the string need to be decrypted.</param>
        /// <param name="key">the key for decryption.</param>
        /// <returns>the decrypted string.</returns>
        public static string DecryptS(string value, string key)
        {
            ThrowExceptionUtil.ArgumentConditionTrue(!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(key), string.Empty, "should not be empty or null");

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            byte[] inputByteArray = new byte[value.Length / 2];
            for (int x = 0; x < value.Length / 2; x++)
            {
                int i = (Convert.ToInt32(value.Substring(x * 2, 2), 16));
                inputByteArray[x] = (byte)i;
            }

            des.Key = Encoding.UTF8.GetBytes(key); //建立加密对象的密钥和偏移量，此值重要，不能修改 
            des.IV = Encoding.UTF8.GetBytes(key);
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                }

                StringBuilder ret = new StringBuilder(); //建立StringBuild对象，CreateDecrypt使用的是流对象，必须把解密后的文本变成流对象 

                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}
