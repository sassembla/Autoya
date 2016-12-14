using AutoyaFramework.Representation.Base64;

/**
    this class should be edit by user=game creator.

    最終的には外部から特定のクラス(type)を渡すことで、挙動を制御したい。
*/
namespace AutoyaFramework.Editable {
    public class EncryptionEditable {
        /**
            boot data encryption.
        */
        public static string OnFirstBootRequestEncryption (byte[] boot) {
            
            return Base64.FromBytes(boot);
        }

        /**
            treat received raw-token before save.
        */
        public static byte[] BeforeTokenSaveEncryption (byte[] newToken) {
            return newToken;
        }

        /**
            treat loaded token. 
        */
        public static byte[] OnTokenLoadEncryption (byte[] loaded) {
            return loaded;
        }
        /**
            use for JWT request token hashing.
            渡す値も返す値もまだ未定。
        */
        public static byte[] OnRequestEncryption (byte[] request) {
            return request;
        }
    }

    
}