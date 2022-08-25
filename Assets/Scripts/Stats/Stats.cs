using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Stats
{
    public static int minProjNumber = 1;
    public static int maxProjNumber = 15;

    public static float minProjDamage = 1;
    public static float maxProjDamage = 120;

    public static float minProjDuration = 1.5f;
    public static float maxProjDuration = 10f;

    public static float minROF = 1f;
    public static float maxROF = 6f;

    public static float minSize = 5;
    public static float maxSize = 60;

    public static float minSpeed = 5;
    public static float maxSpeed = 50;

    public static float minInaccuracy = 5;
    public static float maxInaccuracy = 60;

    public static float[,] stats =
    { {1, 15 }, { 1f, 120f }, { 1.5f, 10f }, { 1f, 6f }, { 5f, 60f }, { 5f, 50f }, { 5f, 60f } };
}
