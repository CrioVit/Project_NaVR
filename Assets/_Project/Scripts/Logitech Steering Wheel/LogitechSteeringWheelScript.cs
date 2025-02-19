using Sirenix.OdinInspector;
using UnityEngine;

public class LogitechSteeringWheelScript : MonoBehaviour
{
    [SerializeField] private Car CC;
    //[SerializeField] private MyCar Car;

    // Steering Wheel
    [FoldoutGroup("Logitech Debug")] public float steeringWheel_xAxis; // Steering Wheel Movement, values from -32767 (left) to 32767 (right) (at 0 is centered)
    [FoldoutGroup("Logitech Debug")] public int x_button0; // X
    [FoldoutGroup("Logitech Debug")] public int square_button1; // Square
    [FoldoutGroup("Logitech Debug")] public int circle_button2; // Circle
    [FoldoutGroup("Logitech Debug")] public int triangle_button3; // Triangle
    //[FoldoutGroup("Logitech Debug")] public int button4; // Left clutch lever
    //[FoldoutGroup("Logitech Debug")] public int button5; // Right clutch lever
    [FoldoutGroup("Logitech Debug")] public int r2_button6; // R2
    [FoldoutGroup("Logitech Debug")] public int plus_button19; // Plus
    [FoldoutGroup("Logitech Debug")] public int minus_button20; // Minus

    // Pedal Board
    [FoldoutGroup("Logitech Debug")] public float acceleratorPedal_yAxis; // Accelerator, value from 32767 (when not pressed) to 0 (when fully pressed)
    [FoldoutGroup("Logitech Debug")] public float brakePedal_zAxisRotation; // Brake, value from 32767 (when not pressed) to 0 (when fully pressed)
                                                                            //public float extraAxes1; // Clutch

    #region Generics Variables
    //Steering Wheel
    public int maxLogitechSteeringWheelAngle = 900;
    public int steeringWheelAngle;

    //Gas
    public float GasInput;

    //Brake
    public float BrakeInput;
    public float storedBrakeForce;

    //Force Feedback
    #region Damper Force
    [Range(0, 100)] public float StationaryDamperForce = 60f;
    [Range(0, 100)] public float MovingDamperForce = 15f;
    [Range(0, 10)] public float DamperForceVelocityCoefficient = 5f;
    private float DamperForce;
    #endregion

    //Collisions
    private Rigidbody rb;
    private ContactPoint[] ContactPoints;
    private int ContactCount;
    private Vector3 LocalContactPoint;

    private float RightForce;
    private float LeftForce;
    private float FrontalForce;
    private float RearForce;

    [Range(0, 5)] public float SideThreshold = 0.5f; // Distance through X-Axis
    [Range(0, 5)] public float FrontBackThreshold = 0.5f; // Distance through Z-Axis

    //Reverse
    public bool ReverseOn;
    #endregion

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("SteeringInit:" + LogitechGSDK.LogiSteeringInitialize(false));

        CC = GetComponent<Car>();
        //Car = GetComponent<MyCar>();

        rb = GetComponent<Rigidbody>();

        storedBrakeForce = CC.brakeForce;
    }

    // Update is called once per frame
    void Update()
    {
        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            LogitechGSDK.DIJOYSTATE2ENGINES rec = LogitechGSDK.LogiGetStateUnity(0);

            #region Buttons Settings
            steeringWheel_xAxis = rec.lX;
            x_button0 = rec.rgbButtons[0];
            square_button1 = rec.rgbButtons[1];
            circle_button2 = rec.rgbButtons[2];
            triangle_button3 = rec.rgbButtons[3];
            r2_button6 = rec.rgbButtons[6];
            plus_button19 = rec.rgbButtons[19];
            minus_button20 = rec.rgbButtons[20];

            //Pedal Board
            acceleratorPedal_yAxis = rec.lY;
            brakePedal_zAxisRotation = rec.lRz;
            #endregion

            if (LogitechGSDK.LogiButtonReleased(0, 6))
                ChangeTravelDirection();
        }
        else
            print("Steering Wheel Not Connected!");
    }

    private void FixedUpdate()
    {
        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            #region Movement Functions
            Accelerate(acceleratorPedal_yAxis);
            Brake(brakePedal_zAxisRotation);
            Turn(steeringWheel_xAxis);

            PlayLeds();

            ForceFeedback();
            #endregion

            PlayDamperForce();
        }
    }
    #region Moving Functions

    public void Accelerate(float yAxis)
    {
        if (yAxis == 32767f)
        {
            GasInput = 0f;
            //CC.throttleAmount = GasInput;
            //CC.ApplyEngineBrakeDeceleration();
        }
        else
        {
            if (yAxis == -32767f)
                GasInput = 1f;
            if (yAxis >= 0f)
                GasInput = Mathf.Lerp(0.5f, 0f, yAxis / 32767f);
            else
                GasInput = Mathf.Lerp(0.5f, 1f, -yAxis / 32767f);

            //CC.throttleAmount = GasInput;
            //CC.ApplyVehicleAcceleration();
        }
    }

    public void Brake(float zAxis)
    {
        if (zAxis == 32767f)
        {
            BrakeInput = 0f;
            CC.brakeAmount = BrakeInput;
        }
        else
        {
            if (zAxis == -32767f)
                BrakeInput = 1f;
            if (zAxis >= 0f)
                BrakeInput = Mathf.Lerp(storedBrakeForce / 2, 0f, zAxis / 32767f);
            else
                BrakeInput = Mathf.Lerp(storedBrakeForce / 2, storedBrakeForce, -zAxis / 32767f);

            //CC.brakeAmount = BrakeInput;
            //CC.ApplyBrakeForce();
        }
    }

    public void ChangeTravelDirection()
    {
        ReverseOn = !ReverseOn;

        if (ReverseOn)
        {
            CC.currentGear = -1;
            CC.automaticGear = Car.AutomaticGear.Reverse;
        }
        else
        {
            CC.currentGear = 1;
            CC.automaticGear = Car.AutomaticGear.Drive;
        }

        Debug.Log("Reverse Mode: " + ReverseOn);
    }

    public void Turn(float xAxis)
    {
        var previousSteeringWheel_xAxis = steeringWheel_xAxis;
        CC.steeringAmount = xAxis / 32767;
        //CC.ApplySteering();
    }

    #region Visual Moving Functions
    public void PlayLeds()
    {
        LogitechGSDK.LogiPlayLeds(0, CC.currentSpeed, 10, 100);
    }
    #endregion

    #endregion

    #region Force Feedback Functions
    public void ForceFeedback()
    {
        #region Forces Functions

        #region Spring Force:
        /*
            1: bool LogiPlaySpringForce(const int index, const int offsetPercentage, const int saturationPercentage, const int coefficientPercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> offsetPercentage: Specifies the center of the spring force effect. Valid range is -100 to 100. Specifying 0 centers the spring. 
                                     Any values outside this range are silently clamped.
                -> saturationPercentage: Specify the level of saturation of the spring force effect. The saturation stays constant after a certain 
                                         deflection from the center of the spring. It is comparable to a magnitude. Valid ranges are 0 to 100. 
                                         Any value higher than 100 is silently clamped.
                -> coefficientPercentage: Specify the slope of the effect strength increase relative to the amount of deflection from the center of the 
                                          condition. Higher values mean that the saturation level is reached sooner. Valid ranges are -100 to 100. 
                                          Any value outside the valid range is silently clamped.

            2: bool LogiStopSpringForce(const int index);
        */
        #endregion

        #region Constant Force: 
        /*
            1: bool LogiPlayConstantForce(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: Specifies the magnitude of the constant force effect. A negative value reverses the direction of the force. 
                                        Valid ranges for magnitudePercentage are -100 to 100. Any values outside the valid range are silently clamped.

            2: bool LogiStopConstantForce(const int index);
        */
        #endregion

        #region Damper Force: 
        /*
            1: bool LogiPlayDamperForce(const int index, const int coefficientPercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> coefficientPercentage : specify the slope of the effect strength increase relative to the amount of deflection from the center of the condition. 
                                           Higher values mean that the saturation level is reached sooner. Valid ranges are -100 to 100.
                                           Any value outside the valid range is silently clamped. -100 simulates a very slippery effect, +100 makes the wheel/joystick 
                                           very hard to move, simulating the car at a stop or in mud.

            2: bool LogiStopDamperForce(const int index);
        */
        #endregion

        #region Side Collision Force: 
        /*
            1: bool LogiPlaySideCollisionForce(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: Specifies the magnitude of the constant force effect. A negative value reverses the direction of the force. 
                                        Valid ranges for magnitudePercentage are -100 to 100. Any values outside the valid range are silently clamped.

            2: bool LogiStopSideCollisionForce(const int index);
        */
        #endregion

        #region Frontal Collision Force: 
        /*
            1: bool LogiPlayFrontalCollisionForce(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: specifies the magnitude of the frontal collision force effect. Valid ranges
                                        for magnitudePercentage are 0 to 100. Values higher than 100 are silently clamped.


            2: bool LogiStopFrontalCollisionForce(const int index);
        */
        #endregion

        #region Dirty Road Effect: 
        /*
            1: bool LogiPlayDirtRoadEffect(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: Specifies the magnitude of the dirt road effect. Valid ranges for
                                        magnitudePercentage are 0 to 100. Values higher than 100 are silently clamped.

            2: bool LogiStopDirtRoadEffect(const int index);
        */
        #endregion

        #region Bumpy Road Effect: 
        /*
            1: bool LogiPlayBumpyRoadEffect(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: Specifies the magnitude of the bumpy road effect. Valid ranges for
                                        magnitudePercentage are 0 to 100. Values higher than 100 are silently clamped.

            2: bool LogiStopBumpyRoadEffect (const int index);
        */
        #endregion

        #region Slippery Road Effect: 
        /*
            1: bool LogiPlayBumpyRoadEffect(const int index, const int magnitudePercentage);

                -> index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                -> magnitudePercentage: Specifies the magnitude of the slippery road effect. Valid ranges for
                                        magnitudePercentage are 0 to 100. 100 corresponds to the most slippery effect.

            2: bool LogiStopSlipperyRoadEffect (const int index);
        */
        #endregion

        #region Surface Effect: 
        /*
            1: bool LogiPlaySurfaceEffect(const int index, const int type, const int magnitudePercentage, const int period);

                ->  index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                ->  type: Specifies the type of force effect. Can be one of the following values:
                          o LOGI_PERIODICTYPE_SINE
                          o LOGI_PERIODICTYPE_SQUARE
                          o LOGI_PERIODICTYPE_TRIANGLE
                -> magnitudePercentage: Specifies the magnitude of the surface effect. Valid ranges for magnitudePercentage are 0 to 100. 
                                        Values higher than 100 are silently clamped.
                -> period: Specifies the period of the periodic force effect. The value is the duration for one full cycle of the periodic 
                           function measured in milliseconds. A good range of values for the period is 20 ms (sand) to 120 ms 
                           (wooden bridge or cobblestones). For a surface effect the period should not be any bigger than 150 ms

            2: bool LogiStopSurfaceEffect (const int index);
        */
        #endregion

        #region Car Airborne: 
        /*
            1: bool LogiPlayCarAirborne(const int index);

                ->  index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.

            2: bool LogiStopCarAirborne(const int index);
        */
        #endregion

        #region Softstop Force: 
        /*
            1: bool LogiPlaySoftstopForce(const int index, const int usableRangePercentage);

                ->  index: index of the game controller. Index 0 corresponds to the first game controller connected. Index 1 to the second game controller.
                ->  usableRangePercentage : specifies the deadband in percentage of the softstop force effect.

            2: bool LogiStopSoftstopForce(const int index);
        */
        #endregion

        #endregion
    }

    public void PlayDamperForce()
    {
        DamperForce = Mathf.Lerp(StationaryDamperForce, MovingDamperForce, CC.currentSpeed * DamperForceVelocityCoefficient * 0.01f);

        LogitechGSDK.LogiPlayDamperForce(0, (int)DamperForce);
    }

    //public void PlayCollisionForces(Collision collision)
    //{
    //    ContactPoints = new ContactPoint[collision.contactCount];
    //    ContactCount = collision.GetContacts(ContactPoints);

    //    RightForce = 0f;
    //    LeftForce = 0f;
    //    FrontalForce = 0f;
    //    RearForce = 0f;

    //    for (int i = 0; i < ContactCount; i++)
    //    {
    //        LocalContactPoint = transform.InverseTransformPoint(ContactPoints[i].point);

    //        if (LocalContactPoint.x > 0)
    //        {
    //            //Collision on Right Side
    //            RightForce = Mathf.Max(RightForce, Mathf.Lerp(0f, 100f, (ContactPoints[i].impulse.magnitude / Time.fixedDeltaTime) * 0.001f));
    //        }
    //        if (LocalContactPoint.x < 0)
    //        {
    //            //Collision on Left Side
    //            LeftForce = Mathf.Min(LeftForce, Mathf.Lerp(0f, -100f, (ContactPoints[i].impulse.magnitude / Time.fixedDeltaTime) * 0.001f));
    //        }
    //        if (LocalContactPoint.z > 0)
    //        {
    //            //Collision on Frontal Side
    //            FrontalForce = Mathf.Max(FrontalForce, Mathf.Lerp(0f, 100f, (ContactPoints[i].impulse.magnitude / Time.fixedDeltaTime) * 0.001f));
    //        }
    //        if (LocalContactPoint.z < 0)
    //        {
    //            //Collision on Rear Side
    //            RearForce = Mathf.Max(RearForce, Mathf.Lerp(0f, 100f, (ContactPoints[i].impulse.magnitude / Time.fixedDeltaTime) * 0.001f));
    //        }
    //    }

    //    #region Play Forces

    //    Debug.Log("Right Force: " + RightForce);
    //    Debug.Log("Left Force: " + LeftForce);
    //    Debug.Log("Frontal Force: " + FrontalForce);
    //    Debug.Log("Rear Force: " + RearForce);

    //    if (RightForce > 0)
    //    {
    //        Debug.Log("Right Force: " + RightForce);
    //        LogitechGSDK.LogiPlaySideCollisionForce(0, (int)RightForce);
    //    }
    //    if (LeftForce < 0)
    //    {
    //        Debug.Log("Left Force: " + LeftForce);
    //        LogitechGSDK.LogiPlaySideCollisionForce(0, (int)LeftForce);
    //    }
    //    if (FrontalForce > 0)
    //    {
    //        Debug.Log("Frontal Force: " + FrontalForce);
    //        LogitechGSDK.LogiPlayFrontalCollisionForce(0, (int)FrontalForce);
    //    }
    //    if (RearForce > 0)
    //    {
    //        Debug.Log("Rear Force: " + RearForce);
    //        LogitechGSDK.LogiPlayFrontalCollisionForce(0, (int)RearForce);
    //    }

    //    #endregion
    //}

    public void PlayCollisionForces(Collision collision)
    {
        ContactPoints = new ContactPoint[collision.contactCount];
        ContactCount = collision.GetContacts(ContactPoints);

        RightForce = 0f;
        LeftForce = 0f;
        FrontalForce = 0f;
        RearForce = 0f;

        for (int i = 0; i < ContactCount; i++)
        {
            LocalContactPoint = transform.InverseTransformPoint(ContactPoints[i].point);
            float normalizedImpulse = Mathf.Clamp(ContactPoints[i].impulse.magnitude / Time.fixedDeltaTime, 0f, 1000f);

            if (LocalContactPoint.x > 0)
            {
                // Collisione lato destro
                RightForce = Mathf.Max(RightForce, Mathf.Lerp(0f, 100f, normalizedImpulse * 0.001f));
            }
            else if (LocalContactPoint.x < 0)
            {
                // Collisione lato sinistro
                LeftForce = Mathf.Min(LeftForce, Mathf.Lerp(0f, -100f, normalizedImpulse * 0.001f));
            }
            if (LocalContactPoint.z > 0)
            {
                // Collisione frontale
                FrontalForce = Mathf.Max(FrontalForce, Mathf.Lerp(0f, 100f, normalizedImpulse * 0.001f));
            }
            else if (LocalContactPoint.z < 0)
            {
                // Collisione posteriore
                RearForce = Mathf.Max(RearForce, Mathf.Lerp(0f, 100f, normalizedImpulse * 0.001f));
            }
        }

        #region Play Forces
        if (RightForce > 0)
        {
            Debug.Log("Right Force: " + RightForce);
            LogitechGSDK.LogiPlaySideCollisionForce(0, (int)RightForce);
        }
        if (LeftForce < 0)
        {
            Debug.Log("Left Force: " + LeftForce);
            LogitechGSDK.LogiPlaySideCollisionForce(0, (int)LeftForce);
        }
        if (FrontalForce > 0)
        {
            Debug.Log("Frontal Force: " + FrontalForce);
            LogitechGSDK.LogiPlayFrontalCollisionForce(0, (int)FrontalForce);
        }
        if (RearForce > 0)
        {
            Debug.Log("Rear Force: " + RearForce);
            LogitechGSDK.LogiPlayFrontalCollisionForce(0, (int)RearForce);
        }
        #endregion
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        PlayCollisionForces(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        PlayCollisionForces(collision);
    }

    void OnApplicationQuit()
    {
        Debug.Log("SteeringShutdown:" + LogitechGSDK.LogiSteeringShutdown());
    }
}
