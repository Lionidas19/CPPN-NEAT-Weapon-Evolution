using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using System.Text;
using System.IO;

public class ShootTesting : UnitController
{
    float nextActionTime;
    float period;
    public GameObject bullet;
    GameObject[] Target;
    GameObject[] IntermediateBorder;
    NeatEvolutionAlgorithm<NeatGenome> _ea;

    float fitness;
    float DpsFit;
    float TargetFitness;
    float ProjectileFitness;

    int ProjectileNumber;
    float Damage;
    float DamageDealt;
    float DamageTarget;
    float Duration;

    float angle;
    float projectileSize;
    float speed;
    [HideInInspector]
    public int individual;

    float distance;
    Vector2 farthestTarget;

    int[] hitsOnTarget;
    int hitsOnIntermediateBorder;
    int projectilesHitBorders;
    int projectilesOutOfBounds;
    int projectilesOnTarget;
    int projectilesMissed;
    int projectilesShot;
    int hitsOnTargets;

    bool IsRunning;
    [HideInInspector]
    public IBlackBox box;

    // Start is called before the first frame update
    void Start()
    {

        GameObject evaluator = GameObject.Find("Evaluator");

        _ea = evaluator.GetComponent<Optimizer>().GetEA();

        Results.order++;
        if (Results.order > _ea.GenomeList.Count)
        {
            Results.order = 1;
        }
        individual = Results.order;

        projectilesShot = 0;
        fitness = 0;
        TargetFitness = 0;
        ProjectileFitness = 0;
        DpsFit = 0;
        hitsOnTargets = 0;
        /*shots = new List<int>();*/

        nextActionTime = 0f;

        Target = GameObject.FindGameObjectsWithTag("Target");
        hitsOnTarget = new int[Target.Length];

        IntermediateBorder = GameObject.FindGameObjectsWithTag("IntermediateBorder");

        projectilesHitBorders = 0;
        projectilesOutOfBounds = 0;
        projectilesOnTarget = 0;
        projectilesMissed = 0;

        farthestTarget = FarthestTarget();
        distance = Vector2.Distance(gameObject.transform.position, farthestTarget);
        
        DamageDealt = 0;
        DamageTarget = 500;

        float[] stats = _ea.GenomeList[individual - 1]._stats;
        SetStats(stats);
        SetResults(individual - 1);        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (IsRunning) 
        {
            if (Time.fixedTime - nextActionTime > period)
            {
                nextActionTime = Time.fixedTime;
                Fire();
            }

            CalculateTargetFitness();
            
            SaveResults();
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
            float successfulProjectilesRatio = (float)projectilesOnTarget / (float)projectilesShot;

            ProjectileFitness = Mathf.Min(successfulProjectilesRatio, 1f);

            if (IntermediateBorder.Length > 0)
            {
                if (projectilesOnTarget == 0)
                    ProjectileFitness /= 2;
                else
                {
                    float interToSuccesfull = (float)hitsOnIntermediateBorder / (float)projectilesOnTarget;
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
    }

    void SaveResults()
    {
        Results.fitness[individual - 1] = fitness;
        Results.weaponFitness[individual - 1] = DpsFit;
        Results.targetFitness[individual - 1] = TargetFitness;
        Results.projectileFitness[individual - 1] = ProjectileFitness;
        Results.projectilesShot[individual - 1] = projectilesShot;
        Results.projectilesOnTarget[individual - 1] = projectilesOnTarget;
        Results.IntermediateTargetsHit[individual - 1] = hitsOnIntermediateBorder;
        Results.hitsOnTargets[individual - 1] = hitsOnTargets;
        Results.projectilesHitBorders[individual - 1] = projectilesHitBorders;
        Results.projectilesOutOfBounds[individual - 1] = projectilesOutOfBounds;
        Results.projectilesMissed[individual - 1] = projectilesMissed;
        Results.projectilesNA[individual - 1] = projectilesShot - projectilesOnTarget - projectilesOutOfBounds - projectilesMissed;
        Results.DamageDealt[individual - 1] = DamageDealt;
        Results.Damage[individual - 1] = Damage;
        Results.ROF[individual - 1] = period;
        Results.Duration[individual - 1] = Duration;
        Results.ProjectileNo[individual - 1] = ProjectileNumber;
        Results.Size[individual - 1] = projectileSize;
        Results.Speed[individual - 1] = speed;
        Results.Inaccuracy[individual - 1] = angle;
    }

    float NormalizeStrict(float variable)
    {
        return 0.5f + 0.5f * (float)System.Math.Tanh((6 * variable * variable) - 2);
    }

    float NormalizeLax(float variable)
    {
        return 0.5f + 0.5f * (float)System.Math.Tanh((4 * variable) - 2);
    }

    Vector2 FarthestTarget()
    {
        float dist = 0;
        foreach (GameObject target in Target)
        {
            if (Vector2.Distance(gameObject.transform.position, target.transform.position) > dist)
            {
                farthestTarget = target.transform.position;
                dist = Vector2.Distance(gameObject.transform.position, target.transform.position);
            }
        }
        return farthestTarget;
    }

    //linePnt - point the line passes through
    //lineDir - unit vector in direction of line, either direction works
    //pnt - the point to find nearest on line for
    public static Vector2 NearestPointOnLine(Vector2 linePnt, Vector2 lineDir, Vector2 pnt)
    {
        lineDir.Normalize();//this needs to be a unit vector
        var v = pnt - linePnt;
        var d = Vector3.Dot(v, lineDir);
        return linePnt + lineDir * d;
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

    public override float GetFitness()
    {
        return fitness;
    }

    public void AddToHit(int targetHit, int numberOfHits)
    {
        DamageDealt += numberOfHits * Damage;
        hitsOnTarget[targetHit] += numberOfHits;
        hitsOnTargets += numberOfHits;
    }

    public void AddSuccesfullProjectile()
    {
        projectilesOnTarget += 1;
    }

    public void AddToHitBorders()
    {
        projectilesHitBorders++;
    }

    public void AddToOutOfBounds()
    {
        projectilesOutOfBounds++;
    }

    public void AddToMissed()
    {
        projectilesMissed++;
    }

    public void AddToIntermediateBorders(int hits)
    {
        hitsOnIntermediateBorder += hits;
    }

    void SetStats(float[] stats)
    {
        ProjectileNumber = Mathf.RoundToInt(stats[0]);
        Damage = stats[1];
        Duration = stats[2];
        period = stats[3];
        projectileSize = stats[4];
        speed = stats[5];
        angle = stats[6];
    }

    void SetResults(int index)
    {
        Results.Nodes[index] = _ea.GenomeList[index].NeuronGeneList.Count;
        Results.Connections[index] = _ea.GenomeList[index].ConnectionList.Count;
        for (int i = 0; i < 7; i++)
        {
            Results.ActivationFunctions[index, i] = 0;
        }
        for (int i = 0; i < Results.Nodes[index]; i++)
        {
            Results.ActivationFunctions[index, _ea.GenomeList[index].NeuronGeneList[i].ActivationFnId]++;
        }
    }

    public void ClosestToTarget(float dist)
    {
        if (dist < distance)
            distance = dist;
    }

    public float GetDuration()
    {
        return Duration;
    }

    public float GetSize()
    {
        return projectileSize;
    }

    public float GetSpeed()
    {
        return speed;
    }
    
    void Fire()
    {
        for (int  i = 0; i < ProjectileNumber; i++)
        {
            projectilesShot++;
            float projectileAngle = gaussian(-angle, angle);
            Quaternion rot = Quaternion.Euler(0, 0, projectileAngle + gameObject.transform.rotation.eulerAngles.z / 2);
            Instantiate(bullet, gameObject.transform.position, rot, gameObject.transform);
        }
    }

    float gaussian(float min, float max)
    {
        float r1, r2, squareSum;

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
