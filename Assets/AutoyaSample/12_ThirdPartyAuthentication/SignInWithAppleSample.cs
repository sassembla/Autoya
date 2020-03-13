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
            (isSignup, userId, email, displayName, idToken, userDetectionStatus) =>
            {
                Debug.Log("isSignup:" + isSignup + " userId:" + userId + " email:" + email + " displayName:" + displayName + " idToken:" + idToken + " userDetectionStatus:" + userDetectionStatus);

                // userIdがあれば、どんな状態か取得できる。
                SignInWithApple.GetCredentialState(
                    userId,
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
