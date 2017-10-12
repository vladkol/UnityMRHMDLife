
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectGenerator : MonoBehaviour
{
    public GameObject objectToClone = null;

    private bool stop = false;

	// Use this for initialization
	void Start ()
    {
        SimpleAdaptiveQuality qualityObject = GetComponent<SimpleAdaptiveQuality>();
        if(qualityObject != null)
        {
            qualityObject.qualityLevelChanged += (SimpleAdaptiveQuality self, int previousQuality, int newQuality) =>
            {
                stop = (newQuality == 0);
            };
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (objectToClone == null || stop)
            return;

        float x = Random.Range(-200, 200);
        float y = Random.Range(-200, 200);
        float z = Random.Range(5, 300);

        float xr = Random.Range(0, 181);
        float yr = Random.Range(0, 181);
        float zr = Random.Range(0, 181);


        Quaternion rotation = Quaternion.Euler(xr, yr, zr);
        Vector3 pos = new Vector3(x, y, z);
        float scale = Random.Range(0.5f, Mathf.Max(pos.magnitude/4, 1f));

        GameObject newObj = (GameObject)GameObject.Instantiate(objectToClone, pos, rotation, null);
        newObj.transform.localScale = new Vector3(scale, scale, scale);

        newObj.SetActive(true);

    }
}
