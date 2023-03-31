using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct BirthCertificate
{
    public string parentA;
    public int parentABoxes;
    public int parentALove;
    public float parentALifetime;
    public float parentABoxWeight;
    public float parentABoatWeight;
    public string parentB;
    public int parentBBoxes;
    public int parentBLove;
    public float parentBLifetime;
    public float parentBBoxWeight;
    public float parentBBoatWeight;
    public string child;
    public float childBoxWeight;
    public float childBoatWeight;
    public float timeOfBirth;
    public int parentAIndex;
    public int parentBIndex;

    public override string ToString()
    {
        return $"{parentA};{parentABoxes};{parentALove};{parentALifetime};{parentABoxWeight};{parentABoatWeight};{parentAIndex};" +
               $"{parentB};{parentBBoxes};{parentBLove};{parentBLifetime};{parentBBoxWeight};{parentBBoatWeight};{parentBIndex};" +
               $"{child};{childBoxWeight};{childBoatWeight};{timeOfBirth}";
    }
}

public struct DeathCertificate
{
    public string name;
    public int boxes;
    public int loveBoxes;
    public int children;
    public float lifeTime;
    public int index;

    public override string ToString()
    {
        return $"{name};{index};{boxes};{loveBoxes};{children};{lifeTime}";
    }
}

public class GenerationManager : MonoBehaviour
{
    public static GenerationManager Instance { get; private set; }

    [Header("Generators")] [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;

    [SerializeField] private GenerateObjectsInArea boatGenerator;
    [SerializeField] private GenerateObjectsInArea pirateGenerator;

    [Space(10)] [Header("Parenting and Mutation")] [SerializeField]
    private float mutationFactor;

    [SerializeField] private float mutationChance;
    [SerializeField] private int boatParentSize;
    [SerializeField] private int pirateParentSize;

    [Space(10)] [Header("Simulation Controls")] [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;

    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;

    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;

    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    [SerializeField] private List<BirthCertificate> certificates;
    [SerializeField] private List<DeathCertificate> deaths = new List<DeathCertificate>();

    [Space(10)] [Header("Prefab Saving")] [SerializeField]
    private string savePrefabsAt;

    private string childsData;
    //public int index;

    [SerializeField] private float simulationTime;

    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")] [SerializeField]
    private AgentData lastBoatWinnerData;

    [SerializeField] private AgentData lastPirateWinnerData;

    private bool _runningSimulation;
    private List<BoatLogic> _activeBoats;
    private List<PirateLogic> _activePirates;
    private BoatLogic[] _boatParents;
    private PirateLogic[] _pirateParents;

    public void AddToDeaths(BoatLogic boat, string name, int boxes,int loveBoxes,int children,float lifeTime, int index)
    {
        //_activeBoats.Remove(boat);
        deaths.Add(new DeathCertificate
        {
            name = name,
            boxes = boxes,
            loveBoxes = loveBoxes,
            children = children,
            lifeTime = lifeTime,
            index = index
        });
    }

    private void Awake()
    {
        Random.InitState(6);
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartSimulation();
        }
    }

    private void Update()
    {
        if (simulationTime > 0)
        {
            simulationTime -= Time.deltaTime;
        }
        else 
        {
            
#if UNITY_EDITOR
                    // Application.Quit() does not work in the editor so
                    // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        }
    }

    private IEnumerator GenerateBoxesOverTime(float spawnTime, int count)
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnTime);
            foreach (GenerateObjectsInArea generateObjectsInArea in boxGenerators)
            {
                generateObjectsInArea.GenerateSomeBoxes((uint)count);
            }
        }
    }


    /// <summary>
    /// Generates the boxes on all box areas.
    /// </summary>
    public void GenerateBoxes()
    {
        foreach (GenerateObjectsInArea generateObjectsInArea in boxGenerators)
        {
            generateObjectsInArea.RegenerateObjects();
        }
    }

    /// <summary>
    /// Generates boats and pirates using the parents list.
    /// If no parents are used, then they are ignored and the boats/pirates are generated using the default prefab
    /// specified in their areas.
    /// </summary>
    /// <param name="boatParents"></param>
    /// <param name="pirateParents"></param>
    public void GenerateObjects(BoatLogic[] boatParents = null, PirateLogic[] pirateParents = null)
    {
        GenerateBoats(boatParents);
        GeneratePirates(pirateParents);
    }

    /// <summary>
    /// Generates the list of pirates using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created pirates will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="pirateParents"></param>
    private void GeneratePirates(PirateLogic[] pirateParents)
    {
        _activePirates = new List<PirateLogic>();
        List<GameObject> objects = pirateGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            PirateLogic pirate = obj.GetComponent<PirateLogic>();
            if (pirate != null)
            {
                _activePirates.Add(pirate);
                if (pirateParents != null)
                {
                    PirateLogic pirateParent = pirateParents[Random.Range(0, pirateParents.Length)];
                    pirate.Birth(pirateParent.GetData());
                }

                pirate.Mutate(mutationFactor, mutationChance);
                pirate.AwakeUp();
            }
        }
    }

    /// <summary>
    /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
    /// /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="boatParents"></param>
    private void GenerateBoats(BoatLogic[] boatParents)
    {
        int i = 0;
        _activeBoats = new List<BoatLogic>();
        List<GameObject> objects = boatGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            obj.name = "Josh" + i;
            i++;
            
            BoatLogic boat = obj.GetComponent<BoatLogic>();
            if (boat != null)
            {
                _activeBoats.Add(boat);
                if (boatParents != null)
                {
                    BoatLogic boatParent = boatParents[Random.Range(0, boatParents.Length)];
                    boat.Birth(boatParent.GetData());
                }

                boat.Mutate(mutationFactor, mutationChance);
                boat.AwakeUp();
            }
        }
    }

    public void GenerateChild(BoatLogic parentA, BoatLogic parentB)
    {
        Vector3 offset = new Vector3(2, 0, 2);
        var boatObject = Instantiate(boatGenerator.firstObject, parentA.transform.position + offset,
            Quaternion.identity);
        BoatLogic boat = boatObject.GetComponent<BoatLogic>();
        boat.BirthKid(parentA, parentB);
        boat.Mutate(mutationFactor, mutationChance);
        childsData = boat.GetData(boat);
        boatObject.name = $"{parentA.gameObject.name} & {parentB.gameObject.name} jr.";
        Debug.Log("Child welcomed to the world!");
        boat.AwakeUp();
        _activeBoats.Add(boat);

        certificates.Add(new BirthCertificate
        {
            parentA = parentA.name,
            parentABoxes = parentA.numberOfFood,
            parentALove = parentA.numberOfLoveBoxes,
            parentALifetime = parentA.lifeTime,
            parentABoatWeight = parentA.boatWeight,
            parentABoxWeight = parentA.boxWeight,
            parentB = parentB.name,
            parentBBoxes = parentB.numberOfFood,
            parentBLove = parentB.numberOfLoveBoxes,
            parentBLifetime = parentB.lifeTime,
            parentBBoatWeight = parentB.boatWeight,
            parentBBoxWeight = parentB.boxWeight,
            timeOfBirth = Time.time,
            child = boatObject.name,
            childBoatWeight = boat.boatWeight,
            childBoxWeight = boat.boxWeight,
            parentAIndex = parentA.LocalIndex,
            parentBIndex = parentB.LocalIndex
        });
    }

    public string GetDataChild()
    {
        return childsData;
    }

    /// <summary>
    /// Creates a new generation by using GenerateBoxes and GenerateBoats/Pirates.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
    /// will use the [generationCount] as an identifier.
    /// </summary>
    public void MakeNewGeneration()
    {
        Random.InitState(6);

        GenerateBoxes();

        //Fetch parents
        //_activeBoats.RemoveAll(item => item == null);
        _activeBoats.Sort();
        if (_activeBoats.Count == 0)
        {
            GenerateBoats(_boatParents);
        }

        _boatParents = new BoatLogic[boatParentSize];
        for (int i = 0; i < boatParentSize; i++)
        {
            _boatParents[i] = _activeBoats[i];
        }

        BoatLogic lastBoatWinner = _activeBoats[0];
        lastBoatWinner.name += "Gen-" + generationCount;
        lastBoatWinnerData = lastBoatWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastBoatWinner.gameObject, savePrefabsAt + lastBoatWinner.name + ".prefab");

        _activePirates.RemoveAll(item => item == null);
        _activePirates.Sort();
        _pirateParents = new PirateLogic[pirateParentSize];
        for (int i = 0; i < pirateParentSize; i++)
        {
            _pirateParents[i] = _activePirates[i];
        }

        PirateLogic lastPirateWinner = _activePirates[0];
        lastPirateWinner.name += "Gen-" + generationCount;
        lastPirateWinnerData = lastPirateWinner.GetData();
        PrefabUtility.SaveAsPrefabAsset(lastPirateWinner.gameObject, savePrefabsAt + lastPirateWinner.name + ".prefab");
        GenerateObjects(_boatParents, _pirateParents);
    }

    /// <summary>
    /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBoxes and GenerateObjects and
    /// then sets the _runningSimulation flag to true.
    /// </summary>
    public void StartSimulation()
    {
        Random.InitState(6);

        GenerateBoxes();
        GenerateObjects();
        _runningSimulation = true;

        StartCoroutine(GenerateBoxesOverTime(10.0f, 5));
    }

    /// <summary>
    /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
    /// It sets the _runningSimulation flag to true.
    /// </summary>
    public void ContinueSimulation()
    {
        MakeNewGeneration();
        _runningSimulation = true;
    }

    /// <summary>
    /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
    /// all boats and pirates to Sleep.
    /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        //_activeBoats.RemoveAll(item => item == null);
       //_activeBoats.ForEach(boat => boat.Sleep());
       // _activePirates.ForEach(pirate => pirate.Sleep());
    }

    private void OnDestroy()
    {
        using (TextWriter writer = new StreamWriter($"Assets/Results/resultsBirths{DateTime.Now:yyyy-M-dTHH:mm:sszzz}.txt", true))
        {
            foreach (var certificate in certificates)
            {
                writer.WriteLine(certificate.ToString());
            }

            writer.Close();
        }
        
        using (TextWriter writer = new StreamWriter($"Assets/Results/resultsDeaths{DateTime.Now:yyyy-M-dTHH:mm:sszzz}.txt", true))
        {
            foreach (var boat in _activeBoats)
            {
                boat?.GenerateDeathCertificate();
            }

            foreach (var certificate in deaths)
            {
                writer.WriteLine(certificate.ToString());
                
            }

            writer.Close();
        }

        AssetDatabase.Refresh();
    }

    private void SaveToFile(string data)
    {
        // string line = "";
        // using StreamReader sr = new StreamReader($"Assets/results{DateTime.Now.ToString(CultureInfo.InvariantCulture)}.txt");
        // while ((line = sr.ReadLine()) != null)
        // {
        //     Console.WriteLine(line);
        // }
    }
}