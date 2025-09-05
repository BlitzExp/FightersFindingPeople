    using UnityEngine;

    public class DronHeight : MonoBehaviour
    {
        public float targetHeight = 120f; 
        public float speed = 0;
        public float acceleration = 10f;  
        public float maxSpeed = 30f;   
        public bool reachedHeight = false;
        private bool isActive = false;

        void Update()
        {
            if (!isActive || reachedHeight) return;

            speed = Mathf.Min(speed + acceleration * Time.deltaTime, maxSpeed);

            Vector3 targetPosition = new Vector3(transform.position.x, targetHeight, transform.position.z);

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Mathf.Abs(transform.position.y - targetHeight) < 0.01f)
            {
                reachedHeight = true;
                speed = 0f;
            }
        }

    // Método público para iniciar la elevación
        public void StartElevation()
        {
            isActive = true;
            reachedHeight = false;
            speed = 5f;
        }
    }
