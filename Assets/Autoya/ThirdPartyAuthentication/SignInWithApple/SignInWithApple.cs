using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace AutoyaFramework.ThirdpartyAuthentication.SignInWithApple
{

    public enum AuthorizationScope
    {
        Email,
        FullName,
        EmailAndFullName
    }

    /*
        state of the user credential given by GetCredentialState method.
    */
    public enum CredentialState
    {
        CredentialRevoked,
        CredentialAuthorized,
        CredentialNotFound,
        CredentialTransferred,
    }



    /*
        status of the user detection given by SignupOrSignin method.
    */
    public enum UserDetectionStatus
    {
        LikelyReal,
        Unknown,
        Unsupported
    }

    /*
        internal user info which contains error status.
    */
    [Serializable]
    public struct _UserInfo
    {
        [SerializeField] public string userId;
        [SerializeField] public string email;
        [SerializeField] public string displayName;

        [SerializeField] public string authorizationCode;
        [SerializeField] public string idToken;
        [SerializeField] public int errorCode;
        [SerializeField] public string reason;

        [SerializeField] public UserDetectionStatus userDetectionStatus;
    }

    /*
        public information of user given by SignupOrSignin method.
    */
    public struct UserInfo
    {
        public readonly string userId;
        public readonly string email;
        public readonly string displayName;

        public readonly string authorizationCode;
        public readonly string idToken;
        public readonly UserDetectionStatus userDetectionStatus;


        public UserInfo(string authorizationCode, string userId, string email, string displayName, string idToken, UserDetectionStatus userDetectionStatus)
        {
            this.authorizationCode = authorizationCode;
            this.userId = userId;
            this.email = email;
            this.displayName = displayName;
            this.idToken = idToken;
            this.userDetectionStatus = userDetectionStatus;
        }
    }




    [Serializable]
    public class SignInWithApple
    {
        /*
            whole data contained callback data type.
        */
        [Serializable]
        public struct CallbackArgs
        {
            [SerializeField] public bool isSIWAEnabled;

            [SerializeField] public CredentialState credentialState;

            [SerializeField] public _UserInfo userInfo;
        }

        private enum State
        {
            None,
            IsSIWAEnabledProcessing,
            SignUpProcessing,
            GettingCredential,
        }

        private static State state = State.None;

        /*
            callback delegate definition which will be called from native layer.
        */
        public delegate void Callback(CallbackArgs args);

        /*
            callbacks.
        */
        private static Callback isSIWAEnabledCompletedCallback;
        private static Callback signupCompletedCallback;
        private static Callback credentialStateCallback;

        /*
            result type definition.
        */
        private delegate void IsSIWAEnabledCompleted(int result);

        /*
            result callback.
        */
        [MonoPInvokeCallback(typeof(IsSIWAEnabledCompleted))]
        private static void IsSIWAEnabledCompletedCallback(int result)
        {
            var args = new CallbackArgs();
            if (result == 1)
            {
                args.isSIWAEnabled = true;
            }
            else
            {
                args.isSIWAEnabled = false;
            }

            isSIWAEnabledCompletedCallback(args);
            isSIWAEnabledCompletedCallback = null;
        }


        /*
            result type definition.
        */
        private delegate void SignupCompleted(int result, _UserInfo info);

        /*
            result callback.
        */
        [MonoPInvokeCallback(typeof(SignupCompleted))]
        private static void SignupCompletedCallback(int result, [MarshalAs(UnmanagedType.Struct)] _UserInfo info)
        {
            var args = new CallbackArgs();
            if (result != 0)
            {
                args.userInfo = new _UserInfo
                {
                    authorizationCode = info.authorizationCode,
                    idToken = info.idToken,
                    displayName = info.displayName,
                    email = info.email,
                    userId = info.userId,
                    userDetectionStatus = info.userDetectionStatus
                };
            }
            else
            {
                args.userInfo = new _UserInfo
                {
                    errorCode = info.errorCode,
                    reason = info.reason
                };
            }

            signupCompletedCallback(args);
            signupCompletedCallback = null;
        }

        /*
            result type definition.
        */
        private delegate void GetCredentialStateCompleted(CredentialState state);

        /*
            result callback.
        */
        [MonoPInvokeCallback(typeof(GetCredentialStateCompleted))]
        private static void GetCredentialStateCallback([MarshalAs(UnmanagedType.SysInt)] CredentialState state)
        {
            var args = new CallbackArgs
            {
                credentialState = state
            };

            credentialStateCallback(args);
            credentialStateCallback = null;
        }



        /*
            check if SIWA is available or not on this environment.
        */
        public static bool IsSIWAAvailable()
        {
            switch (state)
            {
                case State.None:
                    break;
                default:
                    return false;
            }

            state = State.IsSIWAEnabledProcessing;

            var result = false;
            isSIWAEnabledCompletedCallback = args =>
            {
                state = State.None;

                result = args.isSIWAEnabled;
            };

#if UNITY_EDITOR
            state = State.None;

            // iOSであればtrueを、Androidであればfalseを返す。
#if UNITY_IOS
            return true;
#elif UNITY_ANDROID
            return false;
#endif


#elif UNITY_IOS
            IntPtr cback = IntPtr.Zero;
            IsSIWAEnabledCompleted d = IsSIWAEnabledCompletedCallback;
            cback = Marshal.GetFunctionPointerForDelegate(d);

            SignInWithApple_CheckIsSIWAEnabled(cback);
#endif
            return result;
        }


        /*
            signup or signin with nonce.
            nonce is the parameter which help more accurate validation of idToken on your application server.
            
            recommended way of the SIWA authentication with nonce is below.
            1. client gets the nonce from your application server. server should record generated nonce.
            2. client should use the nonce to execute SignupOrSignin(nonce, (isSignup, SIWA userInfo) => {do sending idToken and other data to your server}) method.
            3. application server receives the idToken included the nonce. do delete record of the nonce and validate idToken with nonce. this way gives you to accurate JWT verification and avoid replay attack.
        */
        public static void SignupOrSignin(string nonce, AuthorizationScope authorizationScope, Action<bool, UserInfo> onSucceeded, Action<int, string> onFailed)
        {

            switch (state)
            {
                case State.None:
                    break;
                default:
                    onFailed(-1, "another process running. waiting for end of:" + state);
                    return;
            }

            state = State.SignUpProcessing;

            signupCompletedCallback = args =>
            {
                state = State.None;


                var userInfo = args.userInfo;

                // success
                if (userInfo.errorCode == 0)
                {
                    /*
                        success
                        {
                            "userId": "000692.40362e95611641bbb392d7dddc6b25ca.1447",
                            "email": "xxx@xx.x",
                            "displayName": "xxxxx",
                            "authorizationCode": "c4902247f521c4104ba925bbc17143b8c.0.nwzs.l8YIwin6RbYr9aYGlRMoQg",
                            "idToken": "eyJraWQiOiI4NkQ4OEtmIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLmtpYWFraS50ZXN0IiwiZXhwIjoxNTg0MDExMTk1LCJpYXQiOjE1ODQwMTA1OTUsInN1YiI6IjAwMDY5Mi40MDM2MmU5NTYxMTY0MWJiYjM5MmQ3ZGRkYzZiMjVjYS4xNDQ3IiwiY19oYXNoIjoiUms5RHk4aGhvSUhUR2NTWlVjbkFhdyIsImVtYWlsIjoiOHp0OGpteTVieEBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTU4NDAxMDU5NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.LWDdtt-AS42QbgfO6q2zfe2uJ7rvsQNgUz8phrOO4sltT4fNPMdJDAcdpHj7wuEYUhSoC4lKSTzEyVOSqXzxHNrWah6VEki49vWmNlHObTTdEHyfh6zhjj5Keve5WWO-1s7kmPu6eEFeyz3gAbvRPpck_tTWgx6N6-oijdccTy4jdstAt5mxUtzhT-oPw8LvEC0kLpRhZyOcjfiFsMZ2AFXzkQAbl6JaKdrvSZNcgM-VbzJrfg4b_bS14FAPqKN3ZJ_ksSvyaY3ugI0NBT_rUeINugOoABwk1h1bv7RW4R66Pmg5oAGDH_m3AwKkFkltIbZyAMXsmP3HU6iMr2iquA",
                            "error": "",
                            "userDetectionStatus": 1
                        }
                    */

                    // Debug.Log("data:" + JsonUtility.ToJson(userInfo));

                    var publicUserInfo = new UserInfo(
                        userInfo.authorizationCode,
                        userInfo.userId,
                        userInfo.email,
                        userInfo.displayName,
                        userInfo.idToken,
                        userInfo.userDetectionStatus
                    );

                    // check if requested authorizationScope data is contained or not.
                    // if requested but not contained, the request is not first one. 
                    // determine the result as "signin".
                    var isSignin = false;
                    switch (authorizationScope)
                    {
                        case AuthorizationScope.Email:
                            isSignin = string.IsNullOrEmpty(publicUserInfo.email);
                            break;
                        case AuthorizationScope.FullName:
                            isSignin = string.IsNullOrEmpty(publicUserInfo.displayName);
                            break;
                        case AuthorizationScope.EmailAndFullName:
                            isSignin = string.IsNullOrEmpty(publicUserInfo.email) && string.IsNullOrEmpty(publicUserInfo.displayName);
                            break;
                    }

                    if (isSignin)
                    {
                        // signin.
                        onSucceeded(false, publicUserInfo);
                        return;
                    }

                    // signup.
                    onSucceeded(true, publicUserInfo);
                    return;
                }

                /*
                    {
                        "userInfo": {
                            "userId": "",
                            "email": "",
                            "displayName": "",
                            "authorizationCode": "",
                            "idToken": "",
                            "error": "",
                            "userDetectionStatus": 0
                        }
                        "error": "The operation couldn’t be completed. (com.apple.AuthenticationServices.AuthorizationError error 1001.)"
                    }
                */
                // error
                onFailed(userInfo.errorCode, userInfo.reason);
            };

#if UNITY_EDITOR
            state = State.None;
            onSucceeded(
                true,
                new UserInfo(
                    "dummy authorizationCode",
                    "dummy userId",
                    "dummy email",
                    "dummy displayName",
                    "dummy idToken",
                    UserDetectionStatus.LikelyReal
                )
            );
#elif UNITY_IOS
            IntPtr cback = IntPtr.Zero;
            SignupCompleted d = SignupCompletedCallback;
            cback = Marshal.GetFunctionPointerForDelegate(d);

            SignInWithApple_Signup(nonce, (int)authorizationScope,  cback);
#endif
        }





        /*
            get credential state of SIWA id.
        */
        public static void GetCredentialState(string siwaId, Action<CredentialState> onSucceeded, Action<string> onFailed)
        {
            switch (state)
            {
                case State.None:
                    break;
                default:
                    onFailed("another process running. waiting for end of:" + state);
                    return;
            }

            if (string.IsNullOrEmpty(siwaId))
            {
                onFailed("siwaId is null or empty. please set valid value.");
                return;
            }

            state = State.GettingCredential;
            credentialStateCallback = args =>
            {
                state = State.None;
                var credentialStateValue = args.credentialState;
                var credState = (CredentialState)Enum.ToObject(typeof(CredentialState), credentialStateValue);
                onSucceeded(credState);
            };

#if UNITY_EDITOR
            state = State.None;
            onSucceeded(CredentialState.CredentialAuthorized);
#elif UNITY_IOS
            GetCredentialStateCompleted d = GetCredentialStateCallback;
            IntPtr cback = Marshal.GetFunctionPointerForDelegate(d);

            SignInWithApple_GetCredentialState(siwaId, cback);
#endif
        }



        /*
            bridges for calling native layer from Unity.
        */

#if UNITY_EDITOR
        // nothing to call native layer.
#elif UNITY_IOS
        [DllImport("__Internal")]
        private static extern void SignInWithApple_CheckIsSIWAEnabled(IntPtr callback);

        [DllImport("__Internal")]
        private static extern void SignInWithApple_Signup(string nonce, int scope, IntPtr callback);

        [DllImport("__Internal")]
        private static extern void SignInWithApple_GetCredentialState(string userID, IntPtr callback);
#else
        // nothing to call native layer.
#endif
    }
}