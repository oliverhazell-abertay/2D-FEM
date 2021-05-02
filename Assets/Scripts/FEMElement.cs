using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FEMElement : MonoBehaviour
{
	MeshRenderer meshRenderer;
	MeshFilter meshFilter;

	public GameObject[] nodes = new GameObject[4]; // Four nodes
	Vector3[] verts = new Vector3[4];


	/*
		3----------------2
		|				 |
		|				 |
		|				 | 2b
		|				 |
		|				 |
		0----------------1
				2a
	*/

	float width;
	float height;
	float a;	// Half width
	float b;    // Half height
	float area;
	float[,] B;  // Strain/Displacement Matrix
	public float[,] D;	// Elasticity Matrix
	float ab4;
	public float E = 31200000.0f;  // Young's Modulus of Elasticity - 31.2x10^6 psi
	public float v = 0.3f; // Poisson's Ratio
	float[,] k;	// Stiffness matrix

	void Start()
	{
		width = nodes[1].transform.localPosition.x - nodes[0].transform.localPosition.x;
		height = nodes[3].transform.localPosition.y - nodes[1].transform.localPosition.y;
		a = width / 2;
		b = height / 2;
		ab4 = 4 * (a * b);

		D = new float[3, 3]{{1.0f, v, 0.0f}, 
							{v, 1.0f, 0.0f},
							{0.0f, 0.0f, (1 - v)/2} };
		B = new float[3, 8];
		k = new float[8, 8];
	}

	public void DoMesh()
	{
		meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
		meshFilter = gameObject.GetComponent<MeshFilter>();

		Mesh mesh = new Mesh();

		Vector3[] vertices = new Vector3[4]
		{
			nodes[0].transform.localPosition,
			nodes[1].transform.localPosition,
			nodes[2].transform.localPosition,
			nodes[3].transform.localPosition
		};
		mesh.vertices = vertices;

		int[] tris = new int[6]
		{
            // lower left triangle
            0, 3, 1,
            // upper right triangle
            3, 2, 1
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
