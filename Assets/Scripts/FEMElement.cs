using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEMElement : MonoBehaviour
{
	/*
		2----------------3
		|				 |
		|				 |
		|				 |
		|				 |
		|				 |
		0----------------1
	*/

	GameObject[] verts = new GameObject[4];	// Four nodes
	int[] tris = new int[6]; // Six points for 2 triangles

	void CalculateTriangles()
	{
		// Lower left - 0, 2, 1

		// Upper right - 2, 3, 1
	}
}
