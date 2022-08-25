using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpNeat.Phenomes;

public class FireTesting : MonoBehaviour
{
    private Vector2 firstPos;
    private Vector2 lastPos;
    private Vector2 firstDir;
    private Vector2 lastDir;

    Vector2 farthestTarget;

    float duration;
    float timeStart;
    int shot;
    float projectileSpeed;

    bool hitTarget;
    bool passedStats;
    bool ExitedBorders;

    int[] targetsHit;
    int intermediateBorderHits;

    GameObject[] Target;
    GameObject[] walls;

    [HideInInspector]
    public IBlackBox box;

    // Start is called before the first frame update
    void Start()
    {
        timeStart = Time.fixedTime;
        Target = GameObject.FindGameObjectsWithTag("Target");

        farthestTarget = FarthestTarget();

        targetsHit = new int[Target.Length];
        intermediateBorderHits = 0;
        hitTarget = false;
        passedStats = false;
        ExitedBorders = false;

        firstPos = transform.localPosition;
        lastPos = transform.localPosition;
        firstDir = gameObject.transform.parent.localRotation.eulerAngles;
        lastDir = gameObject.transform.parent.localRotation.eulerAngles;

        box = gameObject.GetComponentInParent<ShootTesting>().box;
        duration = gameObject.GetComponentInParent<ShootTesting>().GetDuration();
        projectileSpeed = gameObject.GetComponentInParent<ShootTesting>().GetSpeed();
        var size = gameObject.GetComponentInParent<ShootTesting>().GetSize();
        transform.localScale = new Vector3(Mathf.Abs(size), Mathf.Abs(size));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(ExitedBorders == true)
        {
            if(passedStats == false)
            {
                passedStats = true;
                gameObject.GetComponentInParent<ShootTesting>().AddToOutOfBounds();
                Destroy(gameObject);
            }
        }
        if (Time.fixedTime - timeStart < duration)
        {
            ISignalArray inputArr = box.InputSignalArray;
            inputArr[0] = Vector2.Distance(gameObject.transform.position, firstPos);
            inputArr[1] = gameObject.transform.localPosition.x - firstPos.x;
            inputArr[2] = gameObject.transform.localPosition.y - firstPos.y;

            box.Activate();
            ISignalArray outputArr = box.OutputSignalArray;

            float x = (float)outputArr[0] *2 * Time.fixedDeltaTime;//IsNumber((float)outputArr[0]) * 2 * Time.fixedDeltaTime;
            float y = (float)outputArr[1] * 2 * Time.fixedDeltaTime;//IsNumber((float)outputArr[1]) * 2 * Time.fixedDeltaTime;
            float color = (float)outputArr[2];//IsNumber((float)outputArr[2]);

            Vector2 move = (((Vector2)transform.right * x) + ((Vector2)transform.up * y)) * projectileSpeed;

            transform.Translate(move);

            ColorBullet(color);

            lastPos = transform.position;
        }
        else
        {
            transform.position = lastPos;
            if (passedStats == false)
            {
                passedStats = true;
                bool reachedTarget = false;
                for (int i = 0; i < Target.Length; i++)
                {
                    gameObject.GetComponentInParent<ShootTesting>().AddToHit(i, targetsHit[i]);
                    if (targetsHit[i] > 0 && reachedTarget == false)
                        reachedTarget = true;
                }
                if (hitTarget == true)
                {
                    gameObject.GetComponentInParent<ShootTesting>().AddSuccesfullProjectile();
                }
                else
                {
                    gameObject.GetComponentInParent<ShootTesting>().AddToMissed();
                }

                gameObject.GetComponentInParent<ShootTesting>().AddToIntermediateBorders(intermediateBorderHits);

                if (!reachedTarget)
                    gameObject.GetComponentInParent<ShootTesting>().ClosestToTarget(Vector2.Distance(lastPos, farthestTarget));

                Destroy(gameObject);
            }
        }
    }

    Vector2 FarthestTarget()
    {
        float dist = 0;
        foreach(GameObject target in Target)
        {
            if (Vector2.Distance(firstPos, target.transform.position) > dist)
            {
                farthestTarget = target.transform.position;
                dist = Vector2.Distance(firstPos, target.transform.position);
            }
        }
        return farthestTarget;
    }

    float IsNumber(float arr)
    {
        float number = arr;
        if (float.IsNaN(arr))
        {
            number = 0;
        }
        else if(float.IsInfinity(arr) || float.IsPositiveInfinity(arr))
        {
            number = 1;
        }
        else if (float.IsNegativeInfinity(arr))
        {
            number = -1;
        }
        return number;
    }

    void ColorBullet(float color)
    {
        if (gameObject.tag == "FireBullet")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.1f * color, 1, 1);
        }
        else if (gameObject.tag == "WaterBullet")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.5f + (0.13f * color), 1, 1);
        }
        else if (gameObject.tag == "WindBullet")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.25f + (0.15f * color), 1, 1);
        }
        else
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.08f + (0.05f * color), 0.85f, 0.45f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Target")
        {
            targetsHit[collision.gameObject.GetComponent<Target>().GetNumber()]++;

            hitTarget = true;

            if (!collision.isTrigger)
            {
                gameObject.GetComponentInParent<ShootTesting>().AddToHit(collision.gameObject.GetComponent<Target>().GetNumber(), 1);
                gameObject.GetComponentInParent<ShootTesting>().AddSuccesfullProjectile();
                Destroy(gameObject);
            }
        }
        if(collision.tag == "Border")
        {
            gameObject.GetComponentInParent<ShootTesting>().AddToHitBorders();
            Destroy(gameObject);
        }
        if (collision.tag == "IntermediateBorder")
        {
            intermediateBorderHits++;
        }
        if (collision.tag == "Bounds")
        {
            ExitedBorders = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "OOB")
        {
            ExitedBorders = true;
        }
    }
}