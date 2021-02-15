using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
	public GameObject cube;
	public int width = 1;
	public int height = 1;
	public GameObject[] cubeArray;

	// Start is called before the first frame update
	void Start()
    {
		cubeArray = new GameObject[width * height];
		// Instantiate cubes
		for (int y = 0; y < height; ++y)
		{
			for (int x = 0; x < width; ++x)
			{
				cubeArray[(y * width) + x] = Instantiate(cube, new Vector3(x, y, 0), Quaternion.identity);
			}
		}
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
