﻿using System.Collections;
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

	// Array to hold elements
	GameObject[] elements;
	public GameObject element;

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
		elements = new GameObject[1];
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
		
		// Update vectors between nodes
		//AB = nodes[1].transform.position - nodes[0].transform.position;
		//BC = nodes[2].transform.position - nodes[1].transform.position;
		//CD = nodes[3].transform.position - nodes[2].transform.position;
		//DA = nodes[0].transform.position - nodes[3].transform.position;
		
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

		if (Input.GetKeyDown("w"))
		{
			Deform(nodes[2]);
		}
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
		// make all nodes children of the element
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i].transform.SetParent(gameObject.transform);
		}
	}

	void InitElements()
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
		FEMNode A, B, C, D;
		A = nodes[5].GetComponent<FEMNode>();
		B = nodes[6].GetComponent<FEMNode>();
		C = nodes[0].GetComponent<FEMNode>();
		D = nodes[1].GetComponent<FEMNode>();

		//Vector3 centre = new Vector3((A.position.x + B.position.x) / 2,
		//								(A.position.y + C.position.y) / 2,
		//									0.0f);
		Vector3 centre = new Vector3(0.0f, 0.0f, 0.0f);
		elements[0] = Instantiate(element, centre, Quaternion.identity);
		elements[0].transform.SetParent(gameObject.transform);
		//elements[0].transform.localScale = new Vector3(0.25f, 0.25f, 1.0f);

		elements[0].GetComponent<FEMElement>().nodes[0] = nodes[5];
		elements[0].GetComponent<FEMElement>().nodes[1] = nodes[6];
		elements[0].GetComponent<FEMElement>().nodes[2] = nodes[0];
		elements[0].GetComponent<FEMElement>().nodes[3] = nodes[1];

		elements[0].GetComponent<FEMElement>().DoMesh();

		//for (int y = 0; y < height; ++y)
		//{
		//	for (int x = 0; x < width; ++x)
		//	{
		//		int nodeNum = (y * width) + x;
		//		FEMNode nodeProperties = nodes[nodeNum].GetComponent<FEMNode>();
		//		if(!nodeProperties.ue && !nodeProperties.re)
		//		{

		//		}
		//	}
		//}
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
		if (collision.gameObject.tag != "Floor" && collision.relativeVelocity.magnitude > forceRequired)
		{
			GameObject closestNode = null;
			float currentClosestDistance = 1000000.0f;
			Vector3 collisionPos = collision.transform.position;

			// Loop through nodes and find closest
			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					int nodeNum = (y * width) + x;
					float distanceToCollision = Vector3.Distance(nodes[nodeNum].transform.position, collisionPos);
					if (distanceToCollision < currentClosestDistance)
					{
						closestNode = nodes[nodeNum];
						currentClosestDistance = distanceToCollision;
					}
				}
			}
			if (closestNode != null)
				Deform(closestNode);
			else
				Debug.Log("Couldn't find nearest node to collision!");
		}
	}

	void Deform(GameObject startNode)
	{
		Debug.Log("Deform() called!");
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
		foreach(GameObject node in perturbed1)
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
