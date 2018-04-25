#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_TVOS
// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security
{
    public class AppleTangle
    {
        private static byte[] data = System.Convert.FromBase64String("2FfAmWJjbf9m3Ex7Qxm4UWC2D3sdAQhNPwICGU0uLF1zemBdW11ZX1hfXFldXls3emBeWF1fXVRfXFldDwEITR4ZDAMJDB8JTRkIHwAeTQwBCE0kAw5DXEtdSWtuOGlmfnAsHUMty5oqIBJlM11ya244cE5pdV1772xta2RH6yXrmg4JaGxd7J9dR2vteUa9BCr5G2STmQbgQy3LmiogEhkFAh8EGRRce115a244aW5+YCwd05ke9oO/CWKmFCJZtc9TlBWSBqVNDAMJTQ4IHxkECwQODBkEAgNNHWLwUJ5GJEV3pZOj2NRjtDNxu6ZQTS4sXe9sT11ga2RH6yXrmmBsbGxlM13vbHxrbjhwTWnvbGVd72xpXRRNDB4eGAAIHk0MDg4IHRkMAw4IXls3XQ9cZl1ka244aWt+bzg+XH4ktRvyXnkIzBr5pEBvbmxtbM7vbPjzF2HJKuY2uXtaXqapYiCjeQS8aWt+bzg+XH5dfGtuOGlnfmcsHR1y6O7odvRQKlqfxPYt40G53P1/tV18a244aWd+ZywdHQEITSQDDkNcBAsEDgwZBAIDTSwYGQUCHwQZFFxrbjhwY2l7aXlGvQQq+Rtkk5kG4AlYTngmeDRw3vmam/Hzoj3XrDU9rQ5eGppXakE7hrdiTGO31x50IthJj4a82h2yYiiMSqecABWAith6ehdd72wbXWNrbjhwYmxskmlpbm9sEizF9ZS8pwvxSQZ8vc7WiXZHrnIZBAsEDgwZCE0PFE0MAxRNHQwfGcWxE09Yp0i4tGK7BrnPSU58mszBGhpDDB0dAQhDDgIAQgwdHQEIDgxrXWJrbjhwfmxskmloXW5sbJJdcDTKaGQRei07fHMZvtrmTlYqzrgCPwgBBAwDDghNAgNNGQUEHk0OCB9LXUlrbjhpZn5wLB0dAQhNLggfGSgTciEGPfss5KkZD2Z97izqXufsYGtkR+sl65pgbGxoaG1u72xsbTHiHuwNq3Y2ZEL/35UpJZ0NVfN4mGhtbu9sYm1d72xnb+9sbG2J/MRkcvy2cyo9hmiAMxTpQIZbzzohOIGkdB+YMGO4EjL2n0hu1zjiIDBgnFBLCk3nXgeaYO+is4bOQpQ+BzYJ5nTks5QmAZhqxk9db4V1U5U9ZL5CXeyua2VGa2xoaGpvb13s23fs3txdNYE3aV/hBd7icLMIHpIKMwjRxs4c/yo+OKzCQizelZaOHaCLziFNAgtNGQUITRkFCANNDB0dAQQODAriZdlNmqbBQU0CHdtSbF3h2i6iZUZrbGhoam9se3MFGRkdHldCQhpd72nWXe9uzs1ub2xvb2xvXWBrZFv0IUAV2oDh9rGeGvafG78aXSKstFsSrOo4tMr01F8vlrW4HPMTzD97XXlrbjhpbn5gLB0dAQhNPwICGWqBEFTu5j5NvlWp3NL3ImcGkkaRAwlNDgIDCQQZBAIDHk0CC00YHghH6yXrmmBsbGhobV0PXGZdZGtuOEFNDggfGQQLBA4MGQhNHQIBBA4U2nbQ/i9Jf0eqYnDbIPEzDqUm7XodAQhNLggfGQQLBA4MGQQCA00sGB8MDhkEDghNHhkMGQgACAMZHkNdPcfnuLeJkb1kalrdGBhM");
        private static int[] order = new int[] { 23, 6, 7, 42, 42, 31, 34, 32, 31, 50, 45, 22, 13, 33, 40, 31, 27, 18, 32, 49, 50, 22, 30, 50, 57, 53, 28, 32, 39, 54, 36, 54, 52, 38, 45, 53, 47, 55, 40, 54, 49, 56, 52, 55, 54, 57, 56, 47, 53, 50, 53, 58, 53, 58, 55, 57, 57, 59, 58, 59, 60 };
        private static int key = 109;

        public static readonly bool IsPopulated = true;

        public static byte[] Data()
        {
            if (IsPopulated == false)
                return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
#endif
