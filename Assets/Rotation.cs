using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	float count = 10;
	// Update is called once per frame
	void Update () {
		gameObject.transform.Rotate(new Vector3(0,0,count));
		// count++;
	}
}
