using System;
using AutoyaFramework.Representation.JWT;
using Miyamasu;
using UnityEngine;


/**
	test for jwt token generation & validation.
*/

public class JWTTests : MiyamasuTestRunner
{
    /*
		parameters are based on https://jwt.io
	*/
    private readonly string VALID_TOKEN_STR = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWV9.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";
    /*
		header
		{
			"alg": "HS256",
			"typ": "JWT"
		}

		payload
		{
			"sub": "1234567890",
			"name": "John Doe",
			"admin": true
		}
	*/

    [Serializable]
    private class SampleTokenData
    {
        [SerializeField] public string sub;
        [SerializeField] public string name;
        [SerializeField] public bool admin;

        public SampleTokenData() { }
        public SampleTokenData(string sub, string name, bool admin)
        {
            this.sub = sub;
            this.name = name;
            this.admin = admin;
        }
    }


    [MTest]
    public void Read()
    {
        var data = VALID_TOKEN_STR;
        var key = "secret";
        var validatedData = JWT.Read<SampleTokenData>(data, key);
        True(
            validatedData.sub == "1234567890" &&
            validatedData.name == "John Doe" &&
            validatedData.admin,
            "parameters not match."
        );
    }


    [MTest]
    public void Create()
    {
        var newData = new SampleTokenData("1234567890", "John Doe", true);
        var key = "secret";

        var newToken = JWT.Create(newData, key);
        True(newToken == VALID_TOKEN_STR, "signature not match.");
    }
}