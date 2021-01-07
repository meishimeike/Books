using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Books
{
    public class Configs
    {
        public static string ImagePath;
        public static SQLiteHelper Sql;
        public static string Font;
        public static string FontColor;
        public static string BackColor;
        public static int UserId;
    }

    public partial class MD5Helper
    {
        /// <summary>
        /// MD5 加密
        /// </summary>
        /// <param name="str">要加密的字符串</param>
        /// <returns>加密后字符串</returns>
        public static string EncryptString(string str)
        {
            MD5 md5 = MD5.Create();
            // 将字符串转换成字节数组
            byte[] byteOld = Encoding.UTF8.GetBytes(str);
            // 调用加密方法
            byte[] byteNew = md5.ComputeHash(byteOld);
            // 将加密结果转换为字符串
            StringBuilder sb = new StringBuilder();
            foreach (byte b in byteNew)
            {
                // 将字节转换成16进制表示的字符串，
                sb.Append(b.ToString("x2"));
            }
            // 返回加密的字符串
            return sb.ToString();
        }
    }

    /// <summary>
    /// 字符串加解密
    /// </summary>
    public class MyCryptography
    {
        /// <summary>
        /// DES加密
        /// </summary>
        /// <param name="data">加密数据</param>
        /// <param name="key">8位字符的密钥字符串</param>
        /// <param name="iv">8位字符的初始化向量字符串</param>
        /// <returns></returns>
        public static string DESEncrypt(string data, string key="df4&hidd", string iv="mjht24x@")
        {
            data = GZipUtil.CompressString(data);
            byte[] byKey = Encoding.ASCII.GetBytes(key);
            byte[] byIV = Encoding.ASCII.GetBytes(iv);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            int i = cryptoProvider.KeySize;
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);

        }

        /// <summary>
        /// DES解密
        /// </summary>
        /// <param name="data">解密数据</param>
        /// <param name="key">8位字符的密钥字符串(需要和加密时相同)</param>
        /// <param name="iv">8位字符的初始化向量字符串(需要和加密时相同)</param>
        /// <returns></returns>
        public static string DESDecrypt(string data, string key = "df4&hidd", string iv = "mjht24x@")
        {
            byte[] byKey = Encoding.ASCII.GetBytes(key);
            byte[] byIV = Encoding.ASCII.GetBytes(iv);
            byte[] byEnc;
            try
            {
                byEnc = System.Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream ms = new MemoryStream(byEnc);
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
            StreamReader sr = new StreamReader(cst);
            string res= sr.ReadToEnd();
            return GZipUtil.DecompressString(res);
        }
    }

    /// <summary>
    /// 字符压缩
    /// </summary>
    public class GZipUtil
    {

        /// <summary>  
        /// 字节数组压缩  
        /// </summary>  
        /// <param name="strSource"></param>  
        /// <returns></returns>  
        public static byte[] Compress(byte[] data)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
                zip.Write(data, 0, data.Length);
                zip.Close();
                byte[] buffer = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(buffer, 0, buffer.Length);
                ms.Close();
                return buffer;

            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        /// <summary>  
        /// 字节数组解压缩  
        /// </summary>  
        /// <param name="strSource"></param>  
        /// <returns></returns>  
        public static byte[] Decompress(byte[] data)
        {
            try
            {
                MemoryStream ms = new MemoryStream(data);
                GZipStream zip = new GZipStream(ms, CompressionMode.Decompress, true);
                MemoryStream msreader = new MemoryStream();
                byte[] buffer = new byte[0x1000];
                while (true)
                {
                    int reader = zip.Read(buffer, 0, buffer.Length);
                    if (reader <= 0)
                    {
                        break;
                    }
                    msreader.Write(buffer, 0, reader);
                }
                zip.Close();
                ms.Close();
                msreader.Position = 0;
                buffer = msreader.ToArray();
                msreader.Close();
                return buffer;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// 字符串压缩
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="str">String.</param>
        public static string CompressString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) { return ""; }
            string compressString = "";
            byte[] compressBeforeByte = Encoding.UTF8.GetBytes(str);
            byte[] compressAfterByte = Compress(compressBeforeByte);
            //compressString = Encoding.GetEncoding("UTF-8").GetString(compressAfterByte);    
            compressString = Convert.ToBase64String(compressAfterByte);
            return compressString;
        }
        /// <summary>
        /// 字符串解压缩
        /// </summary>
        /// <returns>The string.</returns>
        /// <param name="str">String.</param>
        public static string DecompressString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) { return ""; }
            string compressString = "";
            //byte[] compressBeforeByte = Encoding.GetEncoding("UTF-8").GetBytes(str);    
            byte[] compressBeforeByte = Convert.FromBase64String(str);
            byte[] compressAfterByte = Decompress(compressBeforeByte);
            compressString = Encoding.UTF8.GetString(compressAfterByte);
            return compressString;
        }
    }
}
