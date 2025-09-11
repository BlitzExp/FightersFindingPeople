using UnityEngine;

public class DronLanding : MonoBehaviour
{
    [SerializeField] DronManager dronManager;

    // Update is called once per frame
    void Update()
    {
        // Crea un raycast hacia abajo desde la posición del dron
        RaycastHit hit;
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        float maxDistance = 1.5f;

        // Dibujar el raycast en la vista de escena (color rojo si no choca, verde si sí)
        Color rayColor = Color.red;

        if (Physics.Raycast(origin, direction, out hit, maxDistance))
        {
            rayColor = Color.green;

            // Si el raycast golpea el terreno
            if (hit.collider.CompareTag("terrain"))
            {
                Debug.Log("Dron ha aterrizado a 1.5 metros de altura.");
                dronManager.stopLanding();
            }

            // Dibuja línea hasta el punto de impacto
            Debug.DrawLine(origin, hit.point, rayColor);
        }
        else
        {
            // Si no golpea nada, dibuja el rayo completo en rojo
            Debug.DrawRay(origin, direction * maxDistance, rayColor);
        }
    }
}
