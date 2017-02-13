using System;
using System.IO;
using AutoyaFramework;
using AutoyaFramework.Settings.Auth;
using Miyamasu;
using UnityEngine;

/**
	test for authorization flow control.
*/
public class AuthImplementationTests : MiyamasuTestRunner {
	private void DeleteAllData (string path) {
		if (Directory.Exists(path)) {
			Directory.Delete(path, true);
		}
	}
	
	[MSetup] public void Setup () {
		Autoya.ResetAllForceSetting();

		var authorized = false;
		Action onMainThread = () => {
			var dataPath = Application.persistentDataPath;

			var fwPath = Path.Combine(dataPath, AuthSettings.AUTH_STORED_FRAMEWORK_DOMAIN);
            DeleteAllData(fwPath);

			Autoya.TestEntryPoint(dataPath);

			Autoya.Auth_SetOnAuthenticated(
				() => {
					authorized = true;
				}
			);
		};

		RunOnMainThread(onMainThread);
		
		WaitUntil(
			() => {
				return authorized;
			},
			5,
			"timeout in setup."
		);

		Assert(Autoya.Auth_IsAuthenticated(), "not logged in.");
	}

    [MTeardown] public void Teardown () {
        RunOnMainThread(Autoya.Shutdown);
		Autoya.ResetAllForceSetting();
    }

	
	[MTest] public void WaitDefaultAuthenticate () {
		Assert(Autoya.Auth_IsAuthenticated(), "not yet logged in.");
	}

	[MTest] public void DeleteAllUserData () {
		Autoya.Auth_DeleteAllUserData();
		
		var authenticated = Autoya.Auth_IsAuthenticated();
		Assert(!authenticated, "not deleted.");

		RunOnMainThread(
			() => {
				Autoya.Auth_AttemptAuthentication();
			}
		);

		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to firstBoot."
		);
	}

	[MTest] public void HandleBootAuthFailed () {
		Autoya.forceFailFirstBoot = true;

		Autoya.Auth_DeleteAllUserData();
		
		var bootAuthFailHandled = false;
		Autoya.Auth_SetOnBootAuthFailed(
			(code, reason) => {
				bootAuthFailHandled = true;
			}
		);

		RunOnMainThread(
			() => Autoya.Auth_AttemptAuthentication()
		);
		
		WaitUntil(
			() => bootAuthFailHandled,
			10,
			"failed to handle bootAuthFailed."
		);
		
		Autoya.forceFailFirstBoot = false;
	}

	[MTest] public void HandleBootAuthFailedThenAttemptAuthentication () {
		Autoya.forceFailFirstBoot = true;

		Autoya.Auth_DeleteAllUserData();
		
		var bootAuthFailHandled = false;
		Autoya.Auth_SetOnBootAuthFailed(
			(code, reason) => {
				bootAuthFailHandled = true;
			}
		);
		
		RunOnMainThread(
			() => Autoya.Auth_AttemptAuthentication()
		);
		
		WaitUntil(
			() => bootAuthFailHandled,
			10,
			"failed to handle bootAuthFailed."
		);
		
		Autoya.forceFailFirstBoot = false;

		RunOnMainThread(
			() => Autoya.Auth_AttemptAuthentication()
		);

		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to attempt auth."
		);
	}
	
	[MTest] public void HandleLogoutThenAuthenticationAttemptSucceeded () {
		Autoya.Auth_Logout();

		RunOnMainThread(
			() => Autoya.Auth_AttemptAuthentication()
		);

		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to auth"
		);
	}

	
	[MTest] public void IntentionalLogout () {
		Autoya.Auth_Logout();
		
		var loggedIn = Autoya.Auth_IsAuthenticated();
		Assert(!loggedIn, "state does not match.");
	}

	[MTest] public void HandleTokenRefreshFailed () {
		Autoya.forceFailTokenRefresh = true;
		
		var tokenRefreshFailed = false;
		Autoya.Auth_SetOnRefreshAuthFailed(
			(code, reason) => {
				tokenRefreshFailed = true;
			}
		);

		// forcibly get 401 response.
		Autoya.Http_Get(
			"https://httpbin.org/status/401", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				// do nothing.
			}
		);

		WaitUntil(
			() => tokenRefreshFailed,
			10,
			"failed to handle tokenRefreshFailed."
		);
		
		Autoya.forceFailTokenRefresh = false;
	}

	[MTest] public void HandleTokenRefreshFailedThenAttemptAuthentication () {
		Autoya.forceFailTokenRefresh = true;
		
		var tokenRefreshFailed = false;
		Autoya.Auth_SetOnRefreshAuthFailed(
			(code, reason) => {
				tokenRefreshFailed = true;
			}
		);
		
		// forcibly get 401 response.
		Autoya.Http_Get(
			"https://httpbin.org/status/401", 
			(string conId, string resultData) => {
				// do nothing.
			},
			(conId, code, reason, autoyaStatus) => {
				// do nothing.
			}
		);

		WaitUntil(
			() => tokenRefreshFailed,
			10,
			"failed to handle tokenRefreshFailed."
		);
		
		Autoya.forceFailTokenRefresh = false;

		RunOnMainThread(
			() => {
				Autoya.Auth_AttemptAuthentication();
			}
		);
		
		WaitUntil(
			() => Autoya.Auth_IsAuthenticated(),
			5,
			"failed to handle tokenRefreshFailed."
		);
	}
}