using System;
using System.Text;

/*
	Base64 representation for Autoya.
*/
namespace AutoyaFramework.Representation.Base64
{

    public class Base64
    {

        public static string FromString(string source)
        {
            return FromBytes(Encoding.UTF8.GetBytes(source));
        }

        public static string FromBytes(byte[] source)
        {
            return Convert.ToBase64String(source);
        }

        public static string ConvertToStr(string base64Str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64Str));
        }

        public static byte[] ConvertToBytes(string base64Str)
        {
            return Convert.FromBase64String(base64Str);
        }

        public static string Padded(string base64Str)
        {
            var additionalLen = base64Str.Length % 16;
            for (var i = 0; i < additionalLen; i++)
            {
                base64Str = base64Str + "=";
            }
            return base64Str;
        }

        public static string Unpadded(string base64Str)
        {
            return base64Str.TrimEnd('=');
        }
    }

}