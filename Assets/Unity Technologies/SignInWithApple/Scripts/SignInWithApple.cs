using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;



public enum CredentialState
{
    CredentialRevoked,
    CredentialAuthorized,
    CredentialNotFound,
    CredentialTransferred,
}



/*
    ユーザー状態
*/
public enum UserDetectionStatus
{
    LikelyReal,
    Unknown,
    Unsupported
}

/*
    ユーザー情報
*/
[Serializable]
public struct UserInfo
{
    [SerializeField] public string userId;
    [SerializeField] public string email;
    [SerializeField] public string displayName;

    [SerializeField] public string idToken;
    [SerializeField] public string error;

    [SerializeField] public UserDetectionStatus userDetectionStatus;
}








[Serializable]
public class SignInWithApple
{
    /*
        コールバック型
    */
    [Serializable]
    public struct CallbackArgs
    {
        [SerializeField] public bool isSIWAEnabled;

        [SerializeField] public CredentialState credentialState;

        [SerializeField] public UserInfo userInfo;
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
        コールバックのインスタンス
    */
    private static Callback isSIWAEnabledCompletedCallback;
    private static Callback signupCompletedCallback;
    private static Callback credentialStateCallback;



    /*
        ネイティブ側から呼ばれるdelegate定義
    */
    public delegate void Callback(CallbackArgs args);


    private delegate void IsSIWAEnabledCompleted(int result);

    /*
        isSIWAEnabled完了時にネイティブ側から呼ばれる関数
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


    private delegate void SignupCompleted(int result, UserInfo info);

    /*
        signup完了時にネイティブ側から呼ばれる関数
    */
    [MonoPInvokeCallback(typeof(SignupCompleted))]
    private static void SignupCompletedCallback(int result, [MarshalAs(UnmanagedType.Struct)]UserInfo info)
    {
        var args = new CallbackArgs();
        if (result != 0)
        {
            args.userInfo = new UserInfo
            {
                idToken = info.idToken,
                displayName = info.displayName,
                email = info.email,
                userId = info.userId,
                userDetectionStatus = info.userDetectionStatus
            };
        }
        else
        {
            args.userInfo = new UserInfo
            {
                error = info.error
            };
        }

        signupCompletedCallback(args);
        signupCompletedCallback = null;
    }


    private delegate void GetCredentialStateCompleted(CredentialState state);

    [MonoPInvokeCallback(typeof(GetCredentialStateCompleted))]
    private static void GetCredentialStateCallback([MarshalAs(UnmanagedType.SysInt)]CredentialState state)
    {
        var args = new CallbackArgs
        {
            credentialState = state
        };

        credentialStateCallback(args);
        credentialStateCallback = null;
    }



    /*
        SIWAが使用できるかどうか調べる
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
        signupを行う
    */
    public static void SignupOrSignin(Action<bool, string, string, string, string, UserDetectionStatus> onSucceeded, Action<string> onFailed)
    {

        switch (state)
        {
            case State.None:
                break;
            default:
                onFailed("another process running. waiting for end of:" + state);
                return;
        }

        state = State.SignUpProcessing;

        signupCompletedCallback = args =>
        {
            state = State.None;

            var userInfo = args.userInfo; ;

            // success
            if (string.IsNullOrEmpty(userInfo.error))
            {
                /*
                    success
                    {
                        "userId": "000692.40362e95611641bbb392d7dddc6b25ca.1447",
                        "email": "",
                        "displayName": "",
                        "idToken": "eyJraWQiOiI4NkQ4OEtmIiwiYWxnIjoiUlMyNTYifQ.eyJpc3MiOiJodHRwczovL2FwcGxlaWQuYXBwbGUuY29tIiwiYXVkIjoiY29tLmtpYWFraS50ZXN0IiwiZXhwIjoxNTg0MDExMTk1LCJpYXQiOjE1ODQwMTA1OTUsInN1YiI6IjAwMDY5Mi40MDM2MmU5NTYxMTY0MWJiYjM5MmQ3ZGRkYzZiMjVjYS4xNDQ3IiwiY19oYXNoIjoiUms5RHk4aGhvSUhUR2NTWlVjbkFhdyIsImVtYWlsIjoiOHp0OGpteTVieEBwcml2YXRlcmVsYXkuYXBwbGVpZC5jb20iLCJlbWFpbF92ZXJpZmllZCI6InRydWUiLCJpc19wcml2YXRlX2VtYWlsIjoidHJ1ZSIsImF1dGhfdGltZSI6MTU4NDAxMDU5NSwibm9uY2Vfc3VwcG9ydGVkIjp0cnVlfQ.LWDdtt-AS42QbgfO6q2zfe2uJ7rvsQNgUz8phrOO4sltT4fNPMdJDAcdpHj7wuEYUhSoC4lKSTzEyVOSqXzxHNrWah6VEki49vWmNlHObTTdEHyfh6zhjj5Keve5WWO-1s7kmPu6eEFeyz3gAbvRPpck_tTWgx6N6-oijdccTy4jdstAt5mxUtzhT-oPw8LvEC0kLpRhZyOcjfiFsMZ2AFXzkQAbl6JaKdrvSZNcgM-VbzJrfg4b_bS14FAPqKN3ZJ_ksSvyaY3ugI0NBT_rUeINugOoABwk1h1bv7RW4R66Pmg5oAGDH_m3AwKkFkltIbZyAMXsmP3HU6iMr2iquA",
                        "error": "",
                        "userDetectionStatus": 1
                    }  
                */
                var userId = userInfo.userId;
                var email = userInfo.email;
                var displayName = userInfo.displayName;
                var idToken = userInfo.idToken;
                var userDetectionStatus = userInfo.userDetectionStatus;

                if (!string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(email))
                {
                    // signin.
                    onSucceeded(false, userId, email, displayName, idToken, userDetectionStatus);
                    return;
                }

                // signup.
                onSucceeded(true, userId, email, displayName, idToken, userDetectionStatus);
                return;
            }

            /*
                {
                    "userInfo": {
                        "userId": "",
                        "email": "",
                        "displayName": "",
                        "idToken": "",
                        "error": "",
                        "userDetectionStatus": 0
                    }
                    "error": "The operation couldn’t be completed. (com.apple.AuthenticationServices.AuthorizationError error 1001.)"
                }
            */
            // error
            onFailed(userInfo.error);
        };

#if UNITY_EDITOR
        state = State.None;
        onSucceeded(true, "dummy userId", "dummy email", "dummy displayName", "dummy idToken", UserDetectionStatus.LikelyReal);
#elif UNITY_IOS
        IntPtr cback = IntPtr.Zero;
        SignupCompleted d = SignupCompletedCallback;
        cback = Marshal.GetFunctionPointerForDelegate(d);

        SignInWithApple_Signup(cback);
#endif
    }





    /*
        credential state を取得する
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
        ネイティブ側へのブリッジ
    */

#if UNITY_EDITOR
    private static void SignInWithApple_Signup(IntPtr callback)
    {
        // サインアップ処理
    }

    private static void SignInWithApple_GetCredentialState(string userID, IntPtr callback)
    {
        // クレデンシャルの取得処理
    }
#elif UNITY_IOS
    [DllImport("__Internal")]
    private static extern void SignInWithApple_CheckIsSIWAEnabled(IntPtr callback);

    [DllImport("__Internal")]
    private static extern void SignInWithApple_Signup(IntPtr callback);

    [DllImport("__Internal")]
    private static extern void SignInWithApple_GetCredentialState(string userID, IntPtr callback);
#else
    private static void SignInWithApple_CheckIsSIWAEnabled(IntPtr callback){}

    private static void SignInWithApple_Signup(IntPtr callback){}

    private static void SignInWithApple_GetCredentialState(string userID, IntPtr callback){}
#endif

}
