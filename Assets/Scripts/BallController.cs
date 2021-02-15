using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
	public float thrust;
	public Vector2 trag;
	public Rigidbody2D rb;
	// Start is called before the first frame update
	void Start()
    {
		rb = GetComponent<Rigidbody2D>();
	}

    // Update is called once per frame
    void Update()
    {
		//if (Input.GetKeyDown(KeyCode.W))
		//{
		//	rb.AddForce(trag * thrust);
		//}
	}
}
