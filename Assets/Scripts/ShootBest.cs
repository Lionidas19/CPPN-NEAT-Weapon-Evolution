using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpNeat.Phenomes;
using System.Text;
using System.IO;

public class ShootBest : UnitController
{

    float fitness;
    float DpsFit;
    float TargetFitness;
    float ProjectileFitness;

    GameObject[] Target;
    GameObject[] IntermediateObstacle;

    float nextActionTime;
    float period;
    float TrialDuration;
    float StartTime;
    bool IsDone;
    public GameObject shot;

    int ProjectileNumber;
    float Damage;
    float Duration;
    float projectileSize;
    float projectileSpeed;
    float accuracy;
    float[] _stats = new float[7];

    int projectilesOnTarget;
    int projectilesOOB;
    int projectilesMissed;
    int projectilesOnObstacles;
    int hitsOnIntermediateObstacles;
    int successfulProjectiles;
    float averageTimeAlive;
    int ShotsFired;
    int hitsOnTargets;

    int Runs;
    int MaxSimulationNumber;

    int[] hitsOnTarget;
    int projectilesHitBorders;
    int projectilesOutOfBounds;
    int projectilesShot;
    float DamageDealt;
    float DamageTarget;

    float distance;
    Vector2 farthestTarget;

    StringBuilder str1;
    StringBuilder strFit;

    bool IsRunning;
    [HideInInspector]
    public IBlackBox box;
    // Start is called before the first frame update
    void Start()
    {
        GameObject evaluator = GameObject.Find("Evaluator");
        MaxSimulationNumber = evaluator.GetComponent<Optimizer>().NumberOfSimulations;
        Time.timeScale = 30;
        TrialDuration = 30;
        StartTime = Time.fixedTime;
        nextActionTime = -period;
        IsDone = false;
        DamageTarget = 500;
        hitsOnTargets = 0;

        Target = GameObject.FindGameObjectsWithTag("Target");
        hitsOnTarget = new int[Target.Length];

        IntermediateObstacle = GameObject.FindGameObjectsWithTag("IntermediateBorder");

        Debug.Log("Starting the first of " + MaxSimulationNumber + " simulations");

        Runs = 0;
        //Save simulation data
        /*str1 = new StringBuilder("" + shot.name + "-runs, Times Shot(Projectile Number = " + ProjectileNumber + "), Successful Projectiles, Projectiles on Target, Projectiles missed, Projectiles on obstacles, Hits on intermediate obstacles, Projectiles out of bounds, Average Time Alive(Duration = " + Duration +"), Pno, Dur");
        strFit = new StringBuilder("");*/
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        if (IsRunning && !IsDone)
        {
            if(Runs < MaxSimulationNumber)
            {
                if (Time.fixedTime - TrialDuration < StartTime)
                {
                    if (Time.fixedTime - nextActionTime > period)
                    {
                        nextActionTime = Time.fixedTime;
                        Fire();
                        ShotsFired++;
                        projectilesShot += ProjectileNumber;
                    }
                }
                else
                {
                    Runs++;
                    CalculateTargetFitness();
                    //SaveData();
                    Reset();
                }
            }
            else
            {
                IsDone = !IsDone;

                //Save Simulation Data
                /*var filePath1 = Application.persistentDataPath + "/" + gameObject.name + ".csv";
                var filePath2 = Application.persistentDataPath + "/" + gameObject.name + "Fitness.csv";

                using (var writer = new StreamWriter(filePath1, false))
                {
                    writer.Write(str1);
                }
                using (var writer = new StreamWriter(filePath2, false))
                {
                    writer.Write(strFit);
                }*/
            }
        }
    }

    void CalculateTargetFitness()
    {
        TargetFitness = 0;
        ProjectileFitness = 0;
        DpsFit = 0;
        fitness = 0;

        if (projectilesOnTarget != 0)
        {

            //Each target has to be hit
            for (int i = 0; i < Target.Length; i++)
                if (hitsOnTarget[i] > 0)
                    TargetFitness++;

            TargetFitness /= Target.Length;

            //The more hits on the intermediate targets we have the lower the fitness will be
            //The more projectiles miss the targets the lower the fitness will be
            float successfulProjectilesRatio = (float)successfulProjectiles / (float)projectilesShot;

            ProjectileFitness = Mathf.Min(successfulProjectilesRatio, 1f);
            float interToSuccesfull = 0;
            if (IntermediateObstacle.Length > 0)
            {
                if (successfulProjectiles == 0)
                    ProjectileFitness /= 2;
                else
                {
                    interToSuccesfull = (float)hitsOnIntermediateObstacles / (float)successfulProjectiles;
                    ProjectileFitness = (ProjectileFitness + Mathf.Exp(-0.1f * interToSuccesfull * interToSuccesfull)) / 2f;
                }
            }

            //The Damage that has been dealt at the end of an experiment should approach the Damage Target
            if (DamageDealt <= DamageTarget)
                DpsFit = DamageDealt / DamageTarget;
            else
                DpsFit = DamageTarget / DamageDealt;

            fitness = Mathf.Abs(((DpsFit * NormalizeStrict(ProjectileFitness)) + TargetFitness + (ProjectileFitness * NormalizeLax(TargetFitness))) / 3);
        }
        // If no projectiles have hit any of the targets then reward the weapons whose projectiles reached as close to the farthest target as possible
        else
        {
            fitness = Mathf.Max((1 - (distance / Vector2.Distance(farthestTarget, gameObject.transform.position))), 0) / 1000;
        }
        Debug.Log("This run's fitness was: " + fitness);
    }

    float NormalizeStrict(float variable)
    {
        return 0.5f + 0.5f * (float)System.Math.Tanh((6 * variable * variable) - 2);
    }

    float NormalizeLax(float variable)
    {
        return 0.5f + 0.5f * (float)System.Math.Tanh((4 * variable) - 2);
    }

    public override float GetFitness()
    {
        return 0;
    }

    public override void Stop() 
    {
        this.IsRunning = false;
    }

    public override void Activate(IBlackBox box)
    {
        this.box = box;
        this.IsRunning = true;
    }

    public void SetStats(float[] stats)
    {
        for (int j = 0; j < 7; j++)
        {
            _stats[j] = stats[j];
        }
        ProjectileNumber = (int)_stats[0];
        Damage = _stats[1];
        Duration = _stats[2];
        period = _stats[3];
        projectileSize = _stats[4];
        projectileSpeed = _stats[5];
        accuracy = _stats[6];
    }

    void Fire()
    {
        GameObject tempParent = Instantiate(shot, transform.position, transform.rotation) as GameObject;
        tempParent.GetComponent<Shot>().SetParameters(box,ProjectileNumber, Damage, Duration, projectileSpeed, projectileSize, accuracy, name);
        tempParent.GetComponent<Shot>().Shoot();
        Destroy(tempParent, Duration + 1);
    }

    public void AddProjectileOnTarget()
    {
        projectilesOnTarget++;
    }
    public void AddProjectileOOB()
    {
        projectilesOOB++;
    }
    public void AddProjectileMissed()
    {
        projectilesMissed++;
    }
    public void AddProjectileOnObstacles()
    {
        projectilesOnObstacles++;
    }
    public void AddHitOnIntermediateObstacles()
    {
        hitsOnIntermediateObstacles++;
    }
    public void AddSuccessfulProjectiles()
    {
        successfulProjectiles++;
    }
    public void AddTimeAlive(float time)
    {
        averageTimeAlive += time;
    }
    public void AddToHit(int targetHit, int numberOfHits)
    {
        DamageDealt += numberOfHits * Damage;
        hitsOnTarget[targetHit] += numberOfHits;
        hitsOnTargets += numberOfHits;
    }

    void SaveData()
    {
        /*str1.Append("\n" + Runs + "," + ShotsFired + "," + successfulProjectiles + "," + projectilesOnTarget + "," + projectilesMissed + "," + projectilesOnObstacles + "," + hitsOnIntermediateObstacles + "," + projectilesOOB + "," + averageTimeAlive/(ShotsFired* ProjectileNumber) + ",(" + ProjectileNumber + ")," + "(" + Duration + ")");
        strFit.Append(fitness + "\n");*/
    }
    private void Reset()
    {
        projectilesOnTarget = 0;
        successfulProjectiles = 0;
        projectilesOOB = 0;
        projectilesMissed = 0;
        projectilesOnObstacles = 0;
        hitsOnIntermediateObstacles = 0;
        averageTimeAlive = 0;
        fitness = 0;
        StartTime = Time.fixedTime;
        nextActionTime = -period;
        ShotsFired = 0;
        projectilesShot = 0;
        DamageDealt = 0;
        GameObject g = GameObject.Find(shot.name + "(Clone)");
        if (g != null)
        {
            Destroy(g);
        }
    }
}
