using UnityEngine;

/*
    表層レイヤ
*/
public class SignInWithAppleTest : MonoBehaviour
{
    public void ButtonPress()
    {
        // ボタンを押してsiwaを作ってる、別にstaticでいいよね、、
        var siwa = new SignInWithApple();
        siwa.Login(OnLogin);
    }

    public void CredentialButton()
    {
        // User id that was obtained from the user signed into your app for the first time.
        // なるほど、必ずエラーになるこのサンプルコード、、、うーんちゃんとサンプルとしてフローを持っときゃいいのに
        var siwa = gameObject.GetComponent<SignInWithApple>();
        siwa.GetCredentialState("<userid>", OnCredentialState);
    }

    private void OnCredentialState(SignInWithApple.CallbackArgs args)
    {
        Debug.Log("HERE User credential state is: " + JsonUtility.ToJson(args));
    }

    private void OnLogin(SignInWithApple.CallbackArgs args)
    {
        Debug.Log("HERE Sign in with Apple login has completed." + JsonUtility.ToJson(args));
    }
}
