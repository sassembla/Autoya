using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	test for authorization flow control.
*/
public class AuthImplementationTests : MiyamasuTestRunner
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

        var authorized = false;
        var dataPath = Application.persistentDataPath;

        var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
        DeleteAllData(fwPath);

        Autoya.TestEntryPoint(dataPath);

        Autoya.Auth_SetOnAuthenticated(
            () =>
            {
                authorized = true;
            }
        );

        yield return WaitUntil(
            () =>
            {
                return authorized;
            },
            () => { throw new TimeoutException("timeout in setup."); },
            10
        );

        True(Autoya.Auth_IsAuthenticated(), "not logged in.");
    }

    [MTeardown]
    public IEnumerator Teardown()
    {
        Autoya.Shutdown();
        Autoya.ResetAllForceSetting();
        while (GameObject.Find("AutoyaMainthreadDispatcher") != null)
        {
            yield return null;
        }
    }


    [MTest]
    public IEnumerator WaitDefaultAuthenticate()
    {
        True(Autoya.Auth_IsAuthenticated(), "not yet logged in.");
        yield break;
    }

    [MTest]
    public IEnumerator DeleteAllUserData()
    {
        var logouted = false;
        Autoya.Auth_Logout(() => { logouted = true; }, reason => { });
        yield return WaitUntil(
            () => logouted,
            () => { throw new TimeoutException("failed to logout."); }
        );

        var authenticated = Autoya.Auth_IsAuthenticated();
        True(!authenticated, "not deleted.");

        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to firstBoot."); }
        );
    }

    [MTest]
    public IEnumerator HandleBootAuthFailed()
    {
        Autoya.forceFailFirstBoot = true;

        var logouted = false;
        Autoya.Auth_Logout(() => { logouted = true; }, reason => { });
        yield return WaitUntil(
            () => logouted,
            () => { throw new TimeoutException("failed to logout."); }
        );


        var bootAuthFailHandled = false;
        Autoya.Auth_SetOnBootAuthFailed(
            (code, reason) =>
            {
                bootAuthFailHandled = true;
            }
        );

        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => bootAuthFailHandled,
            () => { throw new TimeoutException("failed to handle bootAuthFailed."); },
            10
        );

        Autoya.forceFailFirstBoot = false;
    }

    [MTest]
    public IEnumerator HandleBootAuthFailedThenAttemptAuthentication()
    {
        Autoya.forceFailFirstBoot = true;

        var logouted = false;
        Autoya.Auth_Logout(() => { logouted = true; }, reason => { });
        yield return WaitUntil(
            () => logouted,
            () => { throw new TimeoutException("failed to handle bootAuthFailed."); }
        );


        var bootAuthFailHandled = false;
        Autoya.Auth_SetOnBootAuthFailed(
            (code, reason) =>
            {
                bootAuthFailHandled = true;
            }
        );

        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => bootAuthFailHandled,
            () => { throw new TimeoutException("failed to handle bootAuthFailed."); },
            10
        );

        Autoya.forceFailFirstBoot = false;

        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to attempt auth."); }
        );
    }

    [MTest]
    public IEnumerator HandleLogoutThenAuthenticationAttemptSucceeded()
    {
        var logouted = false;
        Autoya.Auth_Logout(() => { logouted = true; }, reason => { });
        yield return WaitUntil(
            () => logouted,
            () => { throw new TimeoutException("failed to auth"); }
        );


        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to auth"); }
        );
    }


    [MTest]
    public IEnumerator IntentionalLogout()
    {
        var logouted = false;
        Autoya.Auth_Logout(() => { logouted = true; }, reason => { });
        yield return WaitUntil(
            () => logouted,
            () => { throw new TimeoutException("intentional logout timeout."); }
        );


        var loggedIn = Autoya.Auth_IsAuthenticated();
        True(!loggedIn, "state does not match.");
        yield break;
    }

    [MTest]
    public IEnumerator HandleTokenRefreshFailed()
    {
        Autoya.forceSetHttpCodeAsUnauthorized = true;
        Autoya.forceFailTokenRefresh = true;

        var tokenRefreshFailed = false;
        Autoya.Auth_SetOnRefreshAuthFailed(
            (code, reason) =>
            {
                tokenRefreshFailed = true;
            }
        );

        // forcibly get 401 response.
        Autoya.Http_Get(
            "https://httpbin.org/status/401",
            (conId, resultData) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                Autoya.forceSetHttpCodeAsUnauthorized = false;
            }
        );

        yield return WaitUntil(
            () => tokenRefreshFailed,
            () => { throw new TimeoutException("failed to handle tokenRefreshFailed."); },
            20
        );

        Autoya.forceFailTokenRefresh = false;
    }

    [MTest]
    public IEnumerator HandleTokenRefreshFailedThenAttemptAuthentication()
    {
        Autoya.forceSetHttpCodeAsUnauthorized = true;
        Autoya.forceFailTokenRefresh = true;

        var tokenRefreshFailed = false;
        Autoya.Auth_SetOnRefreshAuthFailed(
            (code, reason) =>
            {
                tokenRefreshFailed = true;
            }
        );

        // forcibly get 401 response.
        Autoya.Http_Get(
            "https://httpbin.org/status/401",
            (conId, resultData) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                Autoya.forceSetHttpCodeAsUnauthorized = false;
            }
        );

        yield return WaitUntil(
            () => tokenRefreshFailed,
            () => { throw new TimeoutException("failed to handle tokenRefreshFailed."); },
            20
        );

        Autoya.forceFailTokenRefresh = false;

        Autoya.Auth_AttemptAuthenticationIfNeed();

        yield return WaitUntil(
            () => Autoya.Auth_IsAuthenticated(),
            () => { throw new TimeoutException("failed to handle tokenRefreshFailed."); },
            15
        );
    }

    [MTest]
    public IEnumerator UnauthorizedThenHttpGet()
    {
        var reauthenticationSucceeded = false;

        // forcibly get 401 response.
        Autoya.Http_Get(
            "https://httpbin.org/status/401",
            (conId, resultData) =>
            {
                // do nothing.
            },
            (conId, code, reason, autoyaStatus) =>
            {
                // these handler will be fired automatically.
                Autoya.Auth_SetOnAuthenticated(
                    () =>
                    {
                        Autoya.Http_Get(
                            "https://httpbin.org/get",
                            (string conId2, string data2) =>
                            {
                                reauthenticationSucceeded = true;
                            },
                            (conId2, code2, reason2, autoyaStatus2) =>
                            {
                                // do nothing.
                            }
                        );
                    }
                );
            }
        );

        yield return WaitUntil(
            () => reauthenticationSucceeded,
            () => { throw new TimeoutException("failed to handle SetOnAuthenticated."); },
            10
        );
    }

    [MTest]
    public IEnumerator AvoidHttpAuthFailCascade()
    {
        Autoya.forceFailAuthentication = true;

        var retryActs = new List<Action>();

        Action authDoneAct = () =>
        {
            retryActs.ForEach(r => r());
            retryActs.Clear();
        };

        Autoya.Auth_SetOnAuthenticated(authDoneAct);


        var conCount = 10;

        var doneConIds = new List<string>();
        var onceFailed = new List<string>();

        var connections = new List<Action>();

        for (var i = 0; i < conCount; i++)
        {
            var index = i;
            var currentConId = i.ToString();
            connections.Add(
                () =>
                {
                    Autoya.Http_Get(
                        "https://httpbin.org/status/200",
                        (conId, resultData) =>
                        {
                            doneConIds.Add(conId);
                        },
                        (conId, code, reason, autoyaStatus) =>
                        {
                            if (autoyaStatus.isAuthFailed)
                            {
                                onceFailed.Add(conId);

                                retryActs.Add(connections[index]);
                            }
                        },
                        null,
                        5,
                        currentConId
                    );
                }
            );
        }

        // 通信の全てが行われればOK
        foreach (var act in connections)
        {
            act();
        }

        // once failed.
        yield return WaitUntil(
            () => onceFailed.Count == conCount,
            () => { throw new TimeoutException("too late."); },
            10
        );

        // refreshの完全なfailまでには8秒以上あるので、ここでフラグを変更しても十分にリトライに間に合うはず
        Autoya.forceFailAuthentication = false;

        // once failed.
        yield return WaitUntil(
            () => doneConIds.Count == conCount,
            () => { throw new TimeoutException("too late."); },
            10
        );
    }

    // [MTest] public IEnumerator AvoidHttpAuthFailCascadeWithAppendOnAuthRefreshed () {
    // 	Autoya.forceFailAuthentication = true;

    // 	var conCount = 10;

    // 	var doneConIds = new List<string>();
    // 	var onceFailed = new List<string>();

    // 	var connections = new List<Action>();

    // 	for (var i = 0; i < conCount; i++) {
    // 		var index = i;
    // 		var currentConId = i.ToString();
    // 		connections.Add(
    // 			() => {
    // 				Autoya.Http_Get(
    // 					"https://httpbin.org/status/200",
    // 					(conId, resultData) => {
    // 						doneConIds.Add(conId);
    // 						Debug.Log("done! currentConId:" + currentConId);
    // 					},
    // 					(conId, code, reason, autoyaStatus) => {
    // 						if (autoyaStatus.isAuthFailed) {
    // 							onceFailed.Add(conId);

    // 							Autoya.Auth_OnAuthenticated += () => {
    // 								connections[index]();
    // 							};

    // 							Debug.Log("リトライを行う。自分自身をリトライ動作にaddする。currentConId:" + currentConId);
    // 						}
    // 					},
    // 					null,
    // 					5,
    // 					currentConId
    // 				);
    // 			}
    // 		);
    // 	}

    // 	// 通信の全てが行われればOK
    // 	foreach (var act in connections) {
    // 		act();
    // 	}

    // 	// once failed.
    // 	yield return WaitUntil(
    // 		() => onceFailed.Count == conCount,
    // 		() => {throw new TimeoutException("too late.");},
    // 		10
    // 	);

    // 	// refreshの完全なfailまでには8秒以上あるので、ここでフラグを変更しても十分にリトライに間に合うはず
    // 	Autoya.forceFailAuthentication = false;

    // 	// once failed.
    // 	yield return WaitUntil(
    // 		() => doneConIds.Count == conCount,
    // 		() => {throw new TimeoutException("too late.");},
    // 		10
    // 	);
    // }


}