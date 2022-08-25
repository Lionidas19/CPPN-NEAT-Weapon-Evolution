using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpNeat.Phenomes;

public class FireRunBest : MonoBehaviour
{
    private Vector2 firstPos;
    private Vector2 lastPos;
    GameObject weapon;
    bool hitTarget;
    bool done;

    float damage;
    float duration;
    float timeStart;
    float projectileSpeed;

    GameObject[] Target;
    int[] targetsHit;

    [HideInInspector]
    public IBlackBox box;

    // Start is called before the first frame update
    void Start()
    {
        weapon = GameObject.FindGameObjectWithTag("Weapon");

        timeStart = Time.fixedTime;

        firstPos = transform.localPosition;
        lastPos = transform.localPosition;
        hitTarget = false;
        done = false;
        Target = GameObject.FindGameObjectsWithTag("Target");

        targetsHit = new int[Target.Length];

        box = gameObject.GetComponentInParent<Shot>().GetBox();
        damage = gameObject.GetComponentInParent<Shot>().GetDamage();
        duration = gameObject.GetComponentInParent<Shot>().GetDuration();
        projectileSpeed = gameObject.GetComponentInParent<Shot>().GetSpeed();
        var size = gameObject.GetComponentInParent<Shot>().GetSize();
        transform.localScale = new Vector3(Mathf.Abs(size), Mathf.Abs(size));
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ISignalArray inputArr = box.InputSignalArray;
        inputArr[0] = Vector2.Distance(gameObject.transform.position, firstPos);
        inputArr[1] = gameObject.transform.localPosition.x - firstPos.x;
        inputArr[2] = gameObject.transform.localPosition.y - firstPos.y;

        box.Activate();
        ISignalArray outputArr = box.OutputSignalArray;

        float x = (float)outputArr[0] * 2 * Time.fixedDeltaTime;
        float y = (float)outputArr[1] * 2 * Time.fixedDeltaTime;
        float color = (float)outputArr[2];

        Vector2 move = (((Vector2)transform.right * x) + ((Vector2)transform.up * y)) * projectileSpeed;

        transform.Translate(move);

        ColorBullet(color);

        lastPos = transform.position;

        if (Time.fixedTime - timeStart >= duration)
        {
            if (hitTarget)
            {
                if (!done)
                {
                    done = true;
                    weapon.GetComponent<ShootBest>().AddSuccessfulProjectiles();
                    for (int i = 0; i < Target.Length; i++)
                        weapon.GetComponent<ShootBest>().AddToHit(i, targetsHit[i]);
                    weapon.GetComponent<ShootBest>().AddTimeAlive(duration);
                    Destroy(gameObject);
                }
                done = true;
            }
            else
            {
                weapon.GetComponent<ShootBest>().AddProjectileMissed();
                done = true;
                weapon.GetComponent<ShootBest>().AddTimeAlive(duration);
                Destroy(gameObject);
            }
            if (done)
            {
                weapon.GetComponent<ShootBest>().AddTimeAlive(duration);
                Destroy(gameObject);
            }
        }
    }

    void ColorBullet(float color)
    {
        if (gameObject.tag == "FireBullet")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.1f * color, 1, 1);
        }
        else if (gameObject.tag == "WaterBullet")
        {
            gameObject.GetComponent<Renderer>().material.color = Color.HSVToRGB(0.47f + (0.13f * color), 1, 1);
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
        if (collision.tag == "Target"/*Change tag to enemy*/)
        {
            //Inflict damage to enemy
            //print("hit");
            hitTarget = true;
            weapon.GetComponent<ShootBest>().AddProjectileOnTarget();

            targetsHit[collision.gameObject.GetComponent<Target>().GetNumber()]++;
            if (!collision.isTrigger)
                Destroy(gameObject);
        }
        if (collision.tag == "Border")
        {
            weapon.GetComponent<ShootBest>().AddProjectileOnObstacles();
            weapon.GetComponent<ShootBest>().AddTimeAlive(Time.time - timeStart);
            Destroy(gameObject);
        }
        if(collision.tag == "Bounds")
        {
            weapon.GetComponent<ShootBest>().AddProjectileOOB();
            weapon.GetComponent<ShootBest>().AddTimeAlive(Time.time - timeStart);
            Destroy(gameObject);
        }
        if(collision.tag == "IntermediateBorder")
        {
            weapon.GetComponent<ShootBest>().AddHitOnIntermediateObstacles();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "OOB")
        {
            //weapon.GetComponent<ShootBest>().AddProjectileOOB();
            Destroy(gameObject);
        }
    }
}
