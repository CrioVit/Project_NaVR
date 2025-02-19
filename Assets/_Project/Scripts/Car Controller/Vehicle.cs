using Sirenix.OdinInspector;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    #region Static Variables

    [FoldoutGroup("Components")] public Transform cachedTransform;
    [FoldoutGroup("Components")] public Transform start; // start point
    [FoldoutGroup("Components")] public Transform destination; // final destination
    [FoldoutGroup("Components")] public Rigidbody vehicleRigidBody;
    [FoldoutGroup("Components")] public Collider vehicleCollider;

    #endregion

    #region Obstacle Detection

    [FoldoutGroup("Obstacle Detection")]

    // fwd
    [FoldoutGroup("Obstacle Detection/Forward")] public Transform forwardRayStartingPoint;
    [FoldoutGroup("Obstacle Detection/Forward")] public RaycastHit forwardHitInfo;
    [FoldoutGroup("Obstacle Detection/Forward"), Range(0, 10)] public float securityDistanceForward;
    [FoldoutGroup("Obstacle Detection/Forward")] public float distanceToForwardObstacle;
    [FoldoutGroup("Obstacle Detection/Forward")] public bool _detectingObstacleForward = false;
    [FoldoutGroup("Obstacle Detection/Forward")] public Vector3 rayDirectionForward => cachedTransform.forward;

    [FoldoutGroup("Obstacle Detection/Forward")] public Vehicle vehicleInFront;

    #region Trafficlight

    [FoldoutGroup("Obstacle Detection/Forward/Trafficlight")] TrafficLight trafficLight;

    #endregion

    // bwd
    [FoldoutGroup("Obstacle Detection/Backward")] public Transform backwardRayStartingPoint;
    [FoldoutGroup("Obstacle Detection/Backward")] public RaycastHit backwardHitInfo;
    [FoldoutGroup("Obstacle Detection/Backward"), Range(0, 10)] public float securityDistanceBackward;
    [FoldoutGroup("Obstacle Detection/Backward")] public float distanceToBackwardObstacle;
    [FoldoutGroup("Obstacle Detection/Backward")] public bool _detectingObstacleBackward = false;
    [FoldoutGroup("Obstacle Detection/Backward")] public Vector3 rayDirectionBackward => -cachedTransform.forward;

    #endregion

    #region Setting Parameters

    [FoldoutGroup("Movement Parameters")][Range(0, 200)] public float nominalVelocity=0; // in km/h
    [FoldoutGroup("Movement Parameters")][Range(0, 200)] public float maxVelocity=0; // in km/h
    [FoldoutGroup("Movement Parameters")][Range(0, 1)] public float brakingForce=0; // brake efficiency
    [FoldoutGroup("Movement Parameters")][Range(0, 1)] public float vechicleAcceleration=0; // acceleration efficiency

    [FoldoutGroup("Debug"), ReadOnly] public State state;
    [FoldoutGroup("Debug"), ReadOnly] public Vector3 targetPosition; //next target position
    [FoldoutGroup("Debug"), ReadOnly] public Vector3 nextPosition; //next movement towards the target position
    [FoldoutGroup("Debug"), ReadOnly] public float velocity=0; // in km/h
    [FoldoutGroup("Debug"), ReadOnly] public float velocityChange = 0;
    [FoldoutGroup("Debug"), ReadOnly] public float brakeAmount=0; // brake amount from 0 to 1
    [FoldoutGroup("Debug"), ReadOnly] public float throttleAmount=0; // Throttle amount from 0 to 1
    [FoldoutGroup("Debug"), ReadOnly] public float newVelocity = 0; // new velocity when the car brake

    [FoldoutGroup("Obstacle Detection Parameters"), Range(0, 50)] public float obstacleDetectionRange; // simulate the eye of the driver, how long it can see
    [FoldoutGroup("Obstacle Detection Parameters")][Range(0.3f, 5)] public float reactionTime; //reaction time of the driver, typically it goes from 0.3 to 4

    #endregion

    // Velocity = Distance/Time (m/s - Km/h)
    // Acceleration = Velocity/Time (m/s^2 - km/h^2)
    // Force = Acceleration * Mass (Kg * m/s^2)

    // FinalVelocity = Force/m * Time
    

    // Actual state of the vehicle
    public enum State
    {
        Idle,
        Moving,
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cachedTransform = transform;

        vehicleRigidBody = GetComponent<Rigidbody>();
        vehicleCollider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        EvaluatePreConditions();
        CalculateNewState();
        UpdateState();
        EvaluatePostConditions();
    }

    public void OnDrawGizmos()
    {
        // color the start/destination point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(start.position, 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destination.position, 0.1f);

        if (cachedTransform)
        {
            Debug.DrawRay(forwardRayStartingPoint.position, forwardRayStartingPoint.forward * obstacleDetectionRange, _detectingObstacleForward ? Color.red : Color.green);
            Debug.DrawRay(backwardRayStartingPoint.position, backwardRayStartingPoint.forward * obstacleDetectionRange, _detectingObstacleBackward ? Color.red : Color.green);   
        }
    }

    public bool EvaluatePreConditions()
    {
        return true;
    }
    
    public void CalculateNewState()
    {
        _detectingObstacleForward = Physics.Raycast(forwardRayStartingPoint.position, rayDirectionForward, out forwardHitInfo, obstacleDetectionRange);
        _detectingObstacleBackward = Physics.Raycast(backwardRayStartingPoint.position, rayDirectionBackward, out backwardHitInfo, obstacleDetectionRange);

        if (_detectingObstacleForward)
        {
            vehicleInFront = forwardHitInfo.collider.GetComponent<Vehicle>();
            if (!vehicleInFront)
                trafficLight = forwardHitInfo.collider.GetComponentInParent<TrafficLight>();
        }

        switch (state)
        {
            case State.Idle:

                velocity = 0f;
                velocityChange = 0f;

                break;

            case State.Moving:

                // cerchiamo di raggiungere la nominal velocity di base a monte di qualsiasi controllo
                if (velocity < nominalVelocity)
                    throttleAmount = (nominalVelocity - velocity) / maxVelocity;
                if (velocity > nominalVelocity)
                    brakeAmount = (velocity - nominalVelocity) / maxVelocity;

                if (_detectingObstacleForward)
                {
                    distanceToForwardObstacle = forwardHitInfo.distance;

                    if (vehicleInFront)
                    {
                        if (distanceToForwardObstacle < securityDistanceForward)
                        {
                            brakeAmount = Mathf.Min(1 / distanceToForwardObstacle, 1f);
                            throttleAmount = 0f;
                        }
                    }
                    else
                    {
                        switch (trafficLight.state)
                        {
                            case TrafficLight.State.Green:
                                break;
                                 
                            case TrafficLight.State.Red:
                                
                                newVelocity = Mathf.Lerp(velocity, 0f, 1-((distanceToForwardObstacle-0.1f)/securityDistanceForward));
                                brakeAmount = Mathf.Min(((velocity - newVelocity) / brakingForce), 1);
                                throttleAmount = 0f;

                                break;

                            case TrafficLight.State.Yellow:


                                break;
                        }
                    }
                }

                if (_detectingObstacleBackward)
                {

                }

                break;
        }        
    }

    public void UpdateState()
    {
        targetPosition = destination.position; // in futuro aggiungeremo una funzione che calcola il prossimo passo verso la destinazione
        velocityChange = -(brakingForce * brakeAmount) + (vechicleAcceleration * throttleAmount);
        velocity = velocity + velocityChange;
        nextPosition = cachedTransform.position + (targetPosition - cachedTransform.position).normalized * (velocity * 0.001f);
        cachedTransform.position = nextPosition;
    }

    public void EvaluatePostConditions()
    {

    }
}
