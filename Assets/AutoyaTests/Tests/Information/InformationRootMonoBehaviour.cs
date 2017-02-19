using System;
using UnityEngine;

public class InformationRootMonoBehaviour : MonoBehaviour {
    public void OnImageTapped (Tag tag, string key) {
        Debug.LogError("fmmm,,,, tag:" + tag + " key:" + key);
    }
}