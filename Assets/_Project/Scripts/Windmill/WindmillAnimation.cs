using UnityEngine;

public class WindmillAnimation : MonoBehaviour
{
    [Range(0,150)] public float rotationSpeed;

    private void Start()
    {
        rotationSpeed = Random.Range(0, 100);
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }
}
