using UnityEngine;


// Function for identificate thata the drone is within 2 m of the person
public class stopMovement : MonoBehaviour
{
    [SerializeField] private BotMovement botmov;

    // Checks if the drone is within 3 meters of the person and stops its movement
    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.CompareTag("Dron"))
        {
            Vector3 position = collision.transform.position;
            float distance = Vector3.Distance(transform.position, position);

            if (distance <= 3f)
            {
                botmov.stopMovement();
                this.enabled = false;
            }
        }
    }
}
