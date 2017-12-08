using System;
using UnityEngine;
using AutoyaFramework.Encrypt.SHA_2;

/*
	JsonWebToken representation for Autoya.
*/
namespace AutoyaFramework.Representation.JWT
{
    public class JWT
    {
        private static readonly JWTHeaderStruct headerStruct = new JWTHeaderStruct("HS256", "JWT");
        [Serializable]
        private struct JWTHeaderStruct
        {
#pragma warning disable 414
            [SerializeField] private string alg;
            [SerializeField] private string typ;
#pragma warning restore 414
            public JWTHeaderStruct(string alg, string typ)
            {
                this.alg = alg;
                this.typ = typ;
            }
        }

        public static T Read<T>(string data, string request) where T : new()
        {
            var datas = data.Split('.');

            if (datas.Length != 3)
            {
                return new T();
            }

            var headerStr = datas[0];
            var payloadStr = datas[1];
            var signature = datas[2];

            var generatedSignatureBytes = SHA_2.Sha256Bytes(headerStr + "." + payloadStr, request);
            var encodedSignature = Base64.Base64.FromBytes(generatedSignatureBytes);
            var uppaddedEncodedSignature = Base64.Base64.Unpadded(encodedSignature);

            if (signature == uppaddedEncodedSignature)
            {
                return JsonUtility.FromJson<T>(Base64.Base64.ConvertToStr(payloadStr));
            }

            return new T();// return empty object if value is not matched with sign.
        }

        public static string Create<T>(T body, string request) where T : new()
        {
            var headerStr = Base64.Base64.FromString(JsonUtility.ToJson(headerStruct));
            var payloadStr = Base64.Base64.FromString(JsonUtility.ToJson(body));
            var signature = Base64.Base64.FromBytes(SHA_2.Sha256Bytes(headerStr + "." + payloadStr, request));

            var unpaddedSignature = Base64.Base64.Unpadded(signature);

            return headerStr + "." + payloadStr + "." + unpaddedSignature;
        }
    }

}

