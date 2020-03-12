using UnityEngine;

public class SignInWithAppleSample : MonoBehaviour
{
    public void ButtonPress()
    {
        // SIWAが使用可能かどうか
        var avail = SignInWithApple.IsSIWAAvailable();
        Debug.Log("avail:" + avail);

        if (!avail)
        {
            return;
        }

        /*
            サインアップ/サインイン処理からCredential取得処理を連続して行う
        */
        SignInWithApple.SignupOrSignin(
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
