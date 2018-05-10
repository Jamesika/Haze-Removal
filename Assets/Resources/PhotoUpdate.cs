using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PhotoUpdate : MonoBehaviour {

    public Image target;
    public Image selfImage;
	// Update is called once per frame
	void Update () 
    {
        selfImage.sprite = target.sprite;
	}
}
