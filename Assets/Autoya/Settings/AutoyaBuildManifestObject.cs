using System;
using UnityEngine;

namespace AutoyaFramework.AppManifest
{
    /**
       Autoyaで使用する静的な設定パラメータに関する型情報。
       Resourcesに保存される。

       動的に書き換えることができない。
       初期値を与えることができる。
   */

    [Serializable]
    public class BuildManifestObject
    {
        [SerializeField] public string appVersion = "1.0.0";
        [SerializeField] public string buildNo;
        [SerializeField] public string buildMessage;
        [SerializeField] public string buildDate;

        // Unity cloudbuild compatible build parameters.
        [SerializeField] public string scmCommitId;
        [SerializeField] public string scmBranch;
        [SerializeField] public string buildNumber;
        [SerializeField] public string buildStartTime;
        [SerializeField] public string projectId;
        [SerializeField] public string bundleId;
        [SerializeField] public string unityVersion;
        [SerializeField] public string xcodeVersion;
        [SerializeField] public string cloudBuildTargetName;
    }
}