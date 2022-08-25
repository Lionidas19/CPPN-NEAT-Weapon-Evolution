using UnityEngine;
using System.Collections;
using SharpNeat.Phenomes;
using System.Collections.Generic;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using System;
using System.Xml;
using System.IO;
using System.Text;

public class Optimizer : MonoBehaviour {

    const int NUM_INPUTS = 3;
    const int NUM_OUTPUTS = 3;

    //StringBuilder str1;

    int populationSize;
    int previousActiveLevel = 0;

    [Header("Experiment parameters")]
    [Tooltip("How many times should the experiment run before producing the next generation.")]
    public int Trials;
    [Tooltip("How much time the experiment will last")]
    public float TrialDuration;
    /*[Tooltip("The fitness the experiment must reach before stopping")]
    public float StoppingFitness;
    [Tooltip("Toggle if you want the experiment to stop at the fitness above")]
    public bool StopAtFitness;*/
    [Tooltip("The generation the experiment must reach before stopping")]
    public int StoppingGeneration;
    [Tooltip("Toggle if you want the experiment to stop at the generation above")]
    public bool StopAtGeneration;

    bool EARunning;
    string popFileSavePath, champFileSavePath;
    string name;

    SimpleExperiment experiment;
    static NeatEvolutionAlgorithm<NeatGenome> _ea;

    [Header("The subject of the experiments")]
    public GameObject Unit;
    public GameObject RunBestUnit;
    public GameObject spawn;
    public int NumberOfSimulations;

    [Header("The testbeds for the weapon to evolve through")]
    public List<GameObject> TestBeds;

    Dictionary<IBlackBox, UnitController> ControllerMap = new Dictionary<IBlackBox, UnitController>();
    private DateTime startTime;
    private float timeLeft;
    private float accum;
    private int frames;
    private float updateInterval = 12;

    GameObject[] Target;
    GameObject[] IntermediateBorder;
    GameObject obj;

    #region csv stats
    double AvgFitness;
    double AvgWeaponFitness;
    double AvgProjectileFitness;
    double AvgTargetFitness;
    float AvgDamageDealt;
    float AvgProjectilesShot;
    float AvgOnTarget;
    float AvgTargetHits;
    float AvgInterBorderHit;
    float AvgObstaclesHit;
    float AvgOutOfBounds;
    float AvgMissed;
    float AvgNA;
    float AvgDamage;
    float AvgROF;
    float AvgDuration;
    float AvgProjNumber;
    float AvgProjSize;
    float AvgProjSpeed;
    float AvgWeaponInaccuracy;
    float AvgNodes;
    float AvgConnections;
    float AvgSine;
    float AvgCos;
    float AvgTan;
    float AvgTanh;
    float AvgBiSig;
    float AvgStep;
    float AvgRamp;
    #endregion

    [HideInInspector]
    public uint Generation;
    [HideInInspector]
    public double Fitness;

	// Use this for initialization
	void Start () {
        Utility.DebugLog = true;
        experiment = new SimpleExperiment();
        XmlDocument xmlConfig = new XmlDocument();
        //CppnStatic.xmlConfig = xmlConfig;
        TextAsset textAsset = (TextAsset)Resources.Load("experiment.config");
        xmlConfig.LoadXml(textAsset.text);
        experiment.SetOptimizer(this);

        experiment.Initialize("Bullets", xmlConfig.DocumentElement, NUM_INPUTS, NUM_OUTPUTS);

        if (!TestBeds[previousActiveLevel].active) TestBeds[previousActiveLevel].SetActive(true);

        populationSize = experiment.DefaultPopulationSize;

        InitializeResults();

        Target = GameObject.FindGameObjectsWithTag("Target");
        IntermediateBorder = GameObject.FindGameObjectsWithTag("IntermediateBorder");

        champFileSavePath = Application.persistentDataPath + string.Format("/{0}.champ.xml", Unit.name);
        popFileSavePath = Application.persistentDataPath + string.Format("/{0}.pop.xml", Unit.name);

        print(champFileSavePath);

        name = Unit.GetComponent<ShootTesting>().bullet.name;

        //str1 = new StringBuilder("" + Unit.name + ",Best Fitness,Best Weapon Stats Fitness,Best Projectile Behaviour Fitness,Best Target Fitness,Best Damage Dealt,Best Projectiles Shot,Best On Target,Best Targets Hit Number,Best Intermediate Borders Hit,Best Hit Obstacles,Best Out Of Bounds,Best Missed,Best Not Reached Goal,Best Damage,Best Rate of Fire,Best Duration,Best Projectile Number,Best Projectile Size,Best Projectile Speed,Best Inaccuracy,Best Node Number,Best Connection Number,Best Sine Number,Best Cosine Number,Best Tangent Number,Best Hyperbolic Tangent Number,Best Bipolar Sigmoid Number,Best Step Number,Best Ramp Number,Average Fitness,Fitness STD,Average Weapon Fitness,WFitness STD, Average Projectile Fitness,PFitness STD,Average Target Fitness,TFitness STD,Average Damage Dealt,DDSTD,Average Projectiles Shot,PS STD,Average On Target,OT STD,Average Targets Hit,Targets Hit STD,Average Intermediate Borders Hit,InterBorder STD,Average Hit Obstacles, Obstacles Hit STD,Average Out of Bounds,OOB STD,Average Missed,Missed STD,Average Not Reached Goal,NotReachedGoal STD,Average Damage,Damage STD,Average Rate of Fire,ROF STD,Average Duration,Duration STD,Average Projectile Number,Pno STD,Average Size,Size STD,Average Speed,Speed STD,Average Inaccuracy,Inaccuracy STD,Average Node Number, Node STD,Average Connection Number,Connection STD,Average Sine Number,Sine STD,Average Cosine Number,Cos STD,Average Tangent Number,Tan STD,Average Hyperbolic Tangent Number,Tanh STD,Average Bipolar Sigmoid Number,BiSig STD,Average Step Number,Step STD,Average Ramp Number,Ramp STD");
    }

    // Update is called once per frame
    void Update()
    {
      //  evaluationStartTime += Time.deltaTime;

        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        if (timeLeft <= 0.0)
        {
            var fps = accum / frames;
            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
            //   print("FPS: " + fps);
            if (fps < 10)
            {
                Time.timeScale = Time.timeScale - 1;
                print("Lowering time scale to " + Time.timeScale);
            }
        }
    }

    public void StartEA()
    {        
        Utility.DebugLog = true;
        Utility.Log("Starting PhotoTaxis experiment");
        // print("Loading: " + popFileLoadPath);
        _ea = experiment.CreateEvolutionAlgorithm(popFileSavePath);
        startTime = DateTime.Now;

        _ea.UpdateEvent += new EventHandler(ea_UpdateEvent);
        _ea.PausedEvent += new EventHandler(ea_PauseEvent);

        var evoSpeed = 5;

     //   Time.fixedDeltaTime = 0.045f;
        Time.timeScale = evoSpeed;       
        _ea.StartContinue();
        EARunning = true;
    }

    void ea_UpdateEvent(object sender, EventArgs e)
    {
        Utility.Log(string.Format("gen={0:N0} bestFitness={1:N6}",
            _ea.CurrentGeneration, _ea.Statistics._maxFitness));

        Fitness = _ea.Statistics._maxFitness;
        Generation = _ea.CurrentGeneration;
        Results.gen = (int)Generation;

        XmlWriterSettings _xwSettings = new XmlWriterSettings();
        _xwSettings.Indent = true;

        int index = 0;
        for(int i = 0; i < Results.fitness.Length; i++)
        {
            if((float)Fitness == Results.fitness[i])
            {
                index = i;
                break;
            }
        }

        if (Generation >= StoppingGeneration * TestBeds.Count)
        {
            StopEA();
        }

        int ActiveLevel = (int)Generation / StoppingGeneration;

        if (previousActiveLevel != ActiveLevel)
        {
            TestBeds[previousActiveLevel].SetActive(false);
            TestBeds[ActiveLevel].SetActive(true);
        }

        previousActiveLevel = ActiveLevel;

        //Append the results of each weapon of the current generation
        //ExportResults(index);
    }

    #region SaveDataToCSV
    /*void ExportResults(int index)
    {
        AvgFitness = 0;
        AvgWeaponFitness = 0;
        AvgTargetFitness = 0;
        AvgProjectileFitness = 0;
        AvgDamageDealt = 0;
        AvgProjectilesShot = 0;
        AvgOnTarget = 0;
        AvgTargetHits = 0;
        AvgInterBorderHit = 0;
        AvgObstaclesHit = 0;
        AvgOutOfBounds = 0;
        AvgMissed = 0;
        AvgNA = 0;
        AvgDamage = 0;
        AvgROF = 0;
        AvgDuration = 0;
        AvgProjNumber = 0;
        AvgProjSize = 0;
        AvgProjSpeed = 0;
        AvgWeaponInaccuracy = 0;
        AvgNodes = 0;
        AvgConnections = 0;
        AvgSine = 0;
        AvgCos = 0;
        AvgTan = 0;
        AvgTanh = 0;
        AvgBiSig = 0;
        AvgStep = 0;
        AvgRamp = 0;

        StringBuilder str = new StringBuilder("Weapon Number,Fitness,Weapon Fitness,Target Fitness,Projectile Fitness,Damage Dealt,Projectiles Shot,On Target,Targets Hit,Intermediate Borders Hit,Obsctacles Hit,Out Of Bounds,Missed, Not Reached Goal, Damage, Rate of Fire, Duration, Projectile Number, Projectile Size, Projectile Speed, Weapon Inaccuracy, Node Number, Connection Number, Sine Number, Cosine Number, Tangent Number, Hyperbolic Tangent Number, Bipolar Sigmoid Number, Step Number, Ramp Number");
        
        for (int i = 0; i < 15; i++)
        {
            str.Append("\nWeapon " + (i + 1)).Append("," + Results.fitness[i]).Append("," + Results.weaponFitness[i]).Append("," + Results.targetFitness[i]).Append("," + Results.projectileFitness[i]).Append("," + Results.DamageDealt[i]).Append("," + Results.projectilesShot[i]).Append("," + Results.projectilesOnTarget[i]).Append("," + Results.hitsOnTargets[i]).Append("," + Results.IntermediateTargetsHit[i]).Append("," + Results.projectilesHitBorders[i]).Append("," + Results.projectilesOutOfBounds[i]).Append("," + Results.projectilesMissed[i]).Append("," + Results.projectilesNA[i]).Append("," + Results.Damage[i]).Append("," + Results.ROF[i]).Append("," + Results.Duration[i]).Append("," + Results.ProjectileNo[i]).Append("," + Results.Size[i]).Append("," + Results.Speed[i]).Append("," + Results.Inaccuracy[i]).Append("," + Results.Nodes[i]).Append("," + Results.Connections[i]).Append("," + Results.ActivationFunctions[i, 0]).Append("," + Results.ActivationFunctions[i, 1]).Append("," + Results.ActivationFunctions[i, 2]).Append("," + Results.ActivationFunctions[i, 3]).Append("," + Results.ActivationFunctions[i, 4]).Append("," + Results.ActivationFunctions[i, 5]).Append("," + Results.ActivationFunctions[i, 6]);

            AvgFitness += Results.fitness[i];
            AvgWeaponFitness += Results.weaponFitness[i];
            AvgTargetFitness += Results.targetFitness[i];
            AvgProjectileFitness += Results.projectileFitness[i];
            AvgDamage += Results.Damage[i];
            AvgDamageDealt += Results.DamageDealt[i];
            AvgProjectilesShot += Results.projectilesShot[i];
            AvgDuration += Results.Duration[i];
            AvgMissed += Results.projectilesMissed[i];
            AvgNA += Results.projectilesNA[i];
            AvgOnTarget += Results.projectilesOnTarget[i];
            AvgTargetHits += Results.hitsOnTargets[i];
            AvgInterBorderHit += Results.IntermediateTargetsHit[i];
            AvgObstaclesHit += Results.projectilesHitBorders[i];
            AvgOutOfBounds += Results.projectilesOutOfBounds[i];
            AvgProjNumber += Results.ProjectileNo[i];
            AvgROF += Results.ROF[i];
            AvgProjSize += Results.Size[i];
            AvgProjSpeed += Results.Speed[i];
            AvgWeaponInaccuracy += Results.Inaccuracy[i];
            AvgNodes += Results.Nodes[i];
            AvgConnections += Results.Connections[i];
            AvgSine += Results.ActivationFunctions[i, 0];
            AvgCos += Results.ActivationFunctions[i, 1];
            AvgTan += Results.ActivationFunctions[i, 2];
            AvgTanh += Results.ActivationFunctions[i, 3];
            AvgBiSig += Results.ActivationFunctions[i, 4];
            AvgStep += Results.ActivationFunctions[i, 5];
            AvgRamp += Results.ActivationFunctions[i, 6];

        }
        var filePath = Application.persistentDataPath + "/" + name + "/gen" + Results.gen + ".csv";

        using (var writer = new StreamWriter(filePath, false))
        {
            writer.Write(str);
        }

        AvgFitness /= 15;
        AvgWeaponFitness /= 15;
        AvgTargetFitness /= 15;
        AvgProjectileFitness /= 15;
        AvgDamage /= 15;
        AvgDamageDealt /= 15;
        AvgProjectilesShot /= 15;
        AvgDuration /= 15;
        AvgMissed /= 15;
        AvgNA /= 15;
        AvgOnTarget /= 15;
        AvgTargetHits /= 15;
        AvgInterBorderHit /= 15;
        AvgObstaclesHit /= 15;
        AvgOutOfBounds /= 15;
        AvgProjNumber /= 15;
        AvgROF /= 15;
        AvgProjSize /= 15;
        AvgProjSpeed /= 15;
        AvgWeaponInaccuracy /= 15;
        AvgNodes /= 15;
        AvgConnections /= 15;
        AvgSine /= 15;
        AvgCos /= 15;
        AvgTan /= 15;
        AvgTanh /= 15;
        AvgBiSig /= 15;
        AvgStep /= 15;
        AvgRamp /= 15;

        //Append a new line of data to the generational file
        str1.Append("\nGen: " + Generation + "," + Fitness + "," + Results.weaponFitness[index] + "," + Results.projectileFitness[index] + "," + Results.targetFitness[index] + "," + Results.DamageDealt[index] + "," + Results.projectilesShot[index] + "," + Results.projectilesOnTarget[index] + "," + Results.hitsOnTargets[index] + "," + Results.IntermediateTargetsHit[index] + "," + Results.projectilesHitBorders[index] + "," + Results.projectilesOutOfBounds[index] + "," + Results.projectilesMissed[index] + "," + Results.projectilesNA[index] + "," + Results.Damage[index] + "," + Results.ROF[index] + "," + Results.Duration[index] + "," + Results.ProjectileNo[index] + "," + Results.Size[index] + "," + Results.Speed[index] + "," + Results.Inaccuracy[index] + "," + Results.Nodes[index] + "," + Results.Connections[index] + "," + Results.ActivationFunctions[index, 0] + "," + Results.ActivationFunctions[index, 1] + "," + Results.ActivationFunctions[index, 2] + "," + Results.ActivationFunctions[index, 3] + "," + Results.ActivationFunctions[index, 4] + "," + Results.ActivationFunctions[index, 5] + "," + Results.ActivationFunctions[index, 6] + "," +
            AvgFitness + "," + STD(Results.fitness, (float)AvgFitness) + "," + 
            AvgWeaponFitness + "," + STD(Results.weaponFitness, (float)AvgWeaponFitness) + "," + 
            AvgProjectileFitness + "," + STD(Results.projectileFitness, (float)AvgProjectileFitness) + "," +
            AvgTargetFitness + "," + STD(Results.targetFitness, (float)AvgTargetFitness) + "," +
            AvgDamageDealt + "," + STD(Results.DamageDealt, AvgDamageDealt) + "," +
            AvgProjectilesShot + "," + STD(Results.projectilesShot, AvgProjectilesShot) + "," +
            AvgOnTarget + "," + STD(Results.projectilesOnTarget, AvgOnTarget) + "," +
            AvgTargetHits + "," + STD(Results.hitsOnTargets, AvgTargetHits) + "," +
            AvgInterBorderHit + "," + STD(Results.IntermediateTargetsHit, AvgInterBorderHit) + "," +
            AvgObstaclesHit + "," + STD(Results.projectilesHitBorders, AvgObstaclesHit) + "," +
            AvgOutOfBounds + "," + STD(Results.projectilesOutOfBounds, AvgOutOfBounds) + "," + 
            AvgMissed + "," + STD(Results.projectilesMissed, AvgMissed) + "," +
            AvgNA + "," + STD(Results.projectilesNA, AvgNA) + "," +
            AvgDamage + "," + STD(Results.Damage, AvgDamage) + "," + 
            AvgROF + "," + STD(Results.ROF, AvgROF) + "," + 
            AvgDuration + "," + STD(Results.Duration, AvgDuration) + "," + 
            AvgProjNumber + "," + STD(Results.ProjectileNo, AvgProjNumber) + "," +
            AvgProjSize + "," + STD(Results.Size, AvgProjSize) + "," +
            AvgProjSpeed + "," + STD(Results.Speed, AvgProjSpeed) + "," +
            AvgWeaponInaccuracy + "," + STD(Results.Inaccuracy, AvgWeaponInaccuracy) + "," +
            AvgNodes + "," + STD(Results.Nodes, AvgNodes) + "," +
            AvgConnections + "," + STD(Results.Connections, AvgConnections) + "," +
            AvgSine + "," + STD(Results.ActivationFunctions, AvgSine, 0) + "," +
            AvgCos + "," + STD(Results.ActivationFunctions, AvgCos, 1) + "," +
            AvgTan + "," + STD(Results.ActivationFunctions, AvgTan, 2) + "," +
            AvgTanh + "," + STD(Results.ActivationFunctions, AvgTanh, 3) + "," +
            AvgBiSig + "," + STD(Results.ActivationFunctions, AvgBiSig, 4) + "," +
            AvgStep + "," + STD(Results.ActivationFunctions, AvgStep, 5) + "," +
            AvgRamp + "," + STD(Results.ActivationFunctions, AvgRamp, 6) 
            );
    }*/

    /*float STD(int[,] array, float avg, int index)
    {
        float sumOfSquares = 0;

        for (int i = 0; i < array.GetLength(0); i++)
        {
            sumOfSquares += (array[i,index] - avg) * (array[i,index] - avg);
        }
        return (float)Math.Sqrt(sumOfSquares / (array.Length - 1));
    }

    float STD(float[] array, float avg)
    {
        float sumOfSquares = 0;

        for(int i = 0; i < array.Length; i++)
        {
            sumOfSquares += (array[i] - avg) * (array[i] - avg);
        }
        return (float)Math.Sqrt(sumOfSquares / (array.Length -1));
    }

    float STD(int[] array, float avg)
    {
        float sumOfSquares = 0;

        for (int i = 0; i < array.Length; i++)
        {
            sumOfSquares += (array[i] - avg) * (array[i] - avg);
        }
        return (float)Math.Sqrt(sumOfSquares / (array.Length - 1));
    }*/

    #endregion

    void ea_PauseEvent(object sender, EventArgs e)
    {
        Debug.Log("" + Unit.name + " is done.");
        Time.timeScale = 1;
        Utility.Log("Done ea'ing (and neat'ing)");

        //ExportResults(index);

        XmlWriterSettings _xwSettings = new XmlWriterSettings();
        _xwSettings.Indent = true;
        // Save genomes to xml file.        
        DirectoryInfo dirInf = new DirectoryInfo(Application.persistentDataPath);
        if (!dirInf.Exists)
        {
            Debug.Log("Creating subdirectory");
            dirInf.Create();
        }
        using (XmlWriter xw = XmlWriter.Create(popFileSavePath, _xwSettings))
        {
            experiment.SavePopulation(xw, _ea.GenomeList);
        }
             
        // Also save the best genome

        using (XmlWriter xw = XmlWriter.Create(champFileSavePath, _xwSettings))
        {
            experiment.SavePopulation(xw, new NeatGenome[] { _ea.CurrentChampGenome });
        }

        //Save CSV file
        /*var filePath = Application.persistentDataPath + "/" + name + ".csv";

        using (var writer = new StreamWriter(filePath, false))
        {
            writer.Write(str1);
        }*/

        DateTime endTime = DateTime.Now;
        Utility.Log("Total time elapsed: " + (endTime - startTime));

        System.IO.StreamReader stream = new System.IO.StreamReader(popFileSavePath);
       
        EARunning = false;        
        
    }

    public void StopEA()
    {

        if (_ea != null && _ea.RunState == SharpNeat.Core.RunState.Running)
        {
            _ea.Stop();
        }
    }

    public void Evaluate(IBlackBox box)
    {
        GameObject obj = Instantiate(Unit, spawn.transform.position, Unit.transform.rotation) as GameObject;
        UnitController controller = obj.GetComponent<UnitController>();

        ControllerMap.Add(box, controller);

        controller.Activate(box);
    }

    public float[] GetStats()
    {
        return _ea.GenomeList[0]._stats;
    }

    public float[] GetStats(int index)
    {
        return _ea.GenomeList[index]._stats;
    }

    private void InitializeResults()
    {
        Results.fitness = new float[populationSize];
        Results.weaponFitness = new float[populationSize];
        Results.targetFitness = new float[populationSize];
        Results.projectileFitness = new float[populationSize];

        Results.projectilesShot = new int[populationSize];

        Results.projectilesOnTarget = new int[populationSize];

        Results.IntermediateTargetsHit = new int[populationSize];

        Results.hitsOnTargets = new int[populationSize];

        Results.projectilesHitBorders = new int[populationSize];

        Results.projectilesOutOfBounds = new int[populationSize];

        Results.projectilesMissed = new int[populationSize];

        Results.projectilesNA = new int[populationSize];

        Results.Damage = new float[populationSize];

        Results.ROF = new float[populationSize];

        Results.Duration = new float[populationSize];

        Results.Size = new float[populationSize];

        Results.Speed = new float[populationSize];

        Results.Inaccuracy = new float[populationSize];

        Results.ProjectileNo = new int[populationSize];

        Results.DamageDealt = new float[populationSize];

        Results.Nodes = new int[populationSize];
        Results.Connections = new int[populationSize];
        Results.ActivationFunctions = new int[populationSize, 7];
    }

    public NeatEvolutionAlgorithm<NeatGenome> GetEA()
    {
        return _ea;
    }

    public void StopEvaluation(IBlackBox box)
    {
        UnitController ct = ControllerMap[box];

        Destroy(ct.gameObject);
    }

    public void RunBest(Vector2 position, Quaternion rotation)
    {
        Time.timeScale = 1;

        NeatGenome genome = null;

        Debug.Log("Running Best with args");

        // Try to load the genome from the XML document.
        try
        {
            using (XmlReader xr = XmlReader.Create(champFileSavePath))
                genome = NeatGenomeXmlIO.ReadCompleteGenomeList(xr, true, (NeatGenomeFactory)experiment.CreateGenomeFactory())[0];

        }
        catch (Exception e1)
        {
            Debug.Log("" + e1.Message);
            // print(champFileLoadPath + " Error loading genome from file!\nLoading aborted.\n"
            //						  + e1.Message + "\nJoe: " + champFileLoadPath);
            return;
        }

        // Get a genome decoder that can convert genomes to phenomes.
        var genomeDecoder = experiment.CreateGenomeDecoder();

        // Decode the genome into a phenome (neural network).
        var phenome = genomeDecoder.Decode(genome);

        GameObject obj = Instantiate(RunBestUnit, position, rotation) as GameObject;
        UnitController controller = obj.GetComponent<UnitController>();
        obj.GetComponent<ShootBest>().SetStats(genome._stats);

        ControllerMap.Add(phenome, controller);

        controller.Activate(phenome);
    }

    public float GetFitness(IBlackBox box)
    {
        if (ControllerMap.ContainsKey(box))
        {
            return ControllerMap[box].GetFitness();
        }
        return 0;
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 200, 80), "Start EA"))
        {
            StartEA();
        }
        if (GUI.Button(new Rect(10, 110, 200, 80), "Stop EA"))
        {
            StopEA();
        }
        if (GUI.Button(new Rect(10, 210, 200, 80), "Run best"))
        {
            RunBest(spawn.transform.position, spawn.transform.rotation);
        }

        GUI.Button(new Rect(10, Screen.height - 70, 100, 60), string.Format("Generation: {0}\nFitness: {1:0.00}", Generation, Fitness));
    }
}
