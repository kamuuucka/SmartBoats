using System;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class BoatLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 2.0f;
    private static float _piratePoints = -100.0f;
    #endregion

    private string parentsData;
    //public float lifeTime = 50.0f;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Equals("LoveBox"))
        {
            if (!canReproduce)
            {
                canReproduce = true;
                hasBoxTime = 0;
                Destroy(other.gameObject);
            }
            
        }
        if(other.gameObject.tag.Equals("Box"))
        {
            //points += _boxPoints;
            Debug.LogError($"{name} FEEDING TIME");
            lifeTime -= 10.0f;
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
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag.Equals("Enemy"))
        {
            //This is a safe-fail mechanism. In case something goes wrong and the Boat is not destroyed after touching
            //a pirate, it also gets a massive negative number of points.
            points += _piratePoints;
        }

        if (other.gameObject.tag.Equals("Boat"))
        {
            var boatLogic = other.gameObject.GetComponent<BoatLogic>();
            if (boatLogic != null && canReproduce && boatLogic.canReproduce)
            {
                canReproduce = false;
                hasBoxTime = 0;
                boatLogic.canReproduce = false;
                GenerationManager.Instance.GenerateChild(this, boatLogic);
                parentsData = GetData(this) + GetData(boatLogic) + GenerationManager.Instance.GetDataChild();
                SaveToFile(parentsData);
            }
        }
    }

    private void SaveToFile(string data)
    {
        using (TextWriter writer = new StreamWriter("Assets/results.txt", true))
        {

            writer.WriteLine(data);
            writer.Close();
        }

        string line = "";
        using StreamReader sr = new StreamReader("Assets/results.txt");
        while ((line = sr.ReadLine()) != null)
        {
            Console.WriteLine(line);
        }
    }
}
