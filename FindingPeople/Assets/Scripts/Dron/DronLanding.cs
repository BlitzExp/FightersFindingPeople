using UnityEngine;

// Fucntion in charge of detecting when the drone has landed using a raycast
public class DronLanding : MonoBehaviour
{
    [SerializeField] DronManager dronManager;

    // Update is called once per frame
    void Update()
    {
        // Creates a raycast bellow to check the distance to the ground
        RaycastHit hit;
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        float maxDistance = 1.5f;

        Color rayColor = Color.red;

        if (Physics.Raycast(origin, direction, out hit, maxDistance))
        {
            rayColor = Color.green;

            // When the raycast hits the ground, it notifies the DronManager to start the landing
            if (hit.collider.CompareTag("terrain"))
            {
                Debug.Log("Dron ha aterrizado a 1.5 metros de altura.");
                dronManager.stopLanding();
            }
            Debug.DrawLine(origin, hit.point, rayColor);
        }
        else
        {
            Debug.DrawRay(origin, direction * maxDistance, rayColor);
        }
    }
}
