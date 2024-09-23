using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbienceRestarter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // De-activate the game object after 0.1 seconds and then activate it again
        gameObject.SetActive(false);
        Invoke("Activate", 0.1f);
    }

    void Activate()
    {
        gameObject.SetActive(true);
    }
}
