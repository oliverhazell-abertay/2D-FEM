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
	public float[,] D;  // Elasticity Matrix
	float ab4;
	public float E = 31200000.0f;  // Young's Modulus of Elasticity - 31.2x10^6 psi
	public float v = 0.3f; // Poisson's Ratio
	float[,] k; // Stiffness matrix
	public float u1, u2, u3, u4; // Node x values
	float v1, v2, v3, v4; // Node y values

	void Start()
	{
		width = nodes[1].transform.localPosition.x - nodes[0].transform.localPosition.x;
		height = nodes[3].transform.localPosition.y - nodes[1].transform.localPosition.y;
		a = width / 2;
		b = height / 2;
		ab4 = 4 * (a * b);  // Area
		CalculateD();

		u1 = D[0, 0];
		u2 = D[0, 1];
		u3 = D[2, 2];
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

	public float[,] MultiplyS(float[,] matrix, float scalar)
	{
		float[,] result = new float[3, 3];

		for (int i = 0; i < 3; i++)
			for (int j = 0; j < 3; j++)
				result[i, j] = matrix[i, j] * scalar;

		return result;
	}

	public void CalculateD()
	{
		D = new float[3, 3]{{1.0f, v, 0.0f},
							{v, 1.0f, 0.0f},
							{0.0f, 0.0f, (1 - v)/2} };
		float Eover = E / (1 - (v * v));
		D = MultiplyS(D, Eover);
	}

	public float[,] CalculateB(float x, float y)
	{
		float[,] tempB;
		tempB = new float[3, 8];
		float c = 1 / ab4;

		// u1 column
		B[0, 0] = (y - v4) * c;
		B[1, 0] = 0.0f;
		B[2, 0] = (x - u2) *c;

		// v1 column
		B[0, 1] = 0.0f;
		B[1, 1] = (x - u2) * c;
		B[2, 1] = (y - v4) * c;

		// u2 column
		B[0, 2] = (v3 - y) *c;
		B[1, 2] = 0.0f;
		B[2, 2] = (u1 - x) *c;

		// v2 column
		B[0, 3] = 0.0f;
		B[1, 3] = (u1 - x) * c;
		B[2, 3] = (v3 - y) * c;

		// u3 column
		B[0, 4] = (y - v2) * c;
		B[1, 4] = 0.0f;
		B[2, 4] = (x - u4) * c;

		// v3 column
		B[0, 5] = 0.0f;
		B[1, 5] = (x - u4) * c;
		B[2, 5] = (y - v2) * c;

		// u4 column
		B[0, 6] = (v1 - y) * c;
		B[1, 6] = 0.0f;
		B[2, 6] = (u3 - x) * c;

		// v4 column
		B[0, 7] = 0.0f;
		B[1, 7] = (u3 - x) * c;
		B[2, 7] = (v1 - y) *c;

		return tempB;
	}

	// Return transpose of matrix
	public float[,] Transpose(float[,] matrix)
	{
		int matrixWidth = matrix.GetLength(0);
		int matrixHeight = matrix.GetLength(1);
		float[,] transposed = new float[matrixWidth, matrixHeight];


		return transposed;
	}

	public void CalculateK()
	{

	}
}
