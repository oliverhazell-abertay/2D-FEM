using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FEMShape : MonoBehaviour
{
	/*
		A----------------B
		|				 |
		|				 |
		|				 |
		|				 |
		|				 |
		D----------------C

		A - 0
		B - 1
		C - 2
		D - 3
	*/

	// Array to hold nodes
	GameObject[] nodes;
	public GameObject node;

	// Array to hold elements
	public GameObject[] elements;
	public GameObject element;

	Renderer rend;

	// Variables for grid of nodes
	public int width, height;
	Vector2 topLeft;
	float xSpacing, ySpacing;

	// Variables for collidor
	public Vector2[] polygonPath;
	public PolygonCollider2D polyCollider;

	// Variables for collision
	public float stiffness = 0.05f;
	public float forceRequired = 0.0f;
	public float damping = 0.0f;	// N/M

	// Perturbed Lists
	public List<GameObject> perturbed1;
	public List<GameObject> perturbed2;

	// Variables for rendering
	public bool renderNodes = true;

	public float localX;
	public float localY;

	// Start is called before the first frame update
	void Start()
	{
		rend = gameObject.GetComponent<Renderer>();
		nodes = new GameObject[width * height];
		elements = new GameObject[(width - 1) * (height - 1)];
		topLeft = new Vector2(rend.bounds.min.x, rend.bounds.max.y);

		// Get number of edge nodes for polygon collider
		polygonPath = new Vector2[((width - 2) * 2) + (height * 2)];

		// Calculate spacing between nodes in each axis. Set to 0 if width is 1 to avoid divide by 0
		xSpacing = width == 1 ? 0 : (rend.bounds.max.x - rend.bounds.min.x) / (width - 1);
		ySpacing = height == 1 ? 0 : (rend.bounds.max.y - rend.bounds.min.y) / (height - 1);

		// Instantiate nodes
		InitNodes();

		// Instantiate elements
		InitElements();
		
		// Calculate path for collidor and set it
		CalculatePolyCollider();
		polyCollider.enabled = false;
		polyCollider.SetPath(0, polygonPath);
		polyCollider.enabled = true;	
	}

	// Update is called once per frame
	void Update()
	{
		// Calculate path for collidor and set it
		CalculatePolyCollider();
		polyCollider.enabled = false;
		polyCollider.SetPath(0, polygonPath);
		polyCollider.enabled = true;
	}

	void InitNodes()
	{
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int nodeNum = (y * width) + x;
				nodes[nodeNum] = Instantiate(node, new Vector3(topLeft.x + (xSpacing * x), topLeft.y - (ySpacing * y), -1), Quaternion.identity);
				nodes[nodeNum].name = $"Node ({x},{y})";
				nodes[nodeNum].GetComponent<FEMNode>().gridPos = nodeNum;
				nodes[nodeNum].GetComponent<FEMNode>().position = new Vector2(topLeft.x + (xSpacing * x), topLeft.y - (ySpacing * y));
				// Check if it's a left edge
				if (x == 0)
				{
					nodes[nodeNum].GetComponent<FEMNode>().le = true;
				}
				// Check if it's a right edge
				if (x == width - 1)
				{
					nodes[nodeNum].GetComponent<FEMNode>().re = true;
				}
				// Check if it's a top edge
				if (y == 0)
				{
					nodes[nodeNum].GetComponent<FEMNode>().ue = true;
					// Check if it's a corner
					if (x == 0 || x == width - 1)
					{
						nodes[nodeNum].GetComponent<FEMNode>().corner = true;
					}
				}
				// Check if it's a bottom edge
				if (y == height - 1)
				{
					nodes[nodeNum].GetComponent<FEMNode>().de = true;
					// Check if it's a corner
					if (x == 0 || x == width - 1)
					{
						nodes[nodeNum].GetComponent<FEMNode>().corner = true;
					}
				}
			}
		}
		// Populate neighbours of each node
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int nodeNum = (y * width) + x;
				// Left
				if (nodes[nodeNum].GetComponent<FEMNode>().le == false)
				{
					nodes[nodeNum].GetComponent<FEMNode>().leftAdj = nodes[(y * width) + (x - 1)];
					nodes[nodeNum].GetComponent<FEMNode>().neighbourCount++;
				}
				// Right
				if (nodes[nodeNum].GetComponent<FEMNode>().re == false)
				{
					nodes[nodeNum].GetComponent<FEMNode>().rightAdj = nodes[(y * width) + (x + 1)];
					nodes[nodeNum].GetComponent<FEMNode>().neighbourCount++;
				}
				// Up
				if (nodes[nodeNum].GetComponent<FEMNode>().ue == false)
				{
					nodes[nodeNum].GetComponent<FEMNode>().upAdj = nodes[((y - 1) * width) + x];
					nodes[nodeNum].GetComponent<FEMNode>().neighbourCount++;
				}
				// Down
				if (nodes[nodeNum].GetComponent<FEMNode>().de == false)
				{
					nodes[nodeNum].GetComponent<FEMNode>().downAdj = nodes[((y + 1) * width) + x];
					nodes[nodeNum].GetComponent<FEMNode>().neighbourCount++;
				}
			}
		}
		// make all nodes children of the shape
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].transform.SetParent(gameObject.transform);
		}
		// Only render edges if setting is on
		if (!renderNodes)
		{
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
						nodes[(y * width) + x].GetComponent<SpriteRenderer>().enabled = false;
				}
			}
		}

	}

	// Create elements starting from bottom left node of each element
	void InitElements()
	{
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				int nodeNum = (y * width) + x;
				FEMNode nodeProperties = nodes[nodeNum].GetComponent<FEMNode>();
				if (!nodeProperties.ue && !nodeProperties.re)
				{
					int elementNum = (nodeNum - width) - (y - 1);
					CreateElement(nodeNum, elementNum);
				}
			}
		}
		// Assign neighbouring elements
		for (int y = 0; y < height - 1; ++y)
		{
			for (int x = 0; x < width - 1; ++x)
			{
				int elementNum = (y * (width - 1)) + x;
				// Left
				if (elements[elementNum].GetComponent<FEMElement>().nodes[3].GetComponent<FEMNode>().le == false)
				{
					elements[elementNum].GetComponent<FEMElement>().leftAdj = elements[elementNum - 1];
					elements[elementNum].GetComponent<FEMElement>().neighbourCount++;
				}
				// Right
				if (elements[elementNum].GetComponent<FEMElement>().nodes[1].GetComponent<FEMNode>().re == false)
				{
					elements[elementNum].GetComponent<FEMElement>().rightAdj = elements[elementNum + 1];
					elements[elementNum].GetComponent<FEMElement>().neighbourCount++;
				}
				// Up
				if (elements[elementNum].GetComponent<FEMElement>().nodes[3].GetComponent<FEMNode>().ue == false)
				{
					elements[elementNum].GetComponent<FEMElement>().upAdj = elements[elementNum - (width - 1)];
					elements[elementNum].GetComponent<FEMElement>().neighbourCount++;
				}
				// Down
				if (elements[elementNum].GetComponent<FEMElement>().nodes[1].GetComponent<FEMNode>().de == false)
				{
					elements[elementNum].GetComponent<FEMElement>().downAdj = elements[elementNum + (width - 1)];
					elements[elementNum].GetComponent<FEMElement>().neighbourCount++;
				}
			}
		}
	}

	void CreateElement(int nodeNum, int elementNum)
	{
		/*
		3----------------2
		|				 |
		|				 |
		|				 | 
		|				 |
		|				 |
		0----------------1
		*/
		FEMNode nodeProperties = nodes[nodeNum].GetComponent<FEMNode>();
		GameObject A, B, C, D;
		A = nodes[nodeNum];	// 0
		B = nodeProperties.rightAdj; // 1
		C = nodeProperties.upAdj.GetComponent<FEMNode>().rightAdj; // 2
		D = nodeProperties.upAdj; // 3

		// Instantiate element
		Vector3 centre = new Vector3(0.0f, 0.0f, 0.0f);
		elements[elementNum] = Instantiate(element, centre, Quaternion.identity);
		elements[elementNum].name = $"Element ({elementNum})";
		elements[elementNum].transform.SetParent(gameObject.transform);
		elements[elementNum].transform.localPosition = centre;

		// Assign nodes
		elements[elementNum].GetComponent<FEMElement>().nodes[0] = A;
		elements[elementNum].GetComponent<FEMElement>().nodes[1] = B;
		elements[elementNum].GetComponent<FEMElement>().nodes[2] = C;
		elements[elementNum].GetComponent<FEMElement>().nodes[3] = D;
		
		elements[elementNum].GetComponent<FEMElement>().DoMesh();
	}

	void CalculatePolyCollider()
	{
		int pathNum = 0;
		// Top edge
		for (int x = 0; x < width; ++x)
		{
			polygonPath[pathNum] = new Vector2(nodes[x].transform.localPosition.x, nodes[x].transform.localPosition.y);
			pathNum++;
		}
		// Right edge
		for (int y = 1; y < height; ++y)
		{
			polygonPath[pathNum] = new Vector2(nodes[(y * width) + (width - 1)].transform.localPosition.x, nodes[(y * width) + (width - 1)].transform.localPosition.y);
			pathNum++;
		}
		// Bottom edge
		for (int x = width - 2; x > -1; --x)
		{
			polygonPath[pathNum] = new Vector2(nodes[((height - 1) * width) + x].transform.localPosition.x, nodes[((height - 1) * width) + x].transform.localPosition.y);
			pathNum++;
		}
		// Left edge
		for (int y = height - 2; y > 0; --y)
		{
			polygonPath[pathNum] = new Vector2(nodes[(y * width) + 0].transform.localPosition.x, nodes[(y * width) + 0].transform.localPosition.y);
			pathNum++;
		}
	}

	//// Node deformation
	//private void OnCollisionEnter2D(Collision2D collision)
	//{
	//	if (collision.gameObject.tag != "Floor" && collision.relativeVelocity.magnitude > forceRequired)
	//	{
	//		GameObject closestNode = null;
	//		float currentClosestDistance = 1000000.0f;
	//		Vector3 collisionPos = collision.transform.position;

	//		// Loop through nodes and find closest
	//		for (int y = 0; y < height - 1; ++y)
	//		{
	//			for (int x = 0; x < width - 1; ++x)
	//			{
	//				int nodeNum = (y * width) + x;
	//				float distanceToCollision = Vector3.Distance(nodes[nodeNum].transform.position, collisionPos);
	//				if (distanceToCollision < currentClosestDistance)
	//				{
	//					closestNode = nodes[nodeNum];
	//					currentClosestDistance = distanceToCollision;
	//				}
	//			}
	//		}
	//		if (closestNode != null)
	//			NodeDeform(closestNode);
	//		else
	//			Debug.Log("Couldn't find nearest node to collision!");
	//	}
	//}

	// Deformation based on elements
	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.tag != "Floor" && collision.relativeVelocity.magnitude > forceRequired)
		{
			GameObject closestElement = null;
			float currentClosestDistance = 1000000.0f;
			Vector3 collisionPos = collision.transform.position;

			int nearestElementNum = 0;
			// Loop through elements and find closest
			for (int y = 0; y < height - 1; ++y)
			{
				for (int x = 0; x < width - 1; ++x)
				{
					int elementNum = (y * (width - 1)) + x;
					Vector3 currentElementPos = new Vector3(elements[elementNum].GetComponent<FEMElement>().midX,
																elements[elementNum].GetComponent<FEMElement>().midY,
																	0.0f);
					float distanceToCollision = Vector3.Distance(currentElementPos, collisionPos);
					if (distanceToCollision < currentClosestDistance)
					{
						closestElement = elements[elementNum];
						nearestElementNum = elementNum;
						currentClosestDistance = distanceToCollision;
					}
				}
			}
			if (closestElement != null)
				ElementDeform(closestElement, collision);
			else
				Debug.Log("Couldn't find nearest element to collision!");
		}
	}

	// Deformation based on nodes
	void NodeDeform(GameObject startNode)
	{
		// Calculate force per node
		float currentForce = 5.0f;
		Vector3 impactPos = startNode.transform.position;

		// Create and populate perturbed lists
		// Dissipate force through neighbours 
		startNode.GetComponent<FEMNode>().currentForceApplied = currentForce;
		AddNeighboursToPerturbed1(startNode);
		foreach (GameObject node in perturbed1)
		{
			AddNeighboursToPerturbed2(node);
		}

		// Calculate direction vector for each affected node
		// Move node and reset for next frame
		Vector3 displaceDir;
		displaceDir = new Vector3(0.0f, -1.0f, 0.0f);

		// Displace impacted node
		startNode.transform.position += (displaceDir).normalized * (currentForce * stiffness);

		// Displace first layer of perturbed nodes
		currentForce *= 0.5f;
		foreach (GameObject node in perturbed1)
		{
			displaceDir = node.transform.position - impactPos;
			node.transform.position += (displaceDir).normalized * (currentForce * stiffness);
		}

		// Displace second layer of perturbed nodes
		currentForce *= 0.5f;
		foreach (GameObject node in perturbed2)
		{
			displaceDir = node.transform.position - impactPos;
			node.transform.position += (displaceDir).normalized * (currentForce * stiffness);
		}

		// Clear perturbed lists
		perturbed1.Clear();
		perturbed2.Clear();
	}

	void ElementDeform(GameObject startElement, Collision2D collision)
	{
		// Calculate force per element
		float collisionForce = collision.relativeVelocity.magnitude;

		// F = KX
		// K^-1 * F = X
		float[,] iKe = startElement.GetComponent<FEMElement>().InverseMatrix(startElement.GetComponent<FEMElement>().Ke);
		// Calculate F
		float[,] F = new float[8,1];
		F = startElement.GetComponent<FEMElement>().CalculateF(collision, collisionForce, damping);
		// Calculate X by multiplying the inverse of K by F
		float[,] X = startElement.GetComponent<FEMElement>().MultiplyMatrices(iKe, F);
		// Deform nodes in the element based on X matrix
		startElement.GetComponent<FEMElement>().DeformNodes(X, F);

		AddNeighboursToPerturbed1(startElement);
		foreach (GameObject element in perturbed1)
		{
			// F = KX
			// K^-1 * F = X
			iKe = startElement.GetComponent<FEMElement>().InverseMatrix(element.GetComponent<FEMElement>().Ke);
			// Calculate F
			F = element.GetComponent<FEMElement>().CalculateF(collision, collisionForce, damping);
			// Calculate X by multiplying the inverse of K by F
			X = element.GetComponent<FEMElement>().MultiplyMatrices(iKe, F);
			// Deform nodes in the element based on X matrix
			element.GetComponent<FEMElement>().DeformNodes(X, F);

			AddNeighboursToPerturbed2(element);
		}
		perturbed1.Clear();
		perturbed2.Clear();
	}

	void AddNeighboursToPerturbed1(GameObject currentElementObject)
	{
		FEMElement currentElement = currentElementObject.GetComponent<FEMElement>();

		// Left neighbour
		if(currentElement.leftAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if(!perturbed1.Contains(currentElement.leftAdj))
			{
				//Debug.Log("Adding leftAdj to perturbed1!");
				perturbed1.Add(currentElement.leftAdj);
			}
		}
		// Right neighbour
		if (currentElement.rightAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentElement.rightAdj))
			{
				//Debug.Log("Adding rightAdj to perturbed1!");
				perturbed1.Add(currentElement.rightAdj);
			}
		}
		// Up neighbour
		if (currentElement.upAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentElement.upAdj))
			{
				//Debug.Log("Adding upAdj to perturbed1!");
				perturbed1.Add(currentElement.upAdj);
			}
		}
		// Down neighbour
		if (currentElement.downAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentElement.downAdj))
			{
				//Debug.Log("Adding downAdj to perturbed1!");
				perturbed1.Add(currentElement.downAdj);
			}
		}
	}

	void AddNeighboursToPerturbed2(GameObject currentElementObject)
	{
		FEMElement currentElement = currentElementObject.GetComponent<FEMElement>();

		// Left neighbour
		if (currentElement.leftAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentElement.leftAdj) || !perturbed1.Contains(currentElement.leftAdj))
			{
				//Debug.Log("Adding leftAdj to perturbed2!");
				perturbed2.Add(currentElement.leftAdj);
			}
		}
		// Right neighbour
		if (currentElement.rightAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentElement.rightAdj) || !perturbed1.Contains(currentElement.rightAdj))
			{
				//Debug.Log("Adding rightAdj to perturbed2!");
				perturbed2.Add(currentElement.rightAdj);
			}
		}
		// Up neighbour
		if (currentElement.upAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentElement.upAdj) || !perturbed1.Contains(currentElement.upAdj))
			{
				//Debug.Log("Adding upAdj to perturbed2!");
				perturbed2.Add(currentElement.upAdj);
			}
		}
		// Down neighbour
		if (currentElement.downAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentElement.downAdj) || !perturbed2.Contains(currentElement.downAdj))
			{
				//Debug.Log("Adding downAdj to perturbed2!");
				perturbed2.Add(currentElement.downAdj);
			}
		}
	}
}
