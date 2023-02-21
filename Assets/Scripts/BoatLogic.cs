using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class BoatLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 2.0f;
    private static float _piratePoints = -100.0f;
    #endregion
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Equals("LoveBox"))
        {
            canReproduce = true;
            Destroy(other.gameObject);
        }
        if(other.gameObject.tag.Equals("Box"))
        {
            points += _boxPoints;
            Destroy(other.gameObject);
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
                boatLogic.canReproduce = false;
                //TODO: Instantiate new boat with genes from both parents
                GenerationManager.Instance.GenerateChild(this, boatLogic);
            }
        }
    }
}
