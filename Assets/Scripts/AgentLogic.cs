﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

/// <summary>
/// This struct helps to order the directions an Agent can take based on its utility.
/// Every Direction (a vector to where the Agent would move) has a utility value.
/// Higher utility values are expected to lead to better outcomes.
/// </summary>
struct AgentDirection : IComparable
{
    public Vector3 Direction { get; }
    public float Utility;

    public AgentDirection(Vector3 direction, float utility)
    {
        Direction = direction;
        Utility = utility;
    }
    
    /// <summary>
    /// Notices that this method is an "inverse" sorting. It makes the higher values on top of the Sort, instead of
    /// the smaller values. For the smaller values, the return line would be utility.CompareTo(otherAgent.utility).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        
        AgentDirection otherAgent = (AgentDirection)obj;
        return otherAgent.Utility.CompareTo(Utility);
    }
}

/// <summary>
/// This struct stores all genes / weights from an Agent.
/// It is used to pass this information along to other Agents, instead of using the MonoBehavior itself.
/// Also, it makes it easier to inspect since it is a Serializable struct.
/// </summary>
[Serializable]
public struct AgentData
{
    public int steps;
    public int rayRadius;
    public float sight;
    public float movingSpeed;
    public Vector2 randomDirectionValue;
    public float boxWeight;
    public float distanceFactor;
    public float boatWeight;
    public float boatDistanceFactor;


    public AgentData(int steps, int rayRadius, float sight, float movingSpeed, Vector2 randomDirectionValue, float boxWeight, float distanceFactor, float boatWeight, float boatDistanceFactor)
    {
        this.steps = steps;
        this.rayRadius = rayRadius;
        this.sight = sight;
        this.movingSpeed = movingSpeed;
        this.randomDirectionValue = randomDirectionValue;
        this.boxWeight = boxWeight;
        this.distanceFactor = distanceFactor;
        this.boatWeight = boatWeight;
        this.boatDistanceFactor = boatDistanceFactor;
    }
}

/// <summary>
/// Main script for the Agent behaviour.
/// It is responsible for caring its genes, deciding its actions and controlling debug properties.
/// The agent moves by using its rigidBody velocity. The velocity is set to its speed times the movementDirection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AgentLogic : MonoBehaviour, IComparable
{
    private Rigidbody _rigidbody;
    
    [SerializeField] 
    protected bool canReproduce;
    public float hasBoxTime;

    private bool _isAwake;

    [Header("Genes")]
    [SerializeField, Tooltip("Steps for the area of sight.")]
    private int steps;
    [SerializeField, Range(0.0f, 360.0f), Tooltip("Divides the 360˚ view of the Agent into rayRadius steps.")]
    private int rayRadius = 16;
    [SerializeField, Tooltip("Ray distance. For the front ray, the value of 1.5 * Sight is used.")]
    private float sight = 10.0f;
    [SerializeField]
    private float movingSpeed;
    [SerializeField, Tooltip("All directions starts with a random value from X-Y (Math.Abs, Math.Min and Math.Max are applied).")]
    private Vector2 randomDirectionValue;

    [Space(10)]
    [Header("Weights")]
    [SerializeField]
    public float boxWeight;
    [SerializeField]
    private float distanceFactor;
    [SerializeField]
    public float boatWeight;
    [SerializeField]
    private float boatDistanceFactor;

    [Space(10)]
    [Header("Debug & Help")] 
    [SerializeField]
    private Color visionColor;
    [SerializeField]
    private Color foundColor;
    [SerializeField]
    private Color directionColor;
    [SerializeField, Tooltip("Shows visualization rays.")] 
    private bool debug;

    #region Static Variables
    private static float _minimalSteps = 1.0f;
    private static float _minimalRayRadius = 1.0f;
    private static float _minimalSight = 0.1f;
    private static float _minimalMovingSpeed = 1.0f;
    private static float _speedInfluenceInSight = 0.1250f;
    private static float _sightInfluenceInSpeed = 0.0625f;
    private static float _maxUtilityChoiceChance = 0.85f;
    #endregion

    private float distanceIndex = 0;
    public float lifeTime = 0;
    public static int index;
    private int localIndex;

    public int LocalIndex => localIndex;

    private void Awake()
    {
        Initiate();
    }

    /// <summary>
    /// Initiate the values for this Agent, settings its points to 0 and recalculating its sight parameters.
    /// </summary>
    private void Initiate()
    {
        steps = 360 / rayRadius;
        lifeTime = 0;
        localIndex = index++;
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    /// <summary>
    /// Copies the genes / weights from the parent.
    /// </summary>
    /// <param name="parent"></param>
    public void Birth(AgentData parent)
    {
        steps = parent.steps;
        rayRadius = parent.rayRadius;
        sight = parent.sight;
        movingSpeed = parent.movingSpeed;
        randomDirectionValue = parent.randomDirectionValue;
        boxWeight = parent.boxWeight;
        distanceFactor = parent.distanceFactor;
        boatWeight = parent.boatWeight;
        boatDistanceFactor = parent.boatDistanceFactor;
    }

    public void BirthKid(BoatLogic parentA, BoatLogic parentB)
    {
        
            steps = Random.Range(0, 100) >= 50 ? parentA.steps : parentB.steps;
            rayRadius = Random.Range(0, 100) >= 50 ? parentA.rayRadius : parentB.rayRadius;
            sight = Random.Range(0, 100) >= 50 ? parentA.sight : parentB.sight;
            movingSpeed = Random.Range(0, 100) >= 50 ? parentA.movingSpeed : parentB.movingSpeed;
            randomDirectionValue = Random.Range(0, 100) >= 50 ? parentA.randomDirectionValue : parentB.randomDirectionValue;
            boxWeight = Random.Range(0, 100) >= 50 ? parentA.boxWeight : parentB.boxWeight;
            distanceFactor = Random.Range(0, 100) >= 50 ? parentA.distanceFactor : parentB.distanceFactor;
            boatWeight = Random.Range(0, 100) >= 50 ? parentA.boatWeight : parentB.boatWeight;
            boatDistanceFactor = Random.Range(0, 100) >= 50 ? parentA.boatDistanceFactor : parentB.boatDistanceFactor;
    }

    public string GetData(BoatLogic boat)
    {
        return boat.distanceIndex + "," + boat.boatDistanceFactor + "," + boat.boatWeight + "," + boat.hasBoxTime + ",";
    }

    /// <summary>
    /// Has a mutationChance ([0%, 100%]) of causing a mutationFactor [-mutationFactor, +mutationFactor] to each gene / weight.
    /// The chance of mutation is calculated per gene / weight.
    /// </summary>
    /// <param name="mutationFactor">How much a gene / weight can change (-mutationFactor, +mutationFactor)</param>
    /// <param name="mutationChance">Chance of a mutation happening per gene / weight.</param>
    public void Mutate(float mutationFactor, float mutationChance)
    {
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            steps += (int) Random.Range(-mutationFactor, +mutationFactor);
            steps = (int) Mathf.Max(steps, _minimalSteps);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            rayRadius += (int) Random.Range(-mutationFactor, +mutationFactor);
            rayRadius = (int) Mathf.Max(rayRadius, _minimalRayRadius);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            float sightIncrease = Random.Range(-mutationFactor, +mutationFactor);
            sight += sightIncrease;
            sight = Mathf.Max(sight, _minimalSight);
            if (sightIncrease > 0.0f)
            {
                movingSpeed -= sightIncrease * _sightInfluenceInSpeed;
                movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);    
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            float movingSpeedIncrease = Random.Range(-mutationFactor, +mutationFactor);
            movingSpeed += movingSpeedIncrease;
            movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);
            if (movingSpeedIncrease > 0.0f)
            {
                sight -= movingSpeedIncrease * _speedInfluenceInSight;
                sight = Mathf.Max(sight, _minimalSight);    
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.x += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.y += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boxWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            distanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatDistanceFactor +=  Random.Range(-mutationFactor, +mutationFactor);
        }
    }

    private void Update()
    {
        if (_isAwake)
        {
            Act();    
        }
    }

    /// <summary>
    /// Calculate the best direction to move using the Agent properties.
    /// The agent shoots a ray in a area on front of itself and calculates the utility of each one of them based on what
    /// it did intersect or using a random value (uses a Random from [randomDirectionValue.x, randomDirectionValue.y]).
    /// 
    /// </summary>
    private void Act()
    {
        Transform selfTransform = transform;
        Vector3 forward = selfTransform.forward;
        //Ignores the y component to avoid flying/sinking Agents.
        forward.y = 0.0f;
        forward.Normalize();
        Vector3 selfPosition = selfTransform.position;

        //Initiate the rayDirection on the opposite side of the spectrum.
        Vector3 rayDirection = Quaternion.Euler(0, -1.0f * steps * (rayRadius / 2.0f), 0) * forward;
        
        //List of AgentDirection (direction + utility) for all the directions.
        List<AgentDirection> directions = new List<AgentDirection>();
        for (int i = 0; i <= rayRadius; i++)
        {
            //Add the new calculatedAgentDirection looking at the rayDirection.
            directions.Add(CalculateAgentDirection(selfPosition, rayDirection));
            
            //Rotate the rayDirection by _steps every iteration through the entire rayRadius.
            rayDirection = Quaternion.Euler(0, steps, 0) * rayDirection;
        }
        //Adds an extra direction for the front view with a extra range.
        directions.Add(CalculateAgentDirection(selfPosition, forward, 1.5f));

        directions.Sort();
        //There is a (100 - _maxUtilityChoiceChance) chance of using the second best option instead of the highest one. Should help into ambiguous situation.
        AgentDirection highestAgentDirection = directions[Random.Range(0.0f, 100.0f) <= _maxUtilityChoiceChance ? 0 : 1];
        
        //Rotate towards to direction. The factor of 0.1 helps to create a "rotation" animation instead of automatically rotates towards the target. 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(highestAgentDirection.Direction), 0.1f);
        
        //Sets the velocity using the chosen direction
        _rigidbody.velocity = highestAgentDirection.Direction * movingSpeed;
        
        if (debug)
        {
            Debug.DrawRay(selfPosition, highestAgentDirection.Direction * (sight * 1.5f), directionColor);
        }
    }

    private AgentDirection CalculateAgentDirection(Vector3 selfPosition, Vector3 rayDirection, float sightFactor = 1.0f)
    {
        if (debug)
        {
            Debug.DrawRay(selfPosition, rayDirection * sight, visionColor);
        }

        //Calculate a random utility to initiate the AgentDirection.
        float utility = Random.Range(Mathf.Min(randomDirectionValue.x, randomDirectionValue.y), Mathf.Max(randomDirectionValue.x, randomDirectionValue.y));

        //Create an AgentDirection struct with a random utility value [utility]. Ignores y component.
        AgentDirection direction = new AgentDirection(new Vector3(rayDirection.x, 0.0f, rayDirection.z), utility);
        
        //Raycast into the rayDirection to check if something can be seen in that direction.
        //The sightFactor is a variable that increases / decreases the size of the ray.
        //For now, the sightFactor is only used to control the long sight in front of the agent.
        if (Physics.Raycast(selfPosition, rayDirection, out RaycastHit raycastHit, sight * sightFactor))
        {
            if (debug)
            {
                Debug.DrawLine(selfPosition, raycastHit.point, foundColor);
            }
            
            //Calculate the normalized distance from the agent to the intersected object.
            //Closer objects will have distancedNormalized close to 0, and further objects will have it close to 1.
            float distanceNormalized = (raycastHit.distance / (sight * sightFactor));
            
            //Inverts the distanceNormalized. Closer objects will tend to 1, while further objects will tend to 0.
            //Thus, closer objects will have a higher value.
            distanceIndex = 1.0f - distanceNormalized;

            //Calculate the utility of the found object according to its type.
            switch (raycastHit.collider.gameObject.tag)
            {
                //All formulas are the same. Only the weights change.
                case "Box":
                    utility = distanceIndex * distanceFactor * lifeTime + boxWeight * lifeTime;
                    break;
                case "LoveBox":
                    utility = (!canReproduce) ? distanceIndex * distanceFactor + boxWeight : -10;
                    break;
                case "Boat":
                    if (canReproduce && raycastHit.collider.gameObject.GetComponent<BoatLogic>().canReproduce)
                    {
                        utility = (hasBoxTime*hasBoxTime + 10) * distanceIndex * boatDistanceFactor + boatWeight * hasBoxTime * hasBoxTime;
                    }
                    else if (canReproduce)
                    {
                        utility = (hasBoxTime*hasBoxTime - 10) * (distanceIndex * boatDistanceFactor + boatWeight * hasBoxTime * hasBoxTime);
                    }
                    else
                    {
                        utility = 0;
                    }
                    
                    break;
            }
        }
        
        direction.Utility = utility;
        return direction;
    }

    /// <summary>
    /// Activates the agent update method.
    /// Does nothing if the agent is already awake.
    /// </summary>
    public void AwakeUp()
    {
        _isAwake = true;
    }

    /// <summary>
    /// Compares the points of two agents. When used on Sort function will make the highest points to be on top.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public int CompareTo(object obj) {
        if (obj == null) return 1;
        
        AgentLogic otherAgent = obj as AgentLogic;
        if (otherAgent != null)
        {

            return 1;

        } 
        else
        {
            throw new ArgumentException("Object is not an AgentLogic");
        }
    }

    /// <summary>
    /// Returns the AgentData of this Agent.
    /// </summary>
    /// <returns></returns>
    public AgentData GetData()
    {
        return new AgentData(steps, rayRadius, sight, movingSpeed, randomDirectionValue, boxWeight, distanceFactor, boatWeight, boatDistanceFactor);
    }

    private void OnDrawGizmos()
    {
        if (canReproduce)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.blue;
        }
        
        Gizmos.DrawWireSphere(transform.position, 5);
    }
}
