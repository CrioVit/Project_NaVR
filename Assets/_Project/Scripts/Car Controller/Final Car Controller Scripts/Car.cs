using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Car : MonoBehaviour
{
    #region Enum
    public enum InputType
    {
        Keyboard,
        SteeringWheel
    }

    public enum DriveType
    {
        FWD, // Front Wheel Drive
        RWD, // Rear Wheel Drive
        AWD  // All Wheel Drive
    }

    public enum GearType
    {
        Automatic = 0,
        SemiAutomatic = 1
    }

    public enum AutomaticGear
    {
        Reverse = -1,
        Neutral = 0,
        Drive = 1
    }

    #endregion

    #region Components
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Scripts")] public CLifeLightsController lightsController;
    //[FoldoutGroup("SETUP/Scripts")] public SoundController soundController;

    [FoldoutGroup("SETUP/Components")] public Rigidbody rb;
    [FoldoutGroup("SETUP/Components/Wheels")] public WheelCollider frontLeftWheelCollider;
    [FoldoutGroup("SETUP/Components/Wheels")] public WheelCollider frontRightWheelCollider;
    [FoldoutGroup("SETUP/Components/Wheels")] public WheelCollider rearLeftWheelCollider;
    [FoldoutGroup("SETUP/Components/Wheels")] public WheelCollider rearRightWheelCollider;

    [FoldoutGroup("SETUP/Components/Wheels")] public Transform frontLeftWheelMesh;
    [FoldoutGroup("SETUP/Components/Wheels")] public Transform frontRightWheelMesh;
    [FoldoutGroup("SETUP/Components/Wheels")] public Transform rearLeftWheelMesh;
    [FoldoutGroup("SETUP/Components/Wheels")] public Transform rearRightWheelMesh;
    #endregion

    #region Friction
    private WheelFrictionCurve FLWSidewaysFriction;
    private WheelFrictionCurve FRWSidewaysFriction;
    private WheelFrictionCurve RLWSidewaysFriction;
    private WheelFrictionCurve RRWSidewaysFriction;

    private WheelFrictionCurve FLWForwardFriction;
    private WheelFrictionCurve FRWForwardFriction;
    private WheelFrictionCurve RLWForwardFriction;
    private WheelFrictionCurve RRWForwardFriction;
    #endregion

    #region Vehicles Parameters
    [FoldoutGroup("DEBUG")] public bool isStarted = false;
    [FoldoutGroup("DEBUG")] public bool isEngineOn = false;

    [FoldoutGroup("DEBUG/Wheel RPM")] public float frontLeftWheelRPM = 0f;
    [FoldoutGroup("DEBUG/Wheel RPM")] public float frontRightWheelRPM = 0f;
    [FoldoutGroup("DEBUG/Wheel RPM")] public float rearLeftWheelRPM = 0f;
    [FoldoutGroup("DEBUG/Wheel RPM")] public float rearRightWheelRPM = 0f;

    //Movement
    [FoldoutGroup("SETUP/Vehicles Parameters")]
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float maxForwardSpeed = 100f;
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement"), HideInInspector] public float storedMaxForwardSpeed;
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float maxReverseSpeed = 15f;
    [FoldoutGroup("DEBUG")] public float throttleAmount = 0f;
    [FoldoutGroup("DEBUG")] public float currentSpeed = 0f;

    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float decelerationSpeed = 0.5f;
    [FoldoutGroup("DEBUG")] public float torque = 0f;

    //Brake
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float brakeForce = 350f;
    [FoldoutGroup("DEBUG")] public float brakeAmount = 0f;
    [FoldoutGroup("DEBUG")] public float handbrakeAmount = 0f;

    //Steering
    [FoldoutGroup("SETUP/Vehicles Parameters/Steering Wheel"), Range(0, 1)] public float steeringSpeed = 0.5f;
    [FoldoutGroup("SETUP/Vehicles Parameters/Steering Wheel")] public float maxSteeringAngle = 25f;
    [FoldoutGroup("SETUP/Vehicles Parameters/Steering Wheel")] public float maxSteeringWheelRotation = 900/2;
    [FoldoutGroup("DEBUG")] public float steeringAmount = 0f;
    [FoldoutGroup("DEBUG")] public float currentSteerAngle = 0f;
    [FoldoutGroup("DEBUG")] public float steeringAngle = 0f;


    #region Engine
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float horsePower = 100;
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float maxRPM = 6000;
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public float idleRPM = 800;
    [FoldoutGroup("DEBUG")] public float currentRPM = 0;
    #endregion

    #region Gear
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public float[] gearRatios = { 0f, 3.80f, 2.40f, 1.80f, 1.40f, 1.10f, 0.90f }; // Neutral + 6 gears ratio
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public float reverseGearRatio = 4.20f; // Reverse gear ratio
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public float differentialRatio = 3.50f;
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public int maxGear = 6;
    //private float gearDeceleration = 0f; // reduces speed as RPM increases to simulate the effect of gear too low

    [FoldoutGroup("DEBUG")] public int currentGear = 0;

    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public GearType gearType;
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public AutomaticGear automaticGear;

    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public float automaticGearChangeMaxRPM = 2400f;
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public float automaticGearChangeMinRPM = 1500f;
    #endregion

    #region Input Type
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public InputType inputType;
    #endregion

    #region Drive Type
    [FoldoutGroup("SETUP/Vehicles Parameters/Gear")] public DriveType driveType;
    #endregion

    #endregion

    #region Animation Curves
    [FoldoutGroup("SETUP/Vehicles Parameters/Movement")] public AnimationCurve hpToRPMCurve;
    #endregion

    #region Input Action
    [FoldoutGroup("SETUP/Components/Input Action")] public InputActionAsset inputActionAsset;
    [HideInInspector] public InputAction startAndStopVehicleAction;
    [HideInInspector] public InputAction startAndStopEngineAction;
    [HideInInspector] public InputAction throttleAction;
    [HideInInspector] public InputAction brakeAction;
    [HideInInspector] public InputAction handbrakeAction;
    [HideInInspector] public InputAction steerAction;
    [HideInInspector] public InputAction upShiftAction;
    [HideInInspector] public InputAction downShiftAction;
    [HideInInspector] public InputAction gearTypeAction;
    [HideInInspector] public InputAction driveTypeAction;
    [HideInInspector] public InputAction changeCameraAction;
    [HideInInspector] public InputAction leftBlinkerAction;
    [HideInInspector] public InputAction rightBlinkerAction;
    #endregion
    
    #region Canvas
    [FoldoutGroup("SETUP/Components/Canvas")] public GameObject canvas;
    [FoldoutGroup("SETUP/Components/Canvas")] public TMP_Text currentGearTMP;
    [FoldoutGroup("SETUP/Components/Canvas")] public TMP_Text currentSpeedTMP;
    [FoldoutGroup("SETUP/Components/Canvas")] public TMP_Text currentRpmTMP;
    [FoldoutGroup("SETUP/Components/Canvas")] public Slider accelerationSlider;
    [FoldoutGroup("SETUP/Components/Canvas")] public GameObject handbrakeLight;
    #endregion

    #region Body Parts Animation
    [FoldoutGroup("SETUP/Components/Body Parts Animation")] public Transform steeringWheel;
    [FoldoutGroup("SETUP/Components/Body Parts Animation")] public Transform acceleratorComponent;
    [FoldoutGroup("SETUP/Components/Body Parts Animation")] public Transform brakeComponent;
    [FoldoutGroup("SETUP/Components/Body Parts Animation")] public Transform handbrakeComponent;
    #endregion

    #region Cameras
    [FoldoutGroup("SETUP/Components/Cameras"), SerializeField] private GameObject[] cameras;
    [FoldoutGroup("DEBUG/Cameras"), SerializeField] private int numberOfCameras;
    [FoldoutGroup("DEBUG/Cameras"), SerializeField] private int currentCamera;
    #endregion

    #region Lights
    private bool leftBlinkerIsOn = false;
    private bool rightBlinkerIsOn = false;
    [FoldoutGroup("DEBUG/Lights")] public bool useLights = true;
    #endregion

    protected void Awake()
    {
        cameras[0].SetActive(false);
        currentCamera = 1;

        storedMaxForwardSpeed = maxForwardSpeed;
    }

    protected void Start()
    {
        rb = GetComponent<Rigidbody>();

        maxGear = gearRatios.Length - 1;

        numberOfCameras = cameras.Length;

        // Definizione della curva
        hpToRPMCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f),   // 0% potenza a 0 RPM
            new Keyframe(0.2f, 0.2f),   // 20% potenza a 1200 RPM
            new Keyframe(0.4f, 0.6f),   // 60% potenza a 2400 RPM
            new Keyframe(0.6f, 1.0f),   // 100% potenza a 3600 RPM (picco)
            new Keyframe(0.8f, 0.7f),   // 70% potenza a 4800 RPM
            new Keyframe(1.0f, 0.3f)    // 30% potenza a 6000 RPM
        );

        SetForwardFriction();
        SetSidewaysFriction();
    }

    protected void Update()
    {
        ChangeGearText();

        OnEngineOn();

        AutomaticGearChange();

        LightsManager();
    }

    protected void FixedUpdate()
    {
        CalculateWheelRPM();

        ApplyVehicleAcceleration();
        ApplyEngineBrakeDeceleration();
        ApplyAntiStall();
        ApplyBrakeForce();
        ApplyHandbrakeForce();
        ApplySteering();
    }

    protected void OnEnable()
    {
        // Find Action with Input Action Asset 
        var actionMap = inputActionAsset.FindActionMap("CLifeCarController");

        startAndStopVehicleAction = actionMap.FindAction("Start&StopVehicle");
        startAndStopEngineAction = actionMap.FindAction("Start&StopEngine");
        throttleAction = actionMap.FindAction("Throttle");
        brakeAction = actionMap.FindAction("Brake");
        handbrakeAction = actionMap.FindAction("Handbrake");
        steerAction = actionMap.FindAction("Steer");
        upShiftAction = actionMap.FindAction("UpShift");
        downShiftAction = actionMap.FindAction("DownShift");
        gearTypeAction = actionMap.FindAction("GearType");
        driveTypeAction = actionMap.FindAction("DriveType");
        changeCameraAction = actionMap.FindAction("ChangeCamera");
        leftBlinkerAction = actionMap.FindAction("LeftBlinker");
        rightBlinkerAction = actionMap.FindAction("RightBlinker");

        // Register Callback Events
        startAndStopVehicleAction.performed += StartAndStopVehicle_action;

        startAndStopEngineAction.performed += StartAndStopEngine_action;

        throttleAction.performed += Throttle_action;
        throttleAction.canceled += Throttle_action;

        brakeAction.performed += Brake_action;
        brakeAction.canceled += Brake_action;

        handbrakeAction.performed += Handbrake_action;
        handbrakeAction.canceled += Handbrake_action;

        steerAction.performed += Steer_action;
        steerAction.canceled += Steer_action;

        upShiftAction.performed += UpShift_action;

        downShiftAction.performed += DownShift_action;

        gearTypeAction.performed += GearType_action;

        driveTypeAction.performed += DriveType_action;

        changeCameraAction.performed += ChangeCamera_action;

        leftBlinkerAction.performed += LeftBlinker_action;

        rightBlinkerAction.performed += RightBlinker_action;

        // Enable Action
        startAndStopVehicleAction.Enable();
        startAndStopEngineAction.Enable();
        throttleAction.Enable();
        brakeAction.Enable();
        handbrakeAction.Enable();
        steerAction.Enable();
        upShiftAction.Enable();
        downShiftAction.Enable();
        gearTypeAction.Enable();
        driveTypeAction.Enable();
        changeCameraAction.Enable();
        leftBlinkerAction.Enable();
        rightBlinkerAction.Enable();
    }

    protected void OnDisable()
    {
        // Remove callback
        startAndStopVehicleAction.performed -= StartAndStopVehicle_action;

        startAndStopEngineAction.performed -= StartAndStopEngine_action;

        throttleAction.performed -= Throttle_action;
        throttleAction.canceled -= Throttle_action;

        brakeAction.performed -= Brake_action;
        brakeAction.canceled -= Brake_action;

        handbrakeAction.performed -= Handbrake_action;
        handbrakeAction.canceled -= Handbrake_action;

        steerAction.performed -= Steer_action;
        steerAction.canceled -= Steer_action;

        upShiftAction.performed -= UpShift_action;

        downShiftAction.performed -= DownShift_action;

        gearTypeAction.performed -= GearType_action;

        driveTypeAction.performed -= DriveType_action;

        changeCameraAction.performed -= ChangeCamera_action;

        leftBlinkerAction.performed -= LeftBlinker_action;

        rightBlinkerAction.performed -= RightBlinker_action;

        // Disable Action
        startAndStopVehicleAction.Disable();
        startAndStopEngineAction.Disable();
        throttleAction.Disable();
        brakeAction.Disable();
        handbrakeAction.Disable();
        steerAction.Disable();
        upShiftAction.Disable();
        downShiftAction.Disable();
        gearTypeAction.Disable();
        driveTypeAction.Disable();
        changeCameraAction.Disable();
        leftBlinkerAction.Disable();
        rightBlinkerAction.Disable();
    }

    #region Input Action Callbacks
    private void StartAndStopVehicle_action(InputAction.CallbackContext obj)
    {
        StartAndStopVehicle();
    }

    private void StartAndStopEngine_action(InputAction.CallbackContext obj)
    {
        StartAndStopEngine();
    }

    private void Throttle_action(InputAction.CallbackContext obj)
    {
        Throttle(obj.ReadValue<float>(), animatePedal:true);
    }

    private void Brake_action(InputAction.CallbackContext obj)
    {
        Brake(obj.ReadValue<float>(), animatePedal:true);
    }

    private void Handbrake_action(InputAction.CallbackContext obj)
    {
        Handbrake(obj.ReadValue<float>(), animateHandbrake:true);
    }

    private void Steer_action(InputAction.CallbackContext obj)
    {
        Steer(obj.ReadValue<float>());
    }

    private void UpShift_action(InputAction.CallbackContext obj)
    {
        UpShift();
    }

    private void DownShift_action(InputAction.CallbackContext obj)
    {
        DownShift();
    }

    private void GearType_action(InputAction.CallbackContext obj)
    {
        ChangeGearType();
    }

    private void DriveType_action(InputAction.CallbackContext obj)
    {
        ChangeDriveType();
    }

    private void ChangeCamera_action(InputAction.CallbackContext obj)
    {
        ChangeCamera();
    }

    private void LeftBlinker_action(InputAction.CallbackContext obj)
    {
        ActivateLeftBlinker();
    }

    private void RightBlinker_action(InputAction.CallbackContext obj)
    {
        ActivateRightBlinker();
    }

    #endregion

    #region Public Methods
    public void StartAndStopVehicle()
    {
        StartAndStopVehicle_impl();
    }

    public void StartAndStopEngine()
    {
        StartAndStopEngine_impl();
    }

    public void Throttle(float throttleAmount, bool animatePedal = false)
    {
        Throttle_impl(throttleAmount);

        if (animatePedal)
            AnimateAcceleratorPedal();
    }

    public void Throttle(float throttleAmount, float maxSpeed, bool animatePedal = false)
    {
        Throttle_impl(throttleAmount, maxSpeed);

        if (animatePedal)
            AnimateAcceleratorPedal();
    }

    public void Brake(float brakeAmount, bool animatePedal = false)
    {
        Brake_impl(brakeAmount);

        if (animatePedal)
            AnimateBrakePedal();
    }

    public void Handbrake(float handbrakeAmount, bool animateHandbrake = false)
    {
        Handbrake_impl(handbrakeAmount);

        if (animateHandbrake)
            AnimateHandbrake();

        TurnOnHandBrakeLight();
    }

    public void Steer(float steeringAmount)
    {
        Steer_impl(steeringAmount);
    }

    public void Steer(float steeringAmount, float steeringAngle)
    {
        Steer_impl(steeringAmount, steeringAngle);
    }

    public void UpShift()
    {
        UpShift_impl();
    }

    public void DownShift()
    {
        DownShift_impl();
    }

    public void ChangeGearType()
    {
        ChangeGearType_impl();
    }

    public void ChangeDriveType()
    {
        ChangeDriveType_impl();
    }

    public void ChangeCamera()
    {
        ChangeCamera_impl();
    }

    public void ActivateLeftBlinker()
    {
        ActivateLeftBlinker_impl();
    }

    public void ActivateRightBlinker()
    {
        ActivateRightBlinker_impl();
    }

    #endregion

    #region Methods implementation
    private void StartAndStopVehicle_impl()
    {
        if (isStarted)
        {
            if (isEngineOn)
                isEngineOn = false;

            isStarted = false;
            Debug.Log("Vehicle Stopped");

            // Turn lights OFF
        }
        else
        {
            isStarted = true;
            Debug.Log("Vehicle Started");

            // Turn lights ON
        }
    }

    private void StartAndStopEngine_impl()
    {
        if (isEngineOn)
        {
            isEngineOn = false;
            Debug.Log("Engine Off");

            // Turn engine sound OFF
            //soundController.TurnOnEngineSound();
        }
        else if (isStarted)
        {
            isEngineOn = true;
            Debug.Log("Engine On");

            // Turn engine sound ON
            //soundController.TurnOffEngineSound();
        }
    }

    private void Throttle_impl(float throttleAmount)
    {
        if (throttleAmount < 0)
        {
            if (currentGear == -1)
                this.throttleAmount = throttleAmount;
        }
        else if (throttleAmount > 0)
        {
            if (currentGear >= 0)
                this.throttleAmount = throttleAmount;
        }
        else
            this.throttleAmount = throttleAmount;
    }

    private void Throttle_impl(float throttleAmount, float maxSpeed)
    {
        if (throttleAmount < 0)
        {
            if (currentGear == -1)
            {
                maxReverseSpeed = maxSpeed;
                this.throttleAmount = throttleAmount;
            }
        }
        else if (throttleAmount > 0)
        {
            if (currentGear >= 0)
            {
                maxForwardSpeed = maxSpeed;
                this.throttleAmount = throttleAmount;
            }
        }
        else
        {
            maxForwardSpeed = maxSpeed;
            this.throttleAmount = throttleAmount;
        }
    }

    private void Brake_impl(float brakeAmount)
    {
        this.brakeAmount = brakeAmount;
    }

    private void Handbrake_impl(float handbrakeAmount)
    {
        this.handbrakeAmount = handbrakeAmount;
    }

    private void Steer_impl(float steeringAmount)
    {
        this.steeringAmount = steeringAmount;
        steeringAngle = maxSteeringAngle;
    }

    private void Steer_impl(float steeringAmount, float steeringAngle)
    {
        this.steeringAmount = steeringAmount;
        this.steeringAngle = steeringAngle;
    }

    private void UpShift_impl()
    {
        if (gearType == GearType.SemiAutomatic)
        {
            if (currentGear < maxGear)
                currentGear++;
        }
        else if (gearType == GearType.Automatic)
        {
            switch (automaticGear)
            {
                case AutomaticGear.Reverse:
                    automaticGear = AutomaticGear.Neutral;
                    break;

                case AutomaticGear.Neutral:
                    automaticGear = AutomaticGear.Drive;
                    currentGear++;
                    break;
            }
        }
    }

    private void DownShift_impl()
    {
        if (gearType == GearType.SemiAutomatic)
        {
            if (currentGear > -1)
                currentGear--;
        }
        else if (gearType == GearType.Automatic)
        {
            switch (automaticGear)
            {
                case AutomaticGear.Neutral:
                    automaticGear = AutomaticGear.Reverse;
                    break;

                case AutomaticGear.Drive:
                    automaticGear = AutomaticGear.Neutral;
                    break;
            }
        }
    }

    private void ChangeGearType_impl()
    {
        switch (gearType)
        {
            case GearType.Automatic:
                gearType = GearType.SemiAutomatic;
                break;

            case GearType.SemiAutomatic:
                gearType = GearType.Automatic;
                break;
        }
    }

    private void ChangeDriveType_impl()
    {
        switch (driveType)
        {
            case DriveType.AWD:
                driveType = DriveType.FWD;
                break;

            case DriveType.FWD:
                driveType = DriveType.RWD;
                break;

            case DriveType.RWD:
                driveType = DriveType.AWD;
                break;
        }
    }

    private void ChangeCamera_impl()
    {
        int previousCamera = currentCamera;

        if (currentCamera < numberOfCameras - 1)
            currentCamera++;
        else
            currentCamera = 0;

        cameras[previousCamera].SetActive(false);
        cameras[currentCamera].SetActive(true);
    }

    private void ActivateLeftBlinker_impl()
    {
        if (lightsController != null && useLights == true)
        {
            leftBlinkerIsOn = !leftBlinkerIsOn;

            if (leftBlinkerIsOn)
            {
                lightsController.TurnOnLeftBlinkers();
                leftBlinkerIsOn = false;
            }
            else
                lightsController.TurnOffBlinkers();
        }
    }

    private void ActivateRightBlinker_impl()
    {
        if (lightsController != null && useLights == true)
        {
            rightBlinkerIsOn = !rightBlinkerIsOn;

            if (rightBlinkerIsOn)
            {
                lightsController.TurnOnRightBlinkers();
                rightBlinkerIsOn = false;
            }
            else
                lightsController.TurnOffBlinkers();
        }
    }
    #endregion

    #region Start & Stop
    protected void OnEngineOn()
    {
        if (isEngineOn)
        {
            // Calculate RPM
            CalculateEngineRPM();
        }
        else
        {
            // Set Motor Torque to 0
            frontLeftWheelCollider.motorTorque = 0;
            frontRightWheelCollider.motorTorque = 0;
            rearLeftWheelCollider.motorTorque = 0;
            rearRightWheelCollider.motorTorque = 0;
        }
    }
    #endregion

    #region Change gear
    protected void AutomaticGearChange()
    {
        if(gearType == GearType.Automatic)
        {
            switch (automaticGear)
            {
                case AutomaticGear.Reverse:
                    currentGear = -1;
                    break;

                case AutomaticGear.Neutral:
                    currentGear = 0;
                    break;

                case AutomaticGear.Drive:
                    if (currentRPM >= automaticGearChangeMaxRPM && currentGear < maxGear)
                        currentGear++;
                    else if (currentRPM < automaticGearChangeMinRPM && currentGear > 1)
                        currentGear--;
                    break;
            }
        }
    }
    #endregion

    #region RPM & Anti-Stall
    protected void CalculateWheelRPM()
    {
        frontLeftWheelRPM = frontLeftWheelCollider.rpm;
        frontRightWheelRPM = frontRightWheelCollider.rpm;
        rearLeftWheelRPM = rearLeftWheelCollider.rpm;
        rearRightWheelRPM = rearRightWheelCollider.rpm;
    }

    protected void CalculateEngineRPM()
    {
        float wheelsRPM = Mathf.Abs((frontLeftWheelRPM + frontRightWheelRPM + rearLeftWheelRPM + rearRightWheelRPM) / 4f);

        if (gearType == GearType.SemiAutomatic || gearType == GearType.Automatic)
        {
            // engineRPM = wheelRPM * totalTransmissionRatio
            //           = wheelRPM * (currentGearRatio * differentialRatio)

            if (currentGear == 0) // Neutral
                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM, maxRPM * throttleAmount) + Random.Range(-50, 50), Time.deltaTime);
            else
            {
                if (currentGear == -1) // Reverse
                    wheelsRPM = wheelsRPM * reverseGearRatio * differentialRatio;

                else if (currentGear > 0) // Forward
                    wheelsRPM = wheelsRPM * gearRatios[currentGear] * differentialRatio;

                currentRPM = Mathf.Lerp(currentRPM, Mathf.Max(idleRPM - 100, wheelsRPM), Time.deltaTime * 3f);
            }

            // Clamp RPM to respect idleRPM and maxRPM limits
            currentRPM = Mathf.Clamp(currentRPM, idleRPM, maxRPM);

            UpdateRPMText(currentRPM);
        }
    }

    protected void ApplyAntiStall()
    {
        if (gearType == GearType.SemiAutomatic)
        {
            if (currentGear > 1 && currentRPM <= idleRPM)
            {
                DownShift();
            }
        }
    }
    #endregion

    #region Vehicle Internal Methods

    //protected void ApplyVehicleAcceleration()
    //{
    //    if (isStarted && isEngineOn)
    //    {
    //        gearDeceleration = Mathf.Lerp(1f, 0f, currentRPM / maxRPM) * 0.6f;

    //        switch (currentGear)
    //        {
    //            case -1:
    //                // Reverse
    //                if (throttleAmount < 0 && currentSpeed < maxReverseSpeed && currentRPM < maxRPM)
    //                {
    //                    torque = (hpToRPMCurve.Evaluate(currentRPM / maxRPM) * horsePower / currentRPM) *
    //                              reverseGearRatio * differentialRatio * 5252f * gearDeceleration;

    //                    frontLeftWheelCollider.motorTorque = torque * throttleAmount;
    //                    frontRightWheelCollider.motorTorque = torque * throttleAmount;
    //                }
    //                else
    //                {
    //                    frontLeftWheelCollider.motorTorque = 0;
    //                    frontRightWheelCollider.motorTorque = 0;
    //                    rearLeftWheelCollider.motorTorque = 0;
    //                    rearRightWheelCollider.motorTorque = 0;
    //                }
    //                break;

    //            case 0:
    //                // Neutral
    //                frontLeftWheelCollider.motorTorque = 0;
    //                frontRightWheelCollider.motorTorque = 0;
    //                rearLeftWheelCollider.motorTorque = 0;
    //                rearRightWheelCollider.motorTorque = 0;
    //                break;

    //            default:
    //                if (currentGear > 0)
    //                {
    //                    // Drive
    //                    if (throttleAmount > 0 && currentSpeed < maxForwardSpeed && currentRPM < maxRPM)
    //                    {
    //                        torque = (hpToRPMCurve.Evaluate(currentRPM / maxRPM) * horsePower / currentRPM) *
    //                                  gearRatios[currentGear] * differentialRatio * 5252f * gearDeceleration;

    //                        frontLeftWheelCollider.motorTorque = torque * throttleAmount;
    //                        frontRightWheelCollider.motorTorque = torque * throttleAmount;
    //                    }
    //                    else
    //                    {
    //                        frontLeftWheelCollider.motorTorque = 0;
    //                        frontRightWheelCollider.motorTorque = 0;
    //                        rearLeftWheelCollider.motorTorque = 0;
    //                        rearRightWheelCollider.motorTorque = 0;
    //                    }
    //                }
    //                break;
    //        }

    //        currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Convert speed from m/s to km/h

    //        UpdateAccelerationSlider();
    //        UpdateSpeedText(currentSpeed);
    //    }
    //}

    protected void ApplyVehicleAcceleration()
    {
        if (isStarted && isEngineOn)
        {
            //gearDeceleration = Mathf.Lerp(1f, 0f, currentRPM / maxRPM) * 0.6f;

            switch (currentGear)
            {
                case -1:
                    // Reverse
                    if (throttleAmount < 0 && currentSpeed < maxReverseSpeed && currentRPM < maxRPM)
                        torque = (hpToRPMCurve.Evaluate(currentRPM / maxRPM) * horsePower / currentRPM) *
                                  reverseGearRatio * differentialRatio * 7127f /* * gearDeceleration*/;
                    else
                        torque = 0f;

                    break;

                case 0:
                    // Neutral
                    torque = 0f;

                    break;

                default:
                    if (currentGear > 0)
                    {
                        // Drive
                        if (throttleAmount > 0 && currentSpeed < maxForwardSpeed && currentRPM < maxRPM)
                            torque = (hpToRPMCurve.Evaluate(currentRPM / maxRPM) * horsePower / currentRPM) *
                                      gearRatios[currentGear] * differentialRatio * 7127f /* * gearDeceleration*/;
                        else
                            torque = 0f;
                    }
                    break;
            }

            switch (driveType)
            {
                case DriveType.FWD:

                    frontLeftWheelCollider.motorTorque = (torque / 2) * throttleAmount;
                    frontRightWheelCollider.motorTorque = (torque / 2) * throttleAmount;
                    rearLeftWheelCollider.motorTorque = 0;
                    rearRightWheelCollider.motorTorque = 0;

                    break;

                case DriveType.RWD:

                    frontLeftWheelCollider.motorTorque = 0;
                    frontRightWheelCollider.motorTorque = 0;
                    rearLeftWheelCollider.motorTorque = (torque / 2) * throttleAmount;
                    rearRightWheelCollider.motorTorque = (torque / 2) * throttleAmount;

                    break;

                case DriveType.AWD:

                    frontLeftWheelCollider.motorTorque = (torque / 4) * throttleAmount;
                    frontRightWheelCollider.motorTorque = (torque / 4) * throttleAmount;
                    rearLeftWheelCollider.motorTorque = (torque / 4) * throttleAmount;
                    rearRightWheelCollider.motorTorque = (torque / 4) * throttleAmount;

                    break;
            }

            currentSpeed = rb.linearVelocity.magnitude * 3.6f; // Convert speed from m/s to km/h

            UpdateAccelerationSlider();
            UpdateSpeedText(currentSpeed);
        }
    }

    protected void ApplyEngineBrakeDeceleration()
    {
        if (rb != null)
        {
            if (throttleAmount == 0 || currentGear == 0)
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * decelerationSpeed);
        }
    }

    protected void ApplyBrakeForce()
    {
        if (brakeAmount > 0f)
        {
            frontLeftWheelCollider.brakeTorque = brakeAmount * brakeForce;
            frontRightWheelCollider.brakeTorque = brakeAmount * brakeForce;
        }
        else
        {
            frontLeftWheelCollider.brakeTorque = 0;
            frontRightWheelCollider.brakeTorque = 0;
        }
    }

    protected void ApplyHandbrakeForce()
    {
        if (handbrakeAmount > 0f)
        {
            rearLeftWheelCollider.brakeTorque = handbrakeAmount * brakeForce;
            rearRightWheelCollider.brakeTorque = handbrakeAmount * brakeForce;

            StartDrifting(handbrakeAmount);
        }
        else
        {
            rearLeftWheelCollider.brakeTorque = 0;
            rearRightWheelCollider.brakeTorque = 0;

            StopDrifting();
        }
    }

    #region Wheels
    protected void ApplySteering()
    {
        float steeringPercentage = steeringAmount * steeringAngle;

        if(steeringPercentage >= 0f)
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, Mathf.Clamp(steeringPercentage, 0, maxSteeringAngle), Time.deltaTime * steeringSpeed * 10f);
        else
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, Mathf.Clamp(steeringPercentage, -maxSteeringAngle, 0), Time.deltaTime * steeringSpeed * 10f);

        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

        UpdateWheelPosition(frontLeftWheelCollider, frontLeftWheelMesh);
        UpdateWheelPosition(frontRightWheelCollider, frontRightWheelMesh);
        UpdateWheelPosition(rearLeftWheelCollider, rearLeftWheelMesh);
        UpdateWheelPosition(rearRightWheelCollider, rearRightWheelMesh);

        AnimateSteeringWheel();
    }

    protected void UpdateWheelPosition(WheelCollider col, Transform mesh)
    {
        col.GetWorldPose(out Vector3 position, out Quaternion rotation);
        mesh.SetPositionAndRotation(position, rotation);
    }
    #endregion

    #region Friction
    protected void SetForwardFriction()
    {
        FLWForwardFriction = new WheelFrictionCurve
        {
            extremumSlip = frontLeftWheelCollider.forwardFriction.extremumSlip,
            extremumValue = frontLeftWheelCollider.forwardFriction.extremumValue,
            asymptoteSlip = frontLeftWheelCollider.forwardFriction.asymptoteSlip,
            asymptoteValue = frontLeftWheelCollider.forwardFriction.asymptoteValue,
            stiffness = frontLeftWheelCollider.forwardFriction.stiffness
        };

        FRWForwardFriction = new WheelFrictionCurve
        {
            extremumSlip = frontRightWheelCollider.forwardFriction.extremumSlip,
            extremumValue = frontRightWheelCollider.forwardFriction.extremumValue,
            asymptoteSlip = frontRightWheelCollider.forwardFriction.asymptoteSlip,
            asymptoteValue = frontRightWheelCollider.forwardFriction.asymptoteValue,
            stiffness = frontRightWheelCollider.forwardFriction.stiffness
        };

        RLWForwardFriction = new WheelFrictionCurve
        {
            extremumSlip = rearLeftWheelCollider.forwardFriction.extremumSlip,
            extremumValue = rearLeftWheelCollider.forwardFriction.extremumValue,
            asymptoteSlip = rearLeftWheelCollider.forwardFriction.asymptoteSlip,
            asymptoteValue = rearLeftWheelCollider.forwardFriction.asymptoteValue,
            stiffness = rearLeftWheelCollider.forwardFriction.stiffness
        };

        RRWForwardFriction = new WheelFrictionCurve
        {
            extremumSlip = rearRightWheelCollider.forwardFriction.extremumSlip,
            extremumValue = rearRightWheelCollider.forwardFriction.extremumValue,
            asymptoteSlip = rearRightWheelCollider.forwardFriction.asymptoteSlip,
            asymptoteValue = rearRightWheelCollider.forwardFriction.asymptoteValue,
            stiffness = rearRightWheelCollider.forwardFriction.stiffness
        };
    }

    protected void SetSidewaysFriction()
    {
        FLWSidewaysFriction = new WheelFrictionCurve
        {
            extremumSlip = frontLeftWheelCollider.sidewaysFriction.extremumSlip,
            extremumValue = frontLeftWheelCollider.sidewaysFriction.extremumValue,
            asymptoteSlip = frontLeftWheelCollider.sidewaysFriction.asymptoteSlip,
            asymptoteValue = frontLeftWheelCollider.sidewaysFriction.asymptoteValue,
            stiffness = frontLeftWheelCollider.sidewaysFriction.stiffness
        };

        FRWSidewaysFriction = new WheelFrictionCurve
        {
            extremumSlip = frontRightWheelCollider.sidewaysFriction.extremumSlip,
            extremumValue = frontRightWheelCollider.sidewaysFriction.extremumValue,
            asymptoteSlip = frontRightWheelCollider.sidewaysFriction.asymptoteSlip,
            asymptoteValue = frontRightWheelCollider.sidewaysFriction.asymptoteValue,
            stiffness = frontRightWheelCollider.sidewaysFriction.stiffness
        };

        RLWSidewaysFriction = new WheelFrictionCurve
        {
            extremumSlip = rearLeftWheelCollider.sidewaysFriction.extremumSlip,
            extremumValue = rearLeftWheelCollider.sidewaysFriction.extremumValue,
            asymptoteSlip = rearLeftWheelCollider.sidewaysFriction.asymptoteSlip,
            asymptoteValue = rearLeftWheelCollider.sidewaysFriction.asymptoteValue,
            stiffness = rearLeftWheelCollider.sidewaysFriction.stiffness
        };

        RRWSidewaysFriction = new WheelFrictionCurve
        {
            extremumSlip = rearRightWheelCollider.sidewaysFriction.extremumSlip,
            extremumValue = rearRightWheelCollider.sidewaysFriction.extremumValue,
            asymptoteSlip = rearRightWheelCollider.sidewaysFriction.asymptoteSlip,
            asymptoteValue = rearRightWheelCollider.sidewaysFriction.asymptoteValue,
            stiffness = rearRightWheelCollider.sidewaysFriction.stiffness
        };
    }

    protected void StartDrifting(float currentHandbrakeValue)
    {
        //use if you need to

        //rLwheelForwardFriction.extremumSlip = 
        //rRwheelForwardFriction.extremumSlip = 
        //rLwheelForwardFriction.extremumValue = 
        //rRwheelForwardFriction.extremumValue = 

        RLWSidewaysFriction.extremumSlip = 3f * currentHandbrakeValue;
        RRWSidewaysFriction.extremumSlip = 3f * currentHandbrakeValue;
        RLWSidewaysFriction.extremumValue = 0.7f * currentHandbrakeValue;
        RRWSidewaysFriction.extremumValue = 0.7f * currentHandbrakeValue;

        //Debug.Log(rLWSidewaysFriction.extremumSlip.ToString());

        //ezerealCarController.rearLeftWheelCollider.forwardFriction = rLwheelForwardFriction;
        //ezerealCarController.rearRightWheelCollider.forwardFriction = rRwheelForwardFriction;

        rearLeftWheelCollider.sidewaysFriction = RLWSidewaysFriction;
        rearRightWheelCollider.sidewaysFriction = RRWSidewaysFriction;
    }

    protected void StopDrifting()
    {
        //use if you need to

        //rLwheelForwardFriction.extremumSlip = 
        //rRwheelForwardFriction.extremumSlip = 
        //rLwheelForwardFriction.extremumValue = 
        //rRwheelForwardFriction.extremumValue = 

        //Set default value here
        RLWSidewaysFriction.extremumSlip = 0.2f;
        RRWSidewaysFriction.extremumSlip = 0.2f;
        RLWSidewaysFriction.extremumValue = 1f;
        RRWSidewaysFriction.extremumValue = 1f;

        //Debug.Log(rLWSidewaysFriction.extremumSlip.ToString());

        //ezerealCarController.rearLeftWheelCollider.forwardFriction = rLwheelForwardFriction;
        //ezerealCarController.rearRightWheelCollider.forwardFriction = rRwheelForwardFriction;

        rearLeftWheelCollider.sidewaysFriction = RLWSidewaysFriction;
        rearRightWheelCollider.sidewaysFriction = RRWSidewaysFriction;
    }
    #endregion

    #endregion

    #region UI
    protected void UpdateGearText(string gear)
    {
        currentGearTMP.text = gear;
    }
    
    protected void UpdateSpeedText(float speed)
    {
        speed = Mathf.Abs(speed);
        currentSpeedTMP.text = "KM/H\n" + speed.ToString("F0");
    }

    protected void UpdateRPMText(float rpm)
    {
        rpm = Mathf.Abs(rpm);
        currentRpmTMP.text = "RPM\n" + rpm.ToString("F0");
    }

    protected void ChangeGearText()
    {
        string gearText = "";

        if (currentGear == -1)
            gearText = "R";
        else if (currentGear == 0)
            gearText = "N";
        else if (currentGear >= 1)
        {
            switch (gearType)
            {
                case GearType.Automatic:
                    gearText = "D";
                    break;

                case GearType.SemiAutomatic:
                    for (int i = 1; i < gearRatios.Length; i++)
                    {
                        if (currentGear == i)
                            gearText = currentGear.ToString();
                    }
                    break;
            }
        }

        UpdateGearText(gearText);
    }

    protected void UpdateAccelerationSlider()
    {
        if (currentGear == -1 || currentGear > 0)
            accelerationSlider.value = Mathf.Lerp(accelerationSlider.value, throttleAmount, Time.deltaTime * 15f);
        else
            accelerationSlider.value = 0;
    }

    protected void TurnOnHandBrakeLight()
    {
        if (handbrakeAmount > 0)
            handbrakeLight.SetActive(true);
        else
            handbrakeLight.SetActive(false);
    }
    #endregion

    #region Body Parts Animation
    protected void AnimateSteeringWheel()
    {
        float currentXAngle = steeringWheel.transform.localEulerAngles.x; // Maximum steer angle in degrees

        // Calculate the rotation based on the steer angle
        float normalizedSteerAngle = Mathf.Clamp(frontLeftWheelCollider.steerAngle, -maxSteeringAngle, maxSteeringAngle);
        float rotation = Mathf.Lerp(maxSteeringWheelRotation, -maxSteeringWheelRotation, (normalizedSteerAngle + maxSteeringAngle) / (2 * maxSteeringAngle));

        // Set the local rotation of the steering wheel
        steeringWheel.localRotation = Quaternion.Euler(currentXAngle, 0, rotation);
    }

    protected void AnimateAcceleratorPedal()
    {
        float currentYAngle = acceleratorComponent.transform.rotation.y;
        float currentZAngle = acceleratorComponent.transform.rotation.z;

        float rotation = Mathf.Lerp(0f, -25f, Mathf.Abs(throttleAmount));

        acceleratorComponent.localRotation = Quaternion.Euler(rotation, currentYAngle, currentZAngle);
    }

    protected void AnimateBrakePedal()
    {
        float currentYAngle = brakeComponent.transform.rotation.y;
        float currentZAngle = brakeComponent.transform.rotation.z;

        float rotation = Mathf.Lerp(0f, -25f, brakeAmount);

        brakeComponent.localRotation = Quaternion.Euler(rotation, currentYAngle, currentZAngle);
    }

    protected void AnimateHandbrake()
    {
        float currentYAngle = handbrakeComponent.transform.rotation.y;
        float currentZAngle = handbrakeComponent.transform.rotation.z;

        float rotation = Mathf.Lerp(0f, -40f, handbrakeAmount);

        handbrakeComponent.localRotation = Quaternion.Euler(rotation, currentYAngle, currentZAngle);
    }
    #endregion

    #region Lights
    private void LightsManager()
    {
        if(lightsController != null && useLights == true)
        {
            if (isStarted)
            {
                lightsController.TurnOnHeadlights();
                lightsController.TurnOnRunningLights();

                if (brakeAmount > 0 || handbrakeAmount > 0)
                    lightsController.TurnOnBrakeLights();
                else
                    lightsController.TurnOffBrakeLights();

                if(canvas != null)
                    canvas.SetActive(true);

                if (isEngineOn)
                {
                    if (currentGear == -1)
                        lightsController.TurnOnreverseLights();
                    else
                        lightsController.TurnOffreverseLights();
                }
            }
            else
            {
                lightsController.TurnOffHeadlights();
                lightsController.TurnOffRunningLights();

                if (canvas != null)
                    canvas.SetActive(false);
            }
        }
    }
    #endregion
}
