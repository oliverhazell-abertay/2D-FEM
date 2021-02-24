using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	Renderer rend;
	//public Vector3 AB, BC, CD, DA;

	// Variables for grid of nodes
	public int width, height;
	Vector2 topLeft;
	float xSpacing, ySpacing;

	// Variables for collidor
	public Vector2[] polygonPath;
	public PolygonCollider2D polyCollider;

	// Variables for collision
	public float stiffness = 0.05f;
	public float forceRequired = 0;

	// Perturbed Lists
	public List<GameObject> perturbed1;
	public List<GameObject> perturbed2;

	// Variables for rendering
	public bool edgesOnly = true;

	// Start is called before the first frame update
	void Start()
	{
		rend = gameObject.GetComponent<Renderer>();
		nodes = new GameObject[width * height];
		topLeft = new Vector2(rend.bounds.min.x, rend.bounds.max.y);

		// Get number of edge nodes for polygon collider
		polygonPath = new Vector2[((width - 2) * 2) + (height * 2)];

		// Calculate spacing between nodes in each axis. Set to 0 if width is 1 to avoid divide by 0
		xSpacing = width == 1 ? 0 : (rend.bounds.max.x - rend.bounds.min.x) / (width - 1);
		ySpacing = height == 1 ? 0 : (rend.bounds.max.y - rend.bounds.min.y) / (height - 1);

		// Instantiate nodes
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				nodes[(y * width) + x] = Instantiate(node, new Vector3(topLeft.x + (xSpacing * x), topLeft.y - (ySpacing * y), -1), Quaternion.identity);
				nodes[(y * width) + x].name = $"Node ({x},{y})";
				nodes[(y * width) + x].GetComponent<FEMNode>().Position = new Vector2(topLeft.x + (xSpacing * x), topLeft.y - (ySpacing * y));
				// Check if it's a left edge
				if (x == 0)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().le = true;
				}
				// Check if it's a right edge
				if (x == width - 1)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().re = true;
				}
				// Check if it's a top edge
				if (y == 0)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().ue = true;
					// Check if it's a corner
					if (x == 0 || x == width - 1)
					{
						nodes[(y * width) + x].GetComponent<FEMNode>().corner = true;
					}
				}
				// Check if it's a bottom edge
				if (y == height - 1)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().de = true;
					// Check if it's a corner
					if (x == 0 || x == width - 1)
					{
						nodes[(y * width) + x].GetComponent<FEMNode>().corner = true;
					}
				}
			}
		}
		// Populate neighbours of each node
		for (int y = 0; y < height; ++y)
		{
			for(int x = 0; x < width; ++x)
			{
				// Left
				if (nodes[(y * width) + x].GetComponent<FEMNode>().le == false)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().leftAdj = nodes[(y * width) + (x - 1)];
					nodes[(y * width) + x].GetComponent<FEMNode>().neighbourCount++;
				}
				// Right
				if (nodes[(y * width) + x].GetComponent<FEMNode>().re == false)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().rightAdj = nodes[(y * width) + (x + 1)];
					nodes[(y * width) + x].GetComponent<FEMNode>().neighbourCount++;
				}
				// Up
				if (nodes[(y * width) + x].GetComponent<FEMNode>().ue == false)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().upAdj = nodes[((y - 1) * width) + x];
					nodes[(y * width) + x].GetComponent<FEMNode>().neighbourCount++;
				}
				// Down
				if (nodes[(y * width) + x].GetComponent<FEMNode>().de == false)
				{
					nodes[(y * width) + x].GetComponent<FEMNode>().downAdj = nodes[((y + 1) * width) + x];
					nodes[(y * width) + x].GetComponent<FEMNode>().neighbourCount++;
				}
			}
		}

		// Initialise polygonPath for the collidor
		CalculatePolyCollider();
		
		// make all nodes children of the element
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].transform.SetParent(gameObject.transform);
		}
	}

	// Update is called once per frame
	void Update()
	{
		// Only render edges if setting is on
		if (edgesOnly == true)
		{
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					if (nodes[(y * width) + x].GetComponent<FEMNode>().ue == false
								   && nodes[(y * width) + x].GetComponent<FEMNode>().de == false
								   && nodes[(y * width) + x].GetComponent<FEMNode>().le == false
								   && nodes[(y * width) + x].GetComponent<FEMNode>().re == false)
					{
						nodes[(y * width) + x].GetComponent<SpriteRenderer>().enabled = false;
					}
				}
			}
		}
		// Calculate path for collidor and set it
		CalculatePolyCollider();
		polyCollider.enabled = false;
		polyCollider.SetPath(0, polygonPath);
		polyCollider.enabled = true;
		// Update vectors between nodes
		//AB = nodes[1].transform.position - nodes[0].transform.position;
		//BC = nodes[2].transform.position - nodes[1].transform.position;
		//CD = nodes[3].transform.position - nodes[2].transform.position;
		//DA = nodes[0].transform.position - nodes[3].transform.position;
		if(Input.GetKeyDown("w"))
		{
			Deform();
		}
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

	private void OnCollisionEnter2D(Collision2D collision)
	{

	}

	void Deform()
	{
		Debug.Log("Deform() called!");
		// Calculate force per node
		float currentForce = 5.0f;
		GameObject startNode = nodes[2];
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
		foreach(GameObject node in perturbed1)
		{
			displaceDir = node.transform.position - impactPos;
			node.transform.position += (displaceDir).normalized * (currentForce * stiffness);
		}

		// Displace second layer of perturbed nodes
		foreach (GameObject node in perturbed2)
		{
			displaceDir = node.transform.position - impactPos;
			node.transform.position += (displaceDir).normalized * (currentForce * stiffness);
		}
		

		// Clear perturbed lists
		perturbed1.Clear();
		perturbed2.Clear();
	}

	void AddNeighboursToPerturbed1(GameObject currentNodeObject)
	{
		FEMNode currentNode = currentNodeObject.GetComponent<FEMNode>();

		// Left neighbour
		if(currentNode.leftAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if(!perturbed1.Contains(currentNode.leftAdj))
			{
				//Debug.Log("Adding leftAdj to perturbed1!");
				perturbed1.Add(currentNode.leftAdj);
			}
		}
		// Right neighbour
		if (currentNode.rightAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentNode.rightAdj))
			{
				//Debug.Log("Adding rightAdj to perturbed1!");
				perturbed1.Add(currentNode.rightAdj);
			}
		}
		// Up neighbour
		if (currentNode.upAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentNode.upAdj))
			{
				//Debug.Log("Adding upAdj to perturbed1!");
				perturbed1.Add(currentNode.upAdj);
			}
		}
		// Down neighbour
		if (currentNode.downAdj != null)
		{
			// Check neighbour isn't already in perturbed1 list
			if (!perturbed1.Contains(currentNode.downAdj))
			{
				//Debug.Log("Adding downAdj to perturbed1!");
				perturbed1.Add(currentNode.downAdj);
			}
		}
	}

	void AddNeighboursToPerturbed2(GameObject currentNodeObject)
	{
		FEMNode currentNode = currentNodeObject.GetComponent<FEMNode>();

		// Left neighbour
		if (currentNode.leftAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentNode.leftAdj))
			{
				//Debug.Log("Adding leftAdj to perturbed2!");
				perturbed2.Add(currentNode.leftAdj);
			}
		}
		// Right neighbour
		if (currentNode.rightAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentNode.rightAdj))
			{
				//Debug.Log("Adding rightAdj to perturbed2!");
				perturbed2.Add(currentNode.rightAdj);
			}
		}
		// Up neighbour
		if (currentNode.upAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentNode.upAdj))
			{
				//Debug.Log("Adding upAdj to perturbed2!");
				perturbed2.Add(currentNode.upAdj);
			}
		}
		// Down neighbour
		if (currentNode.downAdj != null)
		{
			// Check neighbour isn't already in perturbed2 list
			if (!perturbed2.Contains(currentNode.downAdj))
			{
				//Debug.Log("Adding downAdj to perturbed2!");
				perturbed2.Add(currentNode.downAdj);
			}
		}
	}
}
