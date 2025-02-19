using UnityEngine;
using Sirenix.OdinInspector;
using Ezereal;
using UnityEngine.InputSystem;

public class MyUltimateCarController : MonoBehaviour
{
    #region Setup Vehicle Parameters
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Components")] public Rigidbody vehicleRB;
    [FoldoutGroup("SETUP/Components"), SerializeField] private WheelCollider frontLeftWheelCollider;
    [FoldoutGroup("SETUP/Components"), SerializeField] private WheelCollider frontRightWheelCollider;
    [FoldoutGroup("SETUP/Components"), SerializeField] private WheelCollider rearLeftWheelCollider;
    [FoldoutGroup("SETUP/Components"), SerializeField] private WheelCollider rearRightWheelCollider;
    [FoldoutGroup("SETUP/Components"), SerializeField] private WheelCollider[] wheels;

    [FoldoutGroup("SETUP/Components"), SerializeField] private Transform frontLeftWheelMesh;
    [FoldoutGroup("SETUP/Components"), SerializeField] private Transform frontRightWheelMesh;
    [FoldoutGroup("SETUP/Components"), SerializeField] private Transform rearLeftWheelMesh;
    [FoldoutGroup("SETUP/Components"), SerializeField] private Transform rearRightWheelMesh;

    [FoldoutGroup("SETUP/Vehicle Parameters")] public float maxForwardSpeed = 100f; // 100f default
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float maxReverseSpeed = 30f; // 30f default
    
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float horsePower = 1000f; // 1000f default
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float brakePower = 2000f; // 2000f default
    
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float handbrakeForce = 3000f; // 3000f default
    
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float maxSteeringAngle = 30f; // 30f default
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float maxSteeringWheelRotation = 360f; // 360 for real steering wheel. 120 would be more suitable for racing.
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float steeringSpeed = 5f; // 0.5f default
    
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float stopThreshold = 1f; // 1f default. At what speed car will make a full stop
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float decelerationSpeed = 0.5f; // 0.5f default
    
    [FoldoutGroup("DEBUG")]
    [FoldoutGroup("DEBUG/Start&Stop")] public bool isStarted = false;

    [FoldoutGroup("DEBUG")] public float currentSpeed = 0f;
    [FoldoutGroup("DEBUG")] public float currentAccelerationValue = 0f;
    
    [FoldoutGroup("DEBUG")] public float currentBrakeValue = 0f;
    [FoldoutGroup("DEBUG")] public float currentHandbrakeValue = 0f;
    
    [FoldoutGroup("DEBUG")] public float currentSteerAngle = 0f;
    [FoldoutGroup("DEBUG")] public float targetSteerAngle = 0f;

    [FoldoutGroup("DEBUG")] public float FrontLeftWheelRPM = 0f;
    [FoldoutGroup("DEBUG")] public float FrontRightWheelRPM = 0f;
    [FoldoutGroup("DEBUG")] public float RearLeftWheelRPM = 0f;
    [FoldoutGroup("DEBUG")] public float RearRightWheelRPM = 0f;
    
    [FoldoutGroup("DEBUG")] public float currentMotorTorque = 0f;

    [FoldoutGroup("DEBUG")] public bool stationary = true;
    
    private float speedFactor = 0f; // Leave at zero. Responsible for smooth acceleration and near-top-speed slowdown.

    #region Engine parameters

    [FoldoutGroup("DEBUG/Start&Stop")] public bool isEngineOn = false;

    [FoldoutGroup("SETUP/Vehicle Parameters")] public float engineIdleRPM = 1000f;
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float engineMaxRPM = 5000f;
    [FoldoutGroup("SETUP/Vehicle Parameters")] public float engineLimiterRPM = 4500f;
    [FoldoutGroup("DEBUG")] public float engineCurrentRPM = 0f;

    #endregion

    #region Drive type

    public enum DriveType
    {
        RWD,
        FWD,
        AWD
    }
    [FoldoutGroup("SETUP/Drive Type")] public DriveType driveType = DriveType.RWD;

    #endregion

    #region Gear parameters

    // Gear Type
    [FoldoutGroup("SETUP/Gear")]

    public enum GearType
    {
        Automatic,
        Manual
    }
    [FoldoutGroup("SETUP/Gear/Gear Type")] public GearType gearType = GearType.Automatic;

    // Automatic Gear
    public enum AutomaticGears
    {
        Drive,
        Neutral,
        Reverse
    }
    [FoldoutGroup("SETUP/Gear/Automatic gear")] public AutomaticGears automaticGear = AutomaticGears.Drive;

    // Manual Gear
    public enum ManualGears
    {
        Neutral,
        First,
        Second,
        Third,
        Fourth,
        Fifth,
        Sixth,
        Reverse
    }
    [FoldoutGroup("SETUP/Gear/Manual gear")] public ManualGears manualGear = ManualGears.Neutral;
                        
    [FoldoutGroup("SETUP/Gear/Manual gear/Gear Ratios")] public float[] gearRatios = { 0f, 3.50f, 2.10f, 1.40f, 1.00f, 0.80f, 0.60f }; // gear ratio from neutral to sixth
    [FoldoutGroup("SETUP/Gear/Manual gear/Gear Ratios")] public float reverseGearRatio = 3.80f; // reverse gear ratio
                        
    [FoldoutGroup("SETUP/Gear/Manual gear/Setup gear"), Range(1,6)] public int maxGear = 6;
    [FoldoutGroup("SETUP/Gear/Manual gear/Setup gear")] public int minGear = -1;
    [FoldoutGroup("SETUP/Gear/Manual gear/Setup gear")] public int actualGear = 0;

    #endregion

    #region Input evaluation

    public enum InputType
    {
        Keyboard,
        SteeringWheel,
    }
    [FoldoutGroup("SETUP/Input Evaluation")] public InputType inputType = InputType.Keyboard;

    #endregion

    #endregion

    private void Awake()
    {
        wheels = new WheelCollider[]
        {
            frontLeftWheelCollider,
            frontRightWheelCollider,
            rearLeftWheelCollider,
            rearRightWheelCollider,
        };
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        OnVehicleStarted();
        OnEngineOn();
    }

    public void OnVehicleStarted()
    {
        isStarted = !isStarted;

        if (isStarted)
        {
            Debug.Log("Vehicle started!");

            // Turn on Lights

            // Change gear
            if(gearType == GearType.Manual)
                ChangeManualGear();
        }
        else
        {
            Debug.Log("Vehicle turned off!");

            // Turn Off Lights
        }
    }

    public void OnEngineOn()
    {
        isEngineOn = !isEngineOn;

        if (isEngineOn)
        {
            // Turn On Engine Sound

            // Calculate RPM
            CalculateEngineRPM();
        }
        else
        {
            // Turn Off Engine Sound

            // Set Motor Torque to 0
            frontLeftWheelCollider.motorTorque = 0;
            frontRightWheelCollider.motorTorque = 0;
            rearLeftWheelCollider.motorTorque = 0;
            rearRightWheelCollider.motorTorque = 0;
        }
    }

    public void InputManager()
    {
        switch (inputType)
        {
            case InputType.Keyboard:

                if (Input.GetKey("W"))
                {
                    currentAccelerationValue = 1;
                }
                if (Input.GetKey("D"))
                {

                }

                break;

            case InputType.SteeringWheel: 
                
                // Implementare logica Volante

                break;
        }
    }

    public void EvaluateDriveType(float currentMotorTorque)
    {
        if (driveType == DriveType.RWD)
        {
            rearLeftWheelCollider.motorTorque = currentMotorTorque;
            rearRightWheelCollider.motorTorque = currentMotorTorque;
        }
        else if (driveType == DriveType.FWD)
        {
            frontLeftWheelCollider.motorTorque = currentMotorTorque;
            frontRightWheelCollider.motorTorque = currentMotorTorque;
        }
        else if (driveType == DriveType.AWD)
        {
            frontLeftWheelCollider.motorTorque = currentMotorTorque;
            frontRightWheelCollider.motorTorque = currentMotorTorque;
            rearLeftWheelCollider.motorTorque = currentMotorTorque;
            rearRightWheelCollider.motorTorque = currentMotorTorque;
        }
        else
            Debug.Log("This Drive Type not exist.");
    }

    public void Acceleration()
    {
        if (isStarted && isEngineOn)
        {
            if (gearType == GearType.Automatic)
            {
                if (automaticGear == AutomaticGears.Drive)
                {
                    // Calculate how close the car is to top speed
                    // as a number from zero to one
                    speedFactor = Mathf.InverseLerp(0, maxForwardSpeed, currentSpeed);

                    // Use that to calculate how much torque is available 
                    // (zero torque at top speed)
                    currentMotorTorque = Mathf.Lerp(horsePower, 0, speedFactor);

                    // Define if acceleration should be positive or negative
                    currentAccelerationValue *= 1;
                }
                else if (automaticGear == AutomaticGears.Reverse)
                {
                    currentMotorTorque = horsePower;
                    currentAccelerationValue *= -1;
                }

                if ((currentAccelerationValue > 0f && currentSpeed < maxForwardSpeed) || (currentAccelerationValue > 0f && currentSpeed > -maxReverseSpeed))
                {
                    EvaluateDriveType(currentMotorTorque * currentAccelerationValue);
                }
                else
                {
                    frontLeftWheelCollider.motorTorque = 0;
                    frontRightWheelCollider.motorTorque = 0;
                    rearLeftWheelCollider.motorTorque = 0;
                    rearRightWheelCollider.motorTorque = 0;
                }
            }
            else if (gearType == GearType.Manual)
            {
                switch (manualGear)
                {
                    case ManualGears.First:

                        break;

                    case ManualGears.Second:

                        break;

                    case ManualGears.Third:

                        break;

                    case ManualGears.Fourth:

                        break;

                    case ManualGears.Fifth:

                        break;

                    case ManualGears.Sixth:

                        break;

                    case ManualGears.Reverse:
                        currentMotorTorque = horsePower;
                        currentAccelerationValue *= -1;
                        break;

                    default :
                        frontLeftWheelCollider.motorTorque = 0;
                        frontRightWheelCollider.motorTorque = 0;
                        rearLeftWheelCollider.motorTorque = 0;
                        rearRightWheelCollider.motorTorque = 0;
                        break;
                }
            }
        }
    }

    public void ChangeManualGear()
    {
        if(inputType == InputType.Keyboard)
        {
            if(actualGear < maxGear && actualGear > minGear)
            {
                if (Input.GetKeyUp(KeyCode.LeftShift))
                {
                    actualGear = actualGear + 1;
                }
                else if (Input.GetKeyUp(KeyCode.LeftControl))
                {
                    actualGear = actualGear - 1;
                }

                switch (actualGear)
                {
                    case -1:
                        manualGear = ManualGears.Reverse;
                        break;

                    case -0:
                        manualGear = ManualGears.Neutral;
                        break;

                    case 1:
                        manualGear = ManualGears.First;
                        break;

                    case 2:
                        manualGear = ManualGears.Second;
                        break;

                    case 3:
                        manualGear = ManualGears.Third;
                        break;

                    case 4:
                        manualGear = ManualGears.Fourth;
                        break;

                    case 5:
                        manualGear = ManualGears.Fifth;
                        break;

                    case 6:
                        manualGear = ManualGears.Sixth;
                        break;
                }
            }
        }
        else if (inputType == InputType.SteeringWheel)
        {

        }
    }

    public void CalculateEngineRPM()
    {
        engineLimiterRPM = engineMaxRPM - 500f;
        var torque = horsePower / engineMaxRPM;

        if (inputType == InputType.Keyboard)
        {
            if (Input.GetKey("W"))
            {
                if(engineCurrentRPM < engineLimiterRPM)
                    engineCurrentRPM = engineCurrentRPM + Mathf.Abs(currentAccelerationValue) * torque;
                else if (engineCurrentRPM > engineLimiterRPM && engineCurrentRPM != engineLimiterRPM)
                    engineCurrentRPM = engineCurrentRPM - 50f;
            }
            else
            {
                if (engineCurrentRPM >= engineIdleRPM)
                    engineCurrentRPM -= engineMaxRPM * torque;
                else
                    engineCurrentRPM = engineIdleRPM;
            }
        }
    }

}
