using System;
using System.Collections;
using UnityEngine;

public class SignInWithAppleSample : MonoBehaviour
{
    public void ButtonPress()
    {
        /*
            さてどんなケースを考える？
            ・SignInWithAppleを使ってsignupする
            ・SignInWithAppleを使ってsigninする
            ・保持しているsiwaIdから状態を知る
            こんなとこか、

            revokeとかは端末からできちゃうんで、知りたいかどうか。

        */
        StartCoroutine(Go());
    }

    private IEnumerator Go()
    {
        // SIWAが使用可能かどうか
        var avail = SignInWithApple.IsSIWAAvailable();
        Debug.Log("avail:" + avail);

        if (!avail)
        {
            yield break;
        }

        var id = "000692.40362e95611641bbb392d7dddc6b25ca.1447";
        var done = false;
        // userIdがあれば、どんな状態か取得できる。
        SignInWithApple.GetCredentialState(
            id,
            credentialState =>
            {
                /*
                    CredentialRevoked,
                    CredentialAuthorized,
                    CredentialNotFound,
                    CredentialTransferred,
                */
                Debug.Log("credentialState:" + credentialState);
                done = true;
            },
            reason =>
            {
                Debug.Log("reason:" + reason);
            }
        );
        while (!done)
        {
            yield return null;
        }

        // nonceをサーバから取得し、サーバ側で検証できるようにする。ここではlocalで作っている。
        var nonce = Guid.NewGuid().ToString();

        /*
            サインアップ/サインイン処理からCredential取得処理を連続して行う
        */
        SignInWithApple.SignupOrSignin(
            nonce,
            (isSignup, userInfo) =>
            {
                Debug.Log(
                    "isSignup:" + isSignup +
                    " authorizationCode:" + userInfo.authorizationCode +
                    " userId:" + userInfo.userId +
                    " email:" + userInfo.email +
                    " displayName:" + userInfo.displayName +
                    " idToken:" + userInfo.idToken +
                    " userDetectionStatus:" + userInfo.userDetectionStatus
                );
                /*
                    userDetectionStatus

                    LikelyReal
                        The user appears to be a real person, and you can treat this account as a valid user. You can skip any additional fraud verification checks or CAPTCHAs that your app normally uses.

                    Unknown
                        The system can’t determine whether the user is a real person. The server may return this value if status determination is taking too long. Treat this user as any other account with limited information that requires additional verification steps. Don’t block service, because the user may be a real person.

                    Unsupported
                        Real user status is only supported on iOS at this time. macOS, watchOS, tvOS, and web-based apps all return Unsupported.
                */

                // userIdがあれば、どんな状態か取得できる。
                SignInWithApple.GetCredentialState(
                    userInfo.userId,
                    credentialState =>
                    {
                        /*
                            CredentialRevoked,
                            CredentialAuthorized,
                            CredentialNotFound,
                            CredentialTransferred,
                        */
                        Debug.Log("credentialState:" + credentialState);
                    },
                    reason =>
                    {
                        Debug.Log("reason:" + reason);
                    }
                );
            },
            reason =>
            {
                Debug.Log("reason:" + reason);
            }
        );
    }
}
