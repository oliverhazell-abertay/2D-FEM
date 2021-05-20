

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
	float area;
	float[,] B;  // Strain/Displacement Matrix
	public float[,] D;  // Elasticity Matrix
	public float E = 31200000.0f;  // Young's Modulus of Elasticity - 31.2x10^6 psi
	public float v = 0.3f; // Poisson's Ratio
	public float[,] Ke; // Stiffness matrix
	public float u1, u2, u3, u4; // Node x values
	public float v1, v2, v3, v4; // Node y values
	//float t = 1.0f; // Thickness
	public GameObject leftAdj, rightAdj, upAdj, downAdj; // Neighbour elements
	public float neighbourCount;
	public float midX, midY;    // Midpoint coords
	public bool perturbed = false;
	
	void Start()
	{
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
		// Node 0 (u1, v1)
		u1 = nodes[0].transform.localPosition.x;
		v1 = nodes[0].transform.localPosition.y;
		// Node 1 (u2, v2)
		u2 = nodes[1].transform.localPosition.x;
		v2 = nodes[1].transform.localPosition.y;
		// Node 2 (u3, v3)
		u3 = nodes[2].transform.localPosition.x;
		v3 = nodes[2].transform.localPosition.y;
		// Node 3 (u4, v4)
		u4 = nodes[3].transform.localPosition.x;
		v4 = nodes[3].transform.localPosition.y;

		// Get area
		area = GetArea();

		// Get Elasticity matrix (stays constant)
		CalculateD();

		// Get Stress-Displacement matrix (B) and Element stiffness matrix (Ke)
		// Needs to be recalculated after every collision/deformation
		B = CalculateB();
		CalculateKe();
	}

	// Called every frame
	void Update()
	{
		// Update node positions
		// Node 0 (u1, v1)
		u1 = nodes[0].transform.localPosition.x;
		v1 = nodes[0].transform.localPosition.y;
		// Node 1 (u2, v2)
		u2 = nodes[1].transform.localPosition.x;
		v2 = nodes[1].transform.localPosition.y;
		// Node 2 (u3, v3)
		u3 = nodes[2].transform.localPosition.x;
		v3 = nodes[2].transform.localPosition.y;
		// Node 3 (u4, v4)
		u4 = nodes[3].transform.localPosition.x;
		v4 = nodes[3].transform.localPosition.y;

		// Calculate midpoint
		midX = (nodes[0].transform.position.x + nodes[1].transform.position.x) / 2;
		midY = (nodes[0].transform.position.y + nodes[2].transform.position.y) / 2;

		// Calculate mesh
		DoMesh();
	}

	// Recalculate B, Ke, and area when nodes move from collision
	// Called with deform function in FEMShape.cs
	public void RefreshMatrices()
	{
		area = GetArea();
		B = CalculateB();
		CalculateKe();
	}

	// Calculate mesh vertices
	public void DoMesh()
	{
		meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
		meshFilter = gameObject.GetComponent<MeshFilter>();
		Vector3 shapeScale = gameObject.transform.parent.transform.localScale;

		Mesh mesh = new Mesh();

		Vector3[] vertices = new Vector3[4]
		{
			Vector3.Scale(nodes[0].transform.localPosition, shapeScale),
			Vector3.Scale(nodes[1].transform.localPosition, shapeScale),
			Vector3.Scale(nodes[2].transform.localPosition, shapeScale),
			Vector3.Scale(nodes[3].transform.localPosition, shapeScale)
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

	// Calculate elasticity matrix
	public void CalculateD()
	{
		D = new float[3, 3]{{1.0f, v, 0.0f},
							{v, 1.0f, 0.0f},
							{0.0f, 0.0f, (1 - v)/2} };

		// Young's modulus / 1 - Poisson's ratio^2
		float Eover = E / (1 - (v * v));

		D = MultiplyMatrixByScalar(D, Eover);
	}

	// Calculate stress-displacement matrix
	public float[,] CalculateB()
	{
		float[,] tempB;
		tempB = new float[3, 8];
		float c = 1 / (2 * area);	// 1 / 2A
		// u1 column
		tempB[0, 0] = (v1 - v4) * c;
		tempB[1, 0] = 0.0f;
		tempB[2, 0] = (u1 - u2) * c;

		// v1 column
		tempB[0, 1] = 0.0f;
		tempB[1, 1] = (u1 - u2) * c;
		tempB[2, 1] = (v1 - v4) * c;

		// u2 column
		tempB[0, 2] = (v3 - v2) * c;
		tempB[1, 2] = 0.0f;
		tempB[2, 2] = (u1 - u2) * c;

		// v2 column
		tempB[0, 3] = 0.0f;
		tempB[1, 3] = (u1 - u2) * c;
		tempB[2, 3] = (v3 - v2) * c;

		// u3 column
		tempB[0, 4] = (v3 - v2) * c;
		tempB[1, 4] = 0.0f;
		tempB[2, 4] = (u3 - u4) * c;

		// v3 column
		tempB[0, 5] = 0.0f;
		tempB[1, 5] = (u3 - u4) * c;
		tempB[2, 5] = (v3 - v2) * c;

		// u4 column
		tempB[0, 6] = (v1 - v4) * c;
		tempB[1, 6] = 0.0f;
		tempB[2, 6] = (u3 - u4) * c;

		// v4 column
		tempB[0, 7] = 0.0f;
		tempB[1, 7] = (u3 - u4) * c;
		tempB[2, 7] = (v1 - v4) * c;

		return tempB;
	}
	
	// Calculate stiffness matrix of element
	public void CalculateKe()
	{
		Ke = new float[8, 8];
		float[,] Bt = Transpose(B);
		
		// Ke = A * (B^T * D * B)
		// Note: A instead of A*t as thickness is constant due to 2D view

		// B^T * D
		float[,] BtD = MultiplyMatrices(Bt, D);
		// (B^T*D) * B
		Ke = MultiplyMatrices(BtD, B);
		// A(B^T*D*B)
		Ke = MultiplyMatrixByScalar(Ke, GetArea());
	}

	// Calculate area by splitting the quadrilateral element
	// into 2 triangles and finding the area of each triangle using
	// the determinant*0.5 then add together
	public float GetArea()
	{
		/*
			3----------------2
			|			  /  |
			|	a		/	 |
			|		  /		 | 
			|	    /	 b	 |
			|	  /			 |
			0----------------1
		*/
		// Convert nodes to 1D float arrays for determinant function
		float[] node0, node1, node2, node3;
		node0 = new float[2] { u1, v1 };
		node1 = new float[2] { u2, v2 };
		node2 = new float[2] { u3, v3 };
		node3 = new float[2] { u4, v4 };

		float aArea;
		aArea = Determinant33(node0, node3, node2) * 0.5f;
		float bArea;
		bArea = Determinant33(node0, node2, node1) * 0.5f;

		return aArea + bArea;
	}

	// Calculate transpose of matrix
	public float[,] Transpose(float[,] matrix)
	{
		int matrixWidth = matrix.GetLength(0);
		int matrixHeight = matrix.GetLength(1);
		float[,] transposed = new float[matrixHeight, matrixWidth];

		for (int rowNum = 0; rowNum < matrixHeight; rowNum++)
			for (int colNum = 0; colNum < matrixWidth; colNum++)
				transposed[rowNum, colNum] = matrix[colNum, rowNum];

		return transposed;
	}
	// Calculate determinant of 2x2 matrix
	public float Determinant22(float a, float b, float c, float d)
	{
		return (a*d)-(b*c);
	}

	// Calculate determinant of 3x3 matrix made from 3 nodes
	// Used to calculate area of triangle
	// Nodes passed in as 1D array of floats with a size of 2 
	// eg. node1 = [x1, y1]
	public float Determinant33(float[] node1, float[] node2, float[] node3)
	{ 
		// Create matrix from nodes
		// |x1	x2	x3|
		// |y1	y2	y3|
		// | 1	 1	 1|
		float[,] m = new float[3, 3] {{node1[0], node2[0], node3[0]},
									  {node1[1], node2[1], node3[1]},
									  {1.0f,		1.0f,     1.0f} };
		float determinant;
		// |a	b	c|
		// |d	e	f|
		// |g	h	i|
		// |M| = a(ei - fh) - b(di - fg) + c(dh - eg)
		determinant = (m[0, 0] * ((m[1, 1] * m[2, 2]) - (m[2, 1] * m[1, 2]))) -
						(m[1, 0] * ((m[0, 1] * m[2, 2]) - (m[0, 2] * m[2, 1]))) +
						(m[2, 0] * ((m[0, 1] * m[1, 2]) - (m[0, 2] * m[1, 1])));
		return determinant;
	}

	// Multiplies matrix by a scalar value
	public float[,] MultiplyMatrixByScalar(float[,] matrix, float scalar)
	{
		int matrixWidth = matrix.GetLength(0);
		int matrixHeight = matrix.GetLength(1);
		float[,] result = new float[matrixWidth, matrixHeight];

		for (int i = 0; i < matrixWidth; i++)
			for (int j = 0; j < matrixHeight; j++)
				result[i, j] = matrix[i, j] * scalar;

		return result;
	}

	// Multiplies two matrices together
	public float[,] MultiplyMatrices(float[,] mA, float[,] mB)
	{
		int aRows = mA.GetLength(0);
		int aCols = mA.GetLength(1);
		int bRows = mB.GetLength(0);
		int bCols = mB.GetLength(1);

		// Check matrices are able to be multiplied
		if(aCols != bRows)
		{
			Debug.Log("Incompatible matrices trying to be multiplied!");
			return null;
		}

		float[,] result = new float[aRows, bCols];

		// Multiply each row of matrix A by the column of matrix B
		for(int i = 0; i < aRows; i++)
			for(int j = 0; j < bCols; j++)
				result[i, j] = (mA[i, 0] * mB[0, j]) + (mA[i, 1] * mB[1, j]) + (mA[i, 2] * mB[2, j]);

		return result;
	}

	// Find inverse of matrix using James McCaffrey's inverse functions
	public float[,] InverseMatrix(float[,] matrix)
	{
		if(matrix.GetLength(0) != matrix.GetLength(1))
		{
			Debug.Log("Can't inverse matrix that isn't square");
			return null;
		}
		float[,] inverse = ToMatrix(GetComponent<MatrixInverseProgram>().MatrixInverse(ToJaggedArray(matrix)));
		return inverse;
	}

	// Helper function to convert a 2D array to a jagged array
	// Needed for Inverting matrices
	public double[][] ToJaggedArray(float[,] matrix)
	{
		int matrixWidth = matrix.GetLength(0);
		int matrixHeight = matrix.GetLength(1);
		double[][] jaggedArray = new double[matrixWidth][];

		for(int i = 0; i < matrixWidth; i++)
		{
			jaggedArray[i] = new double[matrixHeight];
			for(int j = 0; j < matrixHeight; j++)
			{
				//Debug.Log($"Into jagged array {matrix[i, j]}");
				jaggedArray[i][j] = matrix[i, j];
			}
		}
		
		return jaggedArray;
	}

	// Helper function to convert a jagged array back to a 2D array
	// Needed for converting the inverted matrix back
	// Only for 8x8 array!
	public float[,] ToMatrix(double[][] jaggedArray)
	{
		int jaggedArrayWidth = 8;
		int jaggedArrayHeight = 8;

		float[,] tempMatrix = new float [8, 8];

		for (int i = 0; i < jaggedArrayWidth; i++)
		{
			for (int j = 0; j < jaggedArrayHeight; j++)
			{
				//Debug.Log($"Into array {(float)jaggedArray[i][j]}");
				tempMatrix[i, j] = (float)jaggedArray[i][j];
			}
		}

		return tempMatrix;
	}

	public void DeformNodes(float[,] displacementMatrix, float[,] forceMatrix)
	{
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
		nodes[0].transform.localPosition = nodes[0].transform.localPosition + new Vector3(forceMatrix[0, 0], forceMatrix[1, 0], 0.0f) + new Vector3(displacementMatrix[0, 0], displacementMatrix[1, 0], 0.0f);
		nodes[1].transform.localPosition = nodes[1].transform.localPosition + new Vector3(forceMatrix[2, 0], forceMatrix[3, 0], 0.0f) + new Vector3(displacementMatrix[2, 0], displacementMatrix[3, 0], 0.0f);
		nodes[2].transform.localPosition = nodes[2].transform.localPosition + new Vector3(forceMatrix[4, 0], forceMatrix[5, 0], 0.0f) + new Vector3(displacementMatrix[4, 0], displacementMatrix[5, 0], 0.0f);
		nodes[3].transform.localPosition = nodes[3].transform.localPosition + new Vector3(forceMatrix[6, 0], forceMatrix[7, 0], 0.0f) + new Vector3(displacementMatrix[6, 0], displacementMatrix[7, 0], 0.0f);
	}

	public float[,] CalculateF(Collision2D collision, float collisionForce, float damping)
	{
		// Calculate F
		float[,] F = new float[8, 1];
		int fNum = 0;   // Index for force matrix

		foreach (GameObject node in nodes)
		{
			Vector3 forceVector = node.transform.position - collision.transform.position;
			forceVector = forceVector.normalized;
			float xDist = forceVector.x;
			float yDist = forceVector.y;
			F[fNum, 0] = xDist * damping;
			fNum++;
			F[fNum, 0] = yDist * damping;
			fNum++;
		}

		return F;
	}
}
