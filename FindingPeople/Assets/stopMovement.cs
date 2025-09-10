using UnityEngine;

public class stopMovement : MonoBehaviour
{
    [SerializeField] private BotMovement botmov;
    private void OnTriggerEnter(Collider collision) 
    {
        if (collision.gameObject.CompareTag("Dron"))
        {
            botmov.stopMovement();
        }
    }
}
