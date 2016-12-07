/*
    JWT representation for Autoya.
*/
namespace AutoyaFramework.Representation {
    public class JWT {
        public static bool Validate (string token, string safehost) {
            // payloadを開けてissとexpをみる。
            // サーバ側で作成したtokenを開けられる性能を持つ必要がある。
            return true;
        }

        public static byte[] Create (string request, byte[] secret) {
            // リクエストからJWTを作成する必要がある。requestに関わるものとしては、ゲームのパラメータとかその辺一式。
            return new byte[0];
        }
    }
}