using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
	public GameObject ball;
	Vector3 mousePos;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
	{
		// Get mouse position on screen in 2D
		mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		// Change position to in front of camera
		mousePos.z = 0.0f;
		// Spawn ball on mouse click at mouse position
		if (Input.GetMouseButtonDown(0))
		{
			Instantiate(ball, mousePos, Quaternion.identity);
		}
    }
}
