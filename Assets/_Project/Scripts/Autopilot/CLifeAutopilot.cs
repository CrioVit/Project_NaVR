using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;
using static BrakeAssist_BaseFeature;
using UnityEngine.UI;
using System;
using UnityEngine.InputSystem;

public class CLifeAutopilot : MonoBehaviour
{
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Scripts")] public Car car;

    [Header("Car Front (Transform)")]
    [FoldoutGroup("SETUP/Components")] public Transform carFront;

    [Header("General Parameters")]
    [FoldoutGroup("SETUP/Autopilot Parameters")] public List<string> NavMeshLayers;

    [Header("Debug")]
    [FoldoutGroup("DEBUG")] public bool ShowGizmos;
    [FoldoutGroup("DEBUG")] public bool Debugger;

    [Header("Destination Parameters")]
    [FoldoutGroup("DEBUG")] public bool Patrol = true;

    [Header("Custom Destination (Transform)")]
    [FoldoutGroup("SETUP/Components")] public Transform CustomDestination;

    [HideInInspector] public bool move;

    private Vector3 PostionToFollow = Vector3.zero;
    private int currentWayPoint;
    private float AIFOV = 60;
    private bool allowMovement;
    private int NavMeshLayerBite;
    private List<Vector3> waypoints = new List<Vector3>();
    private float LocalMaxSpeed;
    private float steeringAngle;
    [FoldoutGroup("SETUP/Autopilot Parameters")] public int speedLimit;
    private int Fails;

    //Detecting Obstacle
    [Header("Obstacle Detection")]
    [FoldoutGroup("DEBUG"), Range(0, 10)] public float securityDistanceForward;
    [FoldoutGroup("DEBUG"), Range(0, 50)] public float obstacleDetectionRange;
    [FoldoutGroup("DEBUG")] public float distanceToForwardObstacle;

    private TrafficLight trafficLight;
    private RaycastHit forwardHitInfo;
    private int trafficLightStopAreaLayer;
    private bool _detectingObstacleForward = false;
    private bool redLight = false;
    private bool yellowLight = false;
    private bool stopArea = false;

    //Line Renderer
    [Header("Path Line (Line Renderer)")]
    [FoldoutGroup("SETUP/Components")] public LineRenderer lineRenderer;

    [Header("Path Line")]
    [FoldoutGroup("DEBUG")] public bool showLineRenderer = true;

    //Support Variables
    private float smoothThrottle = 0f;

    // Enable/Disable autopilot
    [FoldoutGroup("SETUP/Input Action"), SerializeField] InputActionAsset inputActionAsset;
    InputAction autopilotActivationAction;
    private bool useAutopilot = false;
    private bool enableGizmos = true;

    void Awake()
    {
        currentWayPoint = 0;
        allowMovement = true;
        move = true;
    }

    void Start()
    {
        car = GetComponent<Car>();

        GetComponent<Rigidbody>().centerOfMass = Vector3.zero;
        CalculateNavMashLayerBite();

        if (lineRenderer != null && !lineRenderer.gameObject.activeSelf && showLineRenderer)
        {
            lineRenderer.gameObject.SetActive(true);
        }
    }

    void FixedUpdate()
    {
        if (car.isEngineOn && useAutopilot)
        {
            ApplySteering();
            PathProgress();

            UpdateLineRenderer();
        }
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
                if (Vector3.Distance(carFront.position, PostionToFollow) < 3)
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
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * 100;
            randomDirection += transform.position;
            sourcePostion = carFront.position;
            Calculate(randomDirection, sourcePostion, carFront.forward, NavMeshLayerBite);
        }
        else
        {
            sourcePostion = waypoints[waypoints.Count - 1];
            Vector3 randomPostion = UnityEngine.Random.insideUnitSphere * 100;
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


    // Modified by CLife
    void ApplySteering() // Applies steering to the Current waypoint
    {
        Vector3 relativeVector = transform.InverseTransformPoint(PostionToFollow);
        steeringAngle = (relativeVector.x / relativeVector.magnitude) * car.maxSteeringAngle;

        car.Steer(1, steeringAngle);
    }

    // Modified by CLife
    void Movement() // moves the car forward and backward depending on the input
    {
        if (move == true && allowMovement == true && redLight == false && car.currentGear >=1)
            allowMovement = true;
        else
            allowMovement = false;

        if (allowMovement == true)
        {
            smoothThrottle = Mathf.Lerp(smoothThrottle, 1, Time.deltaTime / 2);

            if (!stopArea)
            {
                if (steeringAngle > 15)
                    LocalMaxSpeed = 20f;
                else
                    LocalMaxSpeed = speedLimit;

                if (car.currentSpeed < LocalMaxSpeed)
                {
                    car.Brake(0f);
                    car.Throttle(smoothThrottle);
                }
                else
                {
                    smoothThrottle = 0f;
                    car.Throttle(0f);
                    car.Brake(1f);
                }

            }
            else if(stopArea)
            {
                if (yellowLight)
                    LocalMaxSpeed = 15f;
                else
                    LocalMaxSpeed = 20f;

                if (car.currentSpeed > LocalMaxSpeed)
                {
                    smoothThrottle = 0f;
                    car.Throttle(0f);
                    car.Brake(1f);
                }
                else
                {
                    car.Brake(0f);
                    car.Throttle(smoothThrottle);
                }
            }
        }
        else
        {
            if (car.currentSpeed > 0.1f)
            {
                smoothThrottle = 0f;
                car.Throttle(0f);
                car.Brake(1f);
            }
            else
            {
                smoothThrottle = 0f;
                car.Throttle(0f);
                car.Brake(0f);
            }
        }
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
        if (ShowGizmos == true && isActiveAndEnabled && enableGizmos == true && useAutopilot == true)
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
                Gizmos.DrawWireSphere(waypoints[i], 3f);
            }
            CalculateFOV();
            ObstacleDetection();
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

        void ObstacleDetection()
        {
            Debug.DrawRay(carFront.position, transform.forward * obstacleDetectionRange, _detectingObstacleForward ? Color.red : Color.green);
        }
    }


    // CLife Methods
    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("Trigger Enter");

        trafficLightStopAreaLayer = LayerMask.NameToLayer("Traffic Light Stop Area");

        if (collider.gameObject.layer == trafficLightStopAreaLayer)
        {
            stopArea = true;

            trafficLight = collider.GetComponentInParent<TrafficLight>();

            LayerMask trafficLightMask = LayerMask.GetMask("Traffic Light");
            _detectingObstacleForward = Physics.Raycast(carFront.position, carFront.forward, out forwardHitInfo, obstacleDetectionRange, trafficLightMask);

            switch (trafficLight.state)
            {
                case TrafficLight.State.Green:

                    yellowLight = false;
                    redLight = false;

                    break;

                case TrafficLight.State.Red:

                    yellowLight = false;

                    if (_detectingObstacleForward)
                    {
                        distanceToForwardObstacle = forwardHitInfo.distance;

                        if (distanceToForwardObstacle < securityDistanceForward)
                            redLight = true;
                    }

                    break;

                case TrafficLight.State.Yellow:

                    yellowLight = true;
                    redLight = false;

                    break;
            }
        }
    }

    private void OnTriggerStay(Collider collider)
    {
        Debug.Log("Trigger Stay");

        trafficLightStopAreaLayer = LayerMask.NameToLayer("Traffic Light Stop Area");

        if (collider.gameObject.layer == trafficLightStopAreaLayer)
        {
            stopArea = true;

            trafficLight = collider.GetComponentInParent<TrafficLight>();

            LayerMask trafficLightMask = LayerMask.GetMask("Traffic Light");
            _detectingObstacleForward = Physics.Raycast(carFront.position, carFront.forward, out forwardHitInfo, obstacleDetectionRange, trafficLightMask);

            switch (trafficLight.state)
            {
                case TrafficLight.State.Green:

                    yellowLight = false;
                    redLight = false;

                    break;

                case TrafficLight.State.Red:

                    yellowLight = false;

                    if (_detectingObstacleForward)
                    {
                        distanceToForwardObstacle = forwardHitInfo.distance;

                        if (distanceToForwardObstacle < securityDistanceForward)
                            redLight = true;
                    }

                    break;

                case TrafficLight.State.Yellow:

                    yellowLight = true;
                    redLight = false;

                    break;
            }
        }
    }

    private void OnTriggerExit(Collider collider)
    {
        Debug.Log("Trigger Exit");

        _detectingObstacleForward = false;
        stopArea = false;

        yellowLight = false;
        redLight = false;
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;

        if (!showLineRenderer)
        {
            if(lineRenderer.gameObject.activeSelf)
                lineRenderer.gameObject.SetActive(false);
        }
        else
        {
            if (!lineRenderer.gameObject.activeSelf)
                lineRenderer.gameObject.SetActive(true);
        }

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

    public void DisableAutopilot()
    {
        useAutopilot = false;

        move = false;
        allowMovement = false;

        smoothThrottle = 0f;
        car.Throttle(0f);
        car.Brake(0f);

        waypoints.Clear();  // Delete waypoints
        currentWayPoint = 0;

        if (lineRenderer != null)
            lineRenderer.gameObject.SetActive(false);

        enableGizmos = false;

        Debug.Log("Autopilota disattivato.");
    }

    public void EnableAutopilot()
    {
        useAutopilot = true;  // Enable the autopilot system

        move = true;  // Allow the vehicle to move
        allowMovement = true;  // Allow the movement logic to proceed

        smoothThrottle = 0f;  // Reset throttle to a safe state
        car.Throttle(0f);  // Ensure no throttle is applied initially
        car.Brake(0f);  // Ensure no braking is applied initially
        car.Steer(0f); // Ensure no steer is applied initially

        // Re-enable line renderer if it's supposed to be visible
        if (lineRenderer != null && showLineRenderer)
            lineRenderer.gameObject.SetActive(true);

        enableGizmos = true;

        // Clear the waypoints to start fresh or reuse the path as necessary
        waypoints.Clear();
        currentWayPoint = 0;

        Debug.Log("Autopilota riattivato.");
    }

    #region Input Action
    void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = inputActionAsset.FindActionMap("CLifeCarController");

        autopilotActivationAction = actionMap.FindAction("AutopilotActivation");

        // Register Callback Events
        autopilotActivationAction.performed += AutopilotActivation_action;

        // Enable Action
        autopilotActivationAction.Enable();
    }

    void OnDisable()
    {
        //Register Callback Events
        autopilotActivationAction.performed -= AutopilotActivation_action;

        // Enable Action
        autopilotActivationAction.Disable();
    }

    private void AutopilotActivation_action(InputAction.CallbackContext obj)
    {
        if (!useAutopilot)
            EnableAutopilot();
        else
            DisableAutopilot();
    }
    #endregion

}
