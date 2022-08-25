using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    int number;

    // Start is called before the first frame update
    void Start()
    {
        Results.targetOrder++;
        if (Results.targetOrder > GameObject.FindGameObjectsWithTag("Target").Length)
        {
            Results.targetOrder = 1;
        }
        number = Results.targetOrder - 1;
    }

    public int GetNumber()
    {
        return number;
    }
}
