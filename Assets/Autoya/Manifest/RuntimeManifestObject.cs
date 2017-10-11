
using System;
using UnityEngine;

namespace AutoyaFramework.AppManifest {    
    /**
        アプリケーションの動的な設定パラメータに関する型情報。
        アプリケーション内にストアされる。

        動的に書き換えることができる。
    */
    [Serializable] public class RuntimeManifestObject {
        [SerializeField] public string resVersion = "0.0.0";
    }
}