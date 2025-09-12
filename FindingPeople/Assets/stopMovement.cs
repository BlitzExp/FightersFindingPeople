using UnityEngine;

public class stopMovement : MonoBehaviour
{
    [SerializeField] private BotMovement botmov;
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
