using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementFEM : MonoBehaviour
{
	public struct Node
	{
		public Node(Vector2 pos, bool fix)
		{
			Position = pos;
			Fixed = fix;
		}
		Vector2 Position;
		bool Fixed;
	}
	
	Node[] nodes;

    // Start is called before the first frame update
    void Start()
    {
		nodes = new Node[4];
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
