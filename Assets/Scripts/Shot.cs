using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpNeat.Phenomes;

public class Shot : MonoBehaviour
{
    public GameObject bullet;

    IBlackBox box;
    int projectileNumber;
    float damage;
    float duration;
    float speed;
    float size;
    float accuracy;
    string individual;

    public IBlackBox GetBox()
    {
        return box;
    }
    public float GetDamage()
    {
        return damage;
    }
    public float GetDuration()
    {
        return duration;
    }
    public float GetSize()
    {
        return size;
    }
    public float GetSpeed()
    {
        return speed;
    }
    public string GetName()
    {
        return individual;
    }

    public void SetParameters(IBlackBox box,int projectileNumber, float damage, float duration, float speed, float size, float accuracy, string name)
    {
        this.box = box;
        this.projectileNumber = projectileNumber;
        this.damage = damage;
        this.duration = duration;
        this.speed = speed;
        this.size = size;
        this.accuracy = accuracy;
        this.individual = name;
    }

    public void Shoot()
    {
        for (int i = 0; i < projectileNumber; i++)
        {
            float projectileAngle = gaussian(-accuracy, accuracy);
            Quaternion rot = Quaternion.Euler(0, 0, projectileAngle + gameObject.transform.rotation.eulerAngles.z / 2);
            Instantiate(bullet, gameObject.transform.position, rot, gameObject.transform);
            //Debug.Break();
        }
    }

    float gaussian(float min, float max)
    {
        float x, r1, r2, squareSum;

        do
        {
            r1 = (2.0f * Random.value) - 1.0f;
            r2 = (2.0f * Random.value) - 1.0f;
            squareSum = (r1 * r1) + (r2 * r2);
        }
        while (squareSum >= 1.0f);

        float std = r1 * Mathf.Sqrt(-2.0f * Mathf.Log(squareSum) / squareSum);

        float mean = (min + max) / 2.0f;
        float sigma = (max - mean) / 3.0f;

        return Mathf.Clamp(std * sigma + mean, min, max);
    }
}
