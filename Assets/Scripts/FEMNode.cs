using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEMNode : MonoBehaviour
{
	public Vector2 Position;
	public bool le, re, ue, de;
	public bool corner;
	public bool frameDone = false;

	public float pressureLimit = 20.0f;
	public float currentForceApplied = 0.0f;

	public GameObject leftAdj, rightAdj, upAdj, downAdj;
	public float neighbourCount;

    // Start is called before the first frame update
    void Start()
    {
		//le = false;
		//re = false;
		//ue = false;
		//de = false;
		//corner = false;

	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
