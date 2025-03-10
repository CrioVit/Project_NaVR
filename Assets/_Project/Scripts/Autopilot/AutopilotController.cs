using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

public class AutopilotController : MonoBehaviour
{
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Scripts")] public Car car;
    
    [FoldoutGroup("SETUP/Components")]

    [Header("Car Wheels (Wheel Collider)")]
    [FoldoutGroup("SETUP/Components")] public WheelCollider frontLeft;
    [FoldoutGroup("SETUP/Components")] public WheelCollider frontRight;
    [FoldoutGroup("SETUP/Components")] public WheelCollider backLeft;
    [FoldoutGroup("SETUP/Components")] public WheelCollider backRight;

    [Header("Car Wheels (Transform)")]
    [FoldoutGroup("SETUP/Components")] public Transform wheelFL;
    [FoldoutGroup("SETUP/Components")] public Transform wheelFR;
    [FoldoutGroup("SETUP/Components")] public Transform wheelBL;
    [FoldoutGroup("SETUP/Components")] public Transform wheelBR;

    [Header("Car Front (Transform)")]
    [FoldoutGroup("SETUP/Components")] public Transform carFront;

    [Header("General Parameters")]// Look at the documentation for a detailed explanation 
    [FoldoutGroup("SETUP/Autopilot Parameters")] public List<string> NavMeshLayers;
    [FoldoutGroup("SETUP/Autopilot Parameters")] public int MaxSteeringAngle = 45;
    [FoldoutGroup("SETUP/Autopilot Parameters")] public int MaxRPM = 150;

    [Header("Debug")]
    [FoldoutGroup("DEBUG")] public bool ShowGizmos;
    [FoldoutGroup("DEBUG")] public bool Debugger;

    [Header("Destination Parameters")]// Look at the documentation for a detailed explanation
    [FoldoutGroup("DEBUG")] public bool Patrol = true;

    [Header("Custom Destination (Transform)")]
    [FoldoutGroup("SETUP/Components")] public Transform CustomDestination;

    [HideInInspector] public bool move;// Look at the documentation for a detailed explanation

    private Vector3 PostionToFollow = Vector3.zero;
    private int currentWayPoint;
    private float AIFOV = 60;
    private bool allowMovement;
    private int NavMeshLayerBite;
    private List<Vector3> waypoints = new List<Vector3>();
    private float LocalMaxSpeed;
    private int Fails;
    private float MovementTorque = 1;

    //Detecting Obstacle
    [Header("Obstacle Detection")]
    [FoldoutGroup("DEBUG"), Range(0, 10)] public float securityDistanceForward;
    [FoldoutGroup("DEBUG"), Range(0, 50)] public float obstacleDetectionRange;
    [FoldoutGroup("DEBUG")] public float distanceToForwardObstacle;

    private TrafficLight trafficLight;
    private RaycastHit forwardHitInfo;
    private int trafficLightLayer;
    private bool _detectingObstacleForward = false;
    private bool redLight = false;

    //Line Renderer
    [Header("Path Line (Line Renderer)")]
    [FoldoutGroup("SETUP/Components")] public LineRenderer lineRenderer;

    [Header("Path Line")]
    [FoldoutGroup("DEBUG")] public bool showLineRenderer = true;

    void Awake()
    {
        currentWayPoint = 0;
        allowMovement = true;
        move = true;
    }

    void Start()
    {
        GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
        CalculateNavMashLayerBite();

        car = GetComponent<Car>();

        if(lineRenderer != null && !lineRenderer.gameObject.activeSelf && showLineRenderer)
            lineRenderer.gameObject.SetActive(true);
    }

    void FixedUpdate()
    {
        UpdateWheels();
        ApplySteering();
        PathProgress();

        UpdateLineRenderer();
    }

    private void CalculateNavMashLayerBite()
    {
        if (NavMeshLayers == null || NavMeshLayers[0] == "AllAreas")
            NavMeshLayerBite = NavMesh.AllAreas;
        else if (NavMeshLayers.Count == 1)
            NavMeshLayerBite += 1 << NavMesh.GetAreaFromName(NavMeshLayers[0]);
        else
        {
            foreach (string Layer in NavMeshLayers)
            {
                int I = 1 << NavMesh.GetAreaFromName(Layer);
                NavMeshLayerBite += I;
            }
        }
    }

    private void PathProgress() //Checks if the agent has reached the currentWayPoint or not. If yes, it will assign the next waypoint as the currentWayPoint depending on the input
    {
        wayPointManager();
        Movement();
        ListOptimizer();

        void wayPointManager()
        {
            if (currentWayPoint >= waypoints.Count)
                allowMovement = false;
            else
            {
                PostionToFollow = waypoints[currentWayPoint];
                allowMovement = true;
                if (Vector3.Distance(carFront.position, PostionToFollow) < 2)
                    currentWayPoint++;
            }

            if (currentWayPoint >= waypoints.Count - 3)
                CreatePath();
        }

        void CreatePath()
        {
            if (CustomDestination == null)
            {
                if (Patrol == true)
                    RandomPath();
                else
                {
                    debug("No custom destination assigned and Patrol is set to false", false);
                    allowMovement = false;
                }
            }
            else
                CustomPath(CustomDestination);

        }

        void ListOptimizer()
        {
            if (currentWayPoint > 1 && waypoints.Count > 30)
            {
                waypoints.RemoveAt(0);
                currentWayPoint--;
            }
        }
    }

    public void RandomPath() // Creates a path to a random destination
    {
        NavMeshPath path = new NavMeshPath();
        Vector3 sourcePostion;

        if (waypoints.Count == 0)
        {
            Vector3 randomDirection = Random.insideUnitSphere * 100;
            randomDirection += transform.position;
            sourcePostion = carFront.position;
            Calculate(randomDirection, sourcePostion, carFront.forward, NavMeshLayerBite);
        }
        else
        {
            sourcePostion = waypoints[waypoints.Count - 1];
            Vector3 randomPostion = Random.insideUnitSphere * 100;
            randomPostion += sourcePostion;
            Vector3 direction = (waypoints[waypoints.Count - 1] - waypoints[waypoints.Count - 2]).normalized;
            Calculate(randomPostion, sourcePostion, direction, NavMeshLayerBite);
        }

        void Calculate(Vector3 destination, Vector3 sourcePostion, Vector3 direction, int NavMeshAreaByte)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 150, 1 << NavMesh.GetAreaFromName(NavMeshLayers[0])) &&
                NavMesh.CalculatePath(sourcePostion, hit.position, NavMeshAreaByte, path) && path.corners.Length > 2)
            {
                if (CheckForAngle(path.corners[1], sourcePostion, direction))
                {
                    waypoints.AddRange(path.corners.ToList());
                    debug("Random Path generated successfully", false);
                }
                else
                {
                    if (CheckForAngle(path.corners[2], sourcePostion, direction))
                    {
                        waypoints.AddRange(path.corners.ToList());
                        debug("Random Path generated successfully", false);
                    }
                    else
                    {
                        debug("Failed to generate a random path. Waypoints are outside the AIFOV. Generating a new one", false);
                        Fails++;
                    }
                }
            }
            else
            {
                debug("Failed to generate a random path. Invalid Path. Generating a new one", false);
                Fails++;
            }
        }
    }

    public void CustomPath(Transform destination) //Creates a path to the Custom destination
    {
        NavMeshPath path = new NavMeshPath();
        Vector3 sourcePostion;

        if (waypoints.Count == 0)
        {
            sourcePostion = carFront.position;
            Calculate(destination.position, sourcePostion, carFront.forward, NavMeshLayerBite);
        }
        else
        {
            sourcePostion = waypoints[waypoints.Count - 1];
            Vector3 direction = (waypoints[waypoints.Count - 1] - waypoints[waypoints.Count - 2]).normalized;
            Calculate(destination.position, sourcePostion, direction, NavMeshLayerBite);
        }

        void Calculate(Vector3 destination, Vector3 sourcePostion, Vector3 direction, int NavMeshAreaBite)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 150, NavMeshAreaBite) &&
                NavMesh.CalculatePath(sourcePostion, hit.position, NavMeshAreaBite, path))
            {
                if (path.corners.ToList().Count() > 1 && CheckForAngle(path.corners[1], sourcePostion, direction))
                {
                    waypoints.AddRange(path.corners.ToList());
                    debug("Custom Path generated successfully", false);
                }
                else
                {
                    if (path.corners.Length > 2 && CheckForAngle(path.corners[2], sourcePostion, direction))
                    {
                        waypoints.AddRange(path.corners.ToList());
                        debug("Custom Path generated successfully", false);
                    }
                    else
                    {
                        debug("Failed to generate a Custom path. Waypoints are outside the AIFOV. Generating a new one", false);
                        Fails++;
                    }
                }
            }
            else
            {
                debug("Failed to generate a Custom path. Invalid Path. Generating a new one", false);
                Fails++;
            }
        }
    }

    private bool CheckForAngle(Vector3 pos, Vector3 source, Vector3 direction) //calculates the angle between the car and the waypoint 
    {
        Vector3 distance = (pos - source).normalized;
        float CosAngle = Vector3.Dot(distance, direction);
        float Angle = Mathf.Acos(CosAngle) * Mathf.Rad2Deg;

        if (Angle < AIFOV)
            return true;
        else
            return false;
    }

    private void ApplyBrakes() // Apply brake torque 
    {
        frontLeft.brakeTorque = 5000;
        frontRight.brakeTorque = 5000;
        backLeft.brakeTorque = 5000;
        backRight.brakeTorque = 5000;
    }

    private void UpdateWheels() // Updates the wheel's postion and rotation
    {
        ApplyRotationAndPostion(frontLeft, wheelFL);
        ApplyRotationAndPostion(frontRight, wheelFR);
        ApplyRotationAndPostion(backLeft, wheelBL);
        ApplyRotationAndPostion(backRight, wheelBR);
    }

    private void ApplyRotationAndPostion(WheelCollider targetWheel, Transform wheel) // Updates the wheel's postion and rotation
    {
        targetWheel.ConfigureVehicleSubsteps(5, 12, 15);

        Vector3 pos;
        Quaternion rot;
        targetWheel.GetWorldPose(out pos, out rot);
        wheel.position = pos;
        wheel.rotation = rot;
    }

    void ApplySteering() // Applies steering to the Current waypoint
    {
        Vector3 relativeVector = transform.InverseTransformPoint(PostionToFollow);
        float SteeringAngle = (relativeVector.x / relativeVector.magnitude) * MaxSteeringAngle;
        if (SteeringAngle > 15) LocalMaxSpeed = 100;
        else LocalMaxSpeed = MaxRPM;

        frontLeft.steerAngle = SteeringAngle;
        frontRight.steerAngle = SteeringAngle;
    }

    void Movement() // moves the car forward and backward depending on the input
    {
        if (move == true && allowMovement == true && redLight == false)
            allowMovement = true;
        else
            allowMovement = false;

        if (allowMovement == true)
        {
            frontLeft.brakeTorque = 0;
            frontRight.brakeTorque = 0;
            backLeft.brakeTorque = 0;
            backRight.brakeTorque = 0;

            int SpeedOfWheels = (int)((frontLeft.rpm + frontRight.rpm + backLeft.rpm + backRight.rpm) / 4);

            if (SpeedOfWheels < LocalMaxSpeed)
            {
                backRight.motorTorque = 400 * MovementTorque;
                backLeft.motorTorque = 400 * MovementTorque;
                frontRight.motorTorque = 400 * MovementTorque;
                frontLeft.motorTorque = 400 * MovementTorque;
            }
            else if (SpeedOfWheels < LocalMaxSpeed + (LocalMaxSpeed * 1 / 4))
            {
                backRight.motorTorque = 0;
                backLeft.motorTorque = 0;
                frontRight.motorTorque = 0;
                frontLeft.motorTorque = 0;
            }
            else
                ApplyBrakes();
        }
        else
            ApplyBrakes();
    }

    void debug(string text, bool IsCritical)
    {
        if (Debugger)
        {
            if (IsCritical)
                Debug.LogError(text);
            else
                Debug.Log(text);
        }
    }

    private void OnDrawGizmos() // shows a Gizmos representing the waypoints and AI FOV
    {
        if (ShowGizmos == true)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (i == currentWayPoint)
                    Gizmos.color = Color.blue;
                else
                {
                    if (i > currentWayPoint)
                        Gizmos.color = Color.red;
                    else
                        Gizmos.color = Color.green;
                }
                Gizmos.DrawWireSphere(waypoints[i], 2f);
            }
            CalculateFOV();
        }

        void CalculateFOV()
        {
            Gizmos.color = Color.white;
            float totalFOV = AIFOV * 2;
            float rayRange = 10.0f;
            float halfFOV = totalFOV / 2.0f;
            Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
            Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
            Vector3 leftRayDirection = leftRayRotation * transform.forward;
            Vector3 rightRayDirection = rightRayRotation * transform.forward;
            Gizmos.DrawRay(carFront.position, leftRayDirection * rayRange);
            Gizmos.DrawRay(carFront.position, rightRayDirection * rayRange);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("Trigger Enter");

        trafficLightLayer = LayerMask.NameToLayer("Traffic Light");

        if (collider.gameObject.layer == trafficLightLayer)
        {
            trafficLight = collider.GetComponentInParent<TrafficLight>();

            switch (trafficLight.state)
            {
                case TrafficLight.State.Green:
                    redLight = false;
                    break;

                case TrafficLight.State.Red:

                    LayerMask trafficLightMask = LayerMask.GetMask("Traffic Light");
                    _detectingObstacleForward = Physics.Raycast(carFront.position, transform.forward, out forwardHitInfo, obstacleDetectionRange, trafficLightMask);
                    if (_detectingObstacleForward)
                    {
                        distanceToForwardObstacle = forwardHitInfo.distance;

                        if (distanceToForwardObstacle < securityDistanceForward)
                            redLight = true;
                    }

                    break;

                case TrafficLight.State.Yellow:
                    redLight = false;
                    break;
            }
        }
    }

    private void OnTriggerStay(Collider collider)
    {
        Debug.Log("Trigger Stay");

        trafficLightLayer = LayerMask.NameToLayer("Traffic Light");

        if (collider.gameObject.layer == trafficLightLayer)
        {
            trafficLight = collider.GetComponentInParent<TrafficLight>();

            switch (trafficLight.state)
            {
                case TrafficLight.State.Green:
                    redLight = false;
                    break;

                case TrafficLight.State.Red:

                    LayerMask trafficLightMask = LayerMask.GetMask("Traffic Light");
                    _detectingObstacleForward = Physics.Raycast(carFront.position, transform.forward, out forwardHitInfo, obstacleDetectionRange, trafficLightMask);
                    if (_detectingObstacleForward)
                    {
                        distanceToForwardObstacle = forwardHitInfo.distance;

                        if (distanceToForwardObstacle < securityDistanceForward)
                            redLight = true;
                    }

                    break;

                case TrafficLight.State.Yellow:
                    redLight = false;
                    break;
            }
        }
    } 

    private void OnTriggerExit(Collider collider)
    {
        Debug.Log("Trigger Exit");
        
        redLight = false;
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null || !showLineRenderer) return;

        List<Vector3> linePositions = new List<Vector3>();

        // Aggiunge la posizione attuale dell'auto come primo punto
        linePositions.Add(carFront.position);

        // Aggiunge solo i waypoint ancora da raggiungere
        for (int i = currentWayPoint; i < waypoints.Count; i++)
        {
            linePositions.Add(waypoints[i]);
        }

        // Imposta i punti aggiornati nel Line Renderer
        lineRenderer.positionCount = linePositions.Count;
        lineRenderer.SetPositions(linePositions.ToArray());
    }

}
