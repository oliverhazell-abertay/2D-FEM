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

	public GameObject[] nodes = new GameObject[4]; // Four nodes
	Vector3[] verts = new Vector3[4];
	//public int[] tris = new int[6]; // Six points for 2 triangles

	//void CalculateTriangles()
	//{
	//	tris = new int[6] {
	//		// Lower left - 0, 2, 1
	//		0, 2, 1,
	//		// Upper right - 2, 3, 1
	//		2, 3, 1
	//	};
	//}

	public void DoMesh()
	{
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

		MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

		Mesh mesh = new Mesh();

		Vector3[] vertices = new Vector3[4]
		{
			nodes[0].transform.position,
			nodes[1].transform.position,
			nodes[2].transform.position,
			nodes[3].transform.position
		};
		mesh.vertices = vertices;

		int[] tris = new int[6]
		{
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
		};
		mesh.triangles = tris;

		Vector3[] normals = new Vector3[4]
		{
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward,
			-Vector3.forward
		};
		mesh.normals = normals;

		Vector2[] uv = new Vector2[4]
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(0, 1),
			new Vector2(1, 1)
		};
		mesh.uv = uv;

		meshFilter.mesh = mesh;
	}
}
