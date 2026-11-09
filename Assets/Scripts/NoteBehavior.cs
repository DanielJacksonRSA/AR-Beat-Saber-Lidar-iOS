using UnityEngine;

public class NoteBehavior : MonoBehaviour
{
    // Hidden because the Spawner completely controls this number now!
    [HideInInspector]
    public float speed;

    void Start()
    {
        Destroy(gameObject, 8f);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
