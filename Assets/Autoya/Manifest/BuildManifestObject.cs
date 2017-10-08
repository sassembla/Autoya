using System;
using UnityEngine;

namespace AutoyaFramework.AppManifest {
    [Serializable] public class BuildManifestObject {
        [SerializeField] public string buildCount;
        [SerializeField] public string buildComment;
        
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