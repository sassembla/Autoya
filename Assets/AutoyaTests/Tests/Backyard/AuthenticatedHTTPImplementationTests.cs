using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	tests for Autoya Authenticated HTTP.
	Autoya strongly handle these server-related errors which comes from game-server.

	these test codes are depends on online env + "https://httpbin.org".
*/
public class AuthenticatedHTTPImplementationTests : MiyamasuTestRunner
{
    private void DeleteAllData(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    [MSetup]
    public IEnumerator Setup()
    {
        Autoya.ResetAllForceSetting();

        var dataPath = Application.persistentDataPath;
        var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
        DeleteAllData(fwPath);

        Autoya.TestEntryPoint(dataPath);

        while (!Autoya.Auth_IsAuthenticated())
        {
            yield return false;
        }

        var authenticated = false;
        Autoya.Auth_SetOnAuthenticated(
            () =>
            {
                authenticated = true;
            }
        );
        Autoya.Auth_SetOnBootAuthFailed(
            (code, reason) =>
            {
                Debug.LogError("code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => authenticated,
            () => { throw new TimeoutException("failed to auth."); }
        );

        True(Autoya.Auth_IsAuthenticated(), "not logged in.");
    }

    [MTeardown]
    public IEnumerator Teardown()
    {
        Autoya.ResetAllForceSetting();
        Autoya.Shutdown();
        while (GameObject.Find("AutoyaMainthreadDispatcher") != null)
        {
            yield return null;
        }
    }

    [MTest]
    public IEnumerator AutoyaHTTPGet()
    {
        var result = string.Empty;
        Autoya.Http_Get(
            "https://httpbin.org/get",
            (conId, resultData) =>
            {
                result = "done!:" + resultData;
            },
            (conId, code, reason, autoyaStatus) =>
            {
                True(false, "failed. code:" + code + " reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => !string.IsNullOrEmpty(result),
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator AutoyaHTTPGetWithAdditionalHeader()
    {
        var result = string.Empty;
        Autoya.Http_Get(
            "https://httpbin.org/headers",
            (conId, resultData) =>
            {
                result = resultData;
            },
            (conId, code, reason, autoyaStatus) =>
            {
                True(false, "failed. code:" + code + " reason:" + reason);
            },
            new Dictionary<string, string>{
                {"Hello", "World"}
            }
        );

        yield return WaitUntil(
            () => (result.Contains("Hello") && result.Contains("World")),
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator AutoyaHTTPGetFailWith404()
    {
        var resultCode = 0;

        Autoya.Http_Get(
            "https://httpbin.org/status/404",
            (conId, resultData) =>
            {
                True(false, "unexpected succeeded. resultData:" + resultData);
            },
            (conId, code, reason, autoyaStatus) =>
            {
                resultCode = code;
            }
        );

        yield return WaitUntil(
            () => (resultCode != 0),
            () => { throw new TimeoutException("timeout."); }
        );

        // result should be have reason,
        True(resultCode == 404, "code unmatched. resultCode:" + resultCode);
    }

    [MTest]
    public IEnumerator AutoyaHTTPGetFailWithUnauth()
    {
        Autoya.forceSetHttpCodeAsUnauthorized = true;
        var unauthorized = false;

        /*
			dummy server returns 401 forcibly.
		*/

        Autoya.Http_Get(
            "https://httpbin.org/status/401",
            (conId, resultData) =>
            {
                Fail("never succeed.");
            },
            (conId, code, reason, autoyaStatus) =>
            {
                unauthorized = autoyaStatus.isAuthFailed;
                Autoya.forceSetHttpCodeAsUnauthorized = false;
            }
        );

        yield return WaitUntil(
            () => unauthorized,
            () => { throw new TimeoutException("timeout."); }
        );

        // token refresh feature is already running. wait end.
        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to refresh token."); }
        );
    }

    [MTest]
    public IEnumerator AutoyaHTTPGetFailWithTimeout()
    {
        var failedCode = -1;
        var timeoutError = string.Empty;
        /*
			fake server should be response in 1msec. 
			server responses 1 sec later.
			it is impossible.
		*/
        Autoya.Http_Get(
            "https://httpbin.org/delay/1",
            (conId, resultData) =>
            {
                True(false, "got success result.");
            },
            (conId, code, reason, autoyaStatus) =>
            {
                failedCode = code;
                timeoutError = reason;
            },
            null,
            0.0001
        );

        yield return WaitUntil(
            () =>
            {
                return !string.IsNullOrEmpty(timeoutError);
            },
            () => { throw new TimeoutException("timeout."); }
        );

        True(failedCode == BackyardSettings.HTTP_TIMEOUT_CODE, "unmatch. failedCode:" + failedCode + " message:" + timeoutError);
    }

    [MTest]
    public IEnumerator AutoyaHTTPPost()
    {
        var result = string.Empty;
        Autoya.Http_Post(
            "https://httpbin.org/post",
            "data",
            (conId, resultData) =>
            {
                result = "done!:" + resultData;
            },
            (conId, code, reason, autoyaStatus) =>
            {
                // do nothing.
            }
        );

        yield return WaitUntil(
            () => !string.IsNullOrEmpty(result),
            () => { throw new TimeoutException("timeout."); }
        );
    }

    /*
		target test site does not support show post request. hmmm,,,
	*/
    // [MTest] public IEnumerator AutoyaHTTPPostWithAdditionalHeader () {
    // 	var result = string.Empty;
    // 	Autoya.Http_Post(
    // 		"https://httpbin.org/headers", 
    // 		"data",
    // 		(conId, resultData) => {
    // 			TestLogger.Log("resultData:" + resultData);
    // 			result = resultData;
    // 		},
    // 		(conId, code, reason) => {
    // 			TestLogger.Log("fmmmm,,,,, AutoyaHTTPPostWithAdditionalHeader failed conId:" + conId + " reason:" + reason);
    // 			// do nothing.
    // 		},
    // 		new Dictionary<string, string>{
    // 			{"Hello", "World"}
    // 		}
    // 	);

    // 	var wait = yield return WaitUntil(
    // 		() => (result.Contains("Hello") && result.Contains("World")), 
    // 		5
    // 	);
    // 	if (!wait) return false; 

    // 	return true;
    // }

    [MTest]
    public IEnumerator AutoyaHTTPPostFailWith404()
    {
        var resultCode = 0;

        Autoya.Http_Post(
            "https://httpbin.org/status/404",
            "data",
            (conId, resultData) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                resultCode = code;
            }
        );

        yield return WaitUntil(
            () => (resultCode == 404),
            () => { throw new TimeoutException("failed to detect 404."); }
        );

        // result should be have reason,
        True(resultCode == 404, "code unmatched. resultCode:" + resultCode);
    }

    [MTest]
    public IEnumerator AutoyaHTTPPostFailWithUnauth()
    {
        Autoya.forceSetHttpCodeAsUnauthorized = true;
        var unauthorized = false;

        /*
			dummy server returns 401 forcibly.
		*/
        Autoya.Http_Post(
            "https://httpbin.org/status/401",
            "dummy_data",
            (conId, resultData) =>
            {
                Fail();
            },
            (conId, code, reason, autoyaStatus) =>
            {
                unauthorized = autoyaStatus.isAuthFailed;
                Autoya.forceSetHttpCodeAsUnauthorized = false;
            }
        );

        yield return WaitUntil(
            () => unauthorized,
            () => { throw new TimeoutException("timeout."); },
            10
        );


        // token refresh feature is already running. wait end.
        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to refresh token."); },
            20
        );
    }

    [MTest]
    public IEnumerator AutoyaHTTPPostFailWithTimeout()
    {
        var timeoutError = string.Empty;
        /*
			fake server should be response 1msec
		*/
        Autoya.Http_Post(
            "https://httpbin.org/delay/1",
            "data",
            (conId, resultData) =>
            {
                True(false, "got success result.");
            },
            (conId, code, reason, autoyaStatus) =>
            {
                True(code == BackyardSettings.HTTP_TIMEOUT_CODE, "not match. code:" + code + " reason:" + reason);
                timeoutError = reason;
            },
            null,
            0.0001// 1ms
        );

        yield return WaitUntil(
            () =>
            {
                return !string.IsNullOrEmpty(timeoutError);
            },
            () => { throw new TimeoutException("timeout."); }
        );
    }

}
