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
	public float stiffness;
	public float forceRequired = 0;

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
		float collisionForce = collision.GetContact(0).normalImpulse / Time.fixedDeltaTime;
		if (collision.gameObject.tag == "Player" && collisionForce > forceRequired)
		{
			//Debug info about collision
			Debug.Log($"Impact at ({collision.GetContact(0).point.x}, {collision.GetContact(0).point.y}). Force: {collisionForce}");
			Debug.Log($"Surface normal of collision ({collision.GetContact(0).normal.x}, {collision.GetContact(0).normal.y})");

			// Check for the nearest two nodes to the collision
			int closestNode = 0;
			int secondClosestNode = 0;
			float closestDistance = 1000;
			float secondClosestDistance = 1000;

			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					if (nodes[(y * width) + x].GetComponent<FEMNode>().ue == true
							|| nodes[(y * width) + x].GetComponent<FEMNode>().de == true
							|| nodes[(y * width) + x].GetComponent<FEMNode>().le == true
							|| nodes[(y * width) + x].GetComponent<FEMNode>().re == true)
					{
						Vector2 currentNodePosition;
						currentNodePosition = new Vector2(nodes[(y * width) + x].transform.position.x, nodes[(y * width) + x].transform.position.y);
						float currentDistance;
						currentDistance = Vector2.Distance(currentNodePosition, collision.GetContact(0).point);
						if (currentDistance < closestDistance)
						{
							closestNode = (y * width) + x;
							closestDistance = currentDistance;
						}
						else if (currentDistance < secondClosestDistance)
						{
							secondClosestNode = (y * width) + x;
							secondClosestDistance = currentDistance;
						}
					}
				}
			}
			// Move node in direction away from collision
			Vector3 dentDirection;
			dentDirection = new Vector3(collision.GetContact(0).normal.x, collision.GetContact(0).normal.y, 0.0f);
			nodes[closestNode].transform.position += (dentDirection * collisionForce).normalized * stiffness;
			nodes[secondClosestNode].transform.position += (dentDirection * collisionForce).normalized * stiffness;
		}
	}
}
