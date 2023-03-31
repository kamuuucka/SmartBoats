using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class BoatLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 2.0f;
    private static float _piratePoints = -100.0f;
    #endregion

    public int numberOfFood;
    public int numberOfLoveBoxes;
    public int numberOfKids;
    private string parentsData;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Equals("LoveBox"))
        {
            if (!canReproduce)
            {
                canReproduce = true;
                hasBoxTime = 0;
                numberOfLoveBoxes++;
                Destroy(other.gameObject);
            }
            
        }
        if(other.gameObject.tag.Equals("Box"))
        {
            lifeTime -= 10.0f;
            numberOfFood++;
            Destroy(other.gameObject);
        }
        
    }

    private void FixedUpdate()
    {
        if (canReproduce)
        {
            hasBoxTime += Time.fixedTime/100;
        }

        lifeTime += Time.deltaTime;

        if (lifeTime > 40)
        {
            GenerateDeathCertificate();
            Destroy(gameObject);
        }
    }

    public void GenerateDeathCertificate()
    {
        GenerationManager.Instance.AddToDeaths(this, name, numberOfFood, numberOfLoveBoxes, numberOfKids, lifeTime, LocalIndex);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag.Equals("Boat"))
        {
            var boatLogic = other.gameObject.GetComponent<BoatLogic>();
            if (boatLogic != null && canReproduce && boatLogic.canReproduce)
            {
                canReproduce = false;
                hasBoxTime = 0;
                boatLogic.canReproduce = false;
                numberOfKids++;
                boatLogic.numberOfKids++;
                GenerationManager.Instance.GenerateChild(this, boatLogic);
                parentsData = GetData(this) + GetData(boatLogic) + GenerationManager.Instance.GetDataChild();
            }
        }
    }

    
}
