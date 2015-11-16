using UnityEngine;
using System.Collections;

public class HeadmeshRandomizer : MonoBehaviour {

    public Headmesh headmesh;
    public float RandomIncrement;

    private float last_random_time;

    void Start()
    {
        last_random_time = Time.time;
        headmesh.Randomize();
    }
	
	// Update is called once per frame
	void Update () {
	    if(Time.time - last_random_time > RandomIncrement)
        {
            headmesh.Randomize();
            last_random_time = Time.time;
        }
	}
}
