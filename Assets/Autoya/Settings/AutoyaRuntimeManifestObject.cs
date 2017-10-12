
using System;
using UnityEngine;

namespace AutoyaFramework.AppManifest {    
    /**
        Autoyaで使用する動的な設定パラメータに関する型情報。
        アプリケーション内に保存される。

        動的に書き換えることができる。
        初期値を与えることができる。
    */
    [Serializable] public class RuntimeManifestObject {
        [SerializeField] public string resVersion = "0.0.0";
    }
}