using System;
using AutoyaFramework.ThirdpartyAuthentication.SignInWithApple;
using UnityEngine;
using UnityEngine.UI;

public class SignInWithAppleSample : MonoBehaviour
{
    public Button SIWAButton;

    public void Awake()
    {
        // check if SIWA is available on this environment.
        var available = SignInWithApple.IsSIWAAvailable();
        Debug.Log("is SIWA Available?:" + available);

        if (!available)
        {
            // SIWA is not available. deactivate the SIWA button. prefer to hide the button if possible.
            SIWAButton.interactable = false;
            return;
        }

        // SIWA is available. activate the SIWA button.
        SIWAButton.interactable = true;
    }

    public void StartSIWA()
    {
        // getting nonce from your application server is good way to get more accurate authentication with SIWA on serverside.
        var nonce = Guid.NewGuid().ToString();// generating nonce here for example.

        /*
            signup / signin with nonce parameter.
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


                // when you have the userId on memory, you can get the credential state of the userId.
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
