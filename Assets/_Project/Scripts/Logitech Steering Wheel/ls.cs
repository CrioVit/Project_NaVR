using Sirenix.OdinInspector;
using UnityEngine;
using static UnityEngine.LightTransport.InputExtraction;

public class ls : MonoBehaviour
{
    //////////////////////////////////////////////////////////////////////////////
    ///
    private LogitechGSDK.LogiControllerPropertiesData properties;
    private LogitechGSDK.DIJOYSTATE2ENGINES steeringWheelInput;

    // Steering Wheel Settings
    public int maxWheelRotation = 900;
    [Range(0, 100)] public int maxWheelGain = 100;

    // Input
    public bool[] buttonPressed = new bool[128];
    public bool[] buttonDown = new bool[128];
    public bool[] buttonPreviouslyPressed = new bool[128];

    [SerializeField, Range(-1, 1)] private float steeringValue;
    [SerializeField, Range(0, 1)] private float throttleValue;
    [SerializeField, Range(0, 1)] private float brakeValue;
    [SerializeField, Range(0, 1)] private float clutchValue;

    [SerializeField] private bool upShiftInput = false;
    [SerializeField] private bool downShiftInput = false;
    public int upShiftButton = 12;
    public int downShiftButton = 13;
    public int alternativeUpShiftButton = 4;
    public int alternativeDownShiftButton = 5;

    //////////////////////////////////////////////////////////////////////////////

    void Start()
    {
        Debug.Log("SteeringInit:" + LogitechGSDK.LogiSteeringInitialize(false));
        UpdateProperties();

        buttonPressed = new bool[128];
        buttonDown = new bool[128];
        buttonPreviouslyPressed = new bool[128];
    }

    void Update()
    {
        if (LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            UpdateProperties();
            GetSteeringWheelInputs();
        }
        else
            print("Steering Wheel Not Connected!");
    }

    private void FixedUpdate()
    {

    }

    //////////////////////////////////////////////////////////////////////////////

    // Input
    float ResolveAxisValue(int value, bool zeroToOne, bool flip)
    {
        float halfMaxValue = 65536 / 2f;

        if (zeroToOne)
        {
            if(flip)
                return -((value - halfMaxValue) / 65536);
            else
                return (value - halfMaxValue) / 65536;
        }
        else
        {
            if (flip)
                return -(value / halfMaxValue);
            else
                return value / halfMaxValue;
        }
    }

    void GetSteeringWheelInputs()
    {
        steeringWheelInput = LogitechGSDK.LogiGetStateUnity(0);

        // Buttons
        for (int i = 0; i < 128; i++)
        {
            buttonPreviouslyPressed[i] = buttonPressed[i];
            buttonPressed[i] = steeringWheelInput.rgbButtons[i] == 128;
            
            buttonDown[i] = !buttonPreviouslyPressed[i] && buttonPressed[i];
        }
        
        //Throttle
        throttleValue = ResolveAxisValue(steeringWheelInput.lY, true, true);

        //Brake
        brakeValue = ResolveAxisValue(steeringWheelInput.lRz, true, true);

        //Steering
        steeringValue = ResolveAxisValue(steeringWheelInput.lX, false, false);

        //Clutch
        clutchValue = ResolveAxisValue(steeringWheelInput.lZ, true, true);

        // Shift Up
        upShiftInput = GetButtonDown(upShiftButton) || GetButtonDown(alternativeUpShiftButton);
        // Shift Down
        downShiftInput = GetButtonDown(downShiftButton) || GetButtonDown(alternativeDownShiftButton);
    }

    bool GetButtonPressed(int buttonIndex)
    {
        if (buttonIndex < 0)
            return false;

        return buttonPressed[buttonIndex];
    }

    bool GetButtonDown(int buttonIndex)
    {
        if (buttonIndex < 0)
            return false;

        return buttonDown[buttonIndex];
    }

    // Controller properties
    void UpdateProperties()
    {
        LogitechGSDK.LogiControllerPropertiesData currentProperties = new LogitechGSDK.LogiControllerPropertiesData();
        LogitechGSDK.LogiGetCurrentControllerProperties(0, ref currentProperties);

        currentProperties.wheelRange = maxWheelRotation;
        currentProperties.forceEnable = true;
        currentProperties.overallGain = (int)(maxWheelGain * 100); ;
        currentProperties.springGain = 80;
        currentProperties.damperGain = 80;
        currentProperties.allowGameSettings = true;
        currentProperties.combinePedals = false;
        currentProperties.defaultSpringEnabled = true;
        currentProperties.defaultSpringGain = 80;
        currentProperties.gameSettingsEnabled = true;

        LogitechGSDK.LogiSetPreferredControllerProperties(properties);
    }

    // Forces
    void PlayConstantForce(float force)
    {
        LogitechGSDK.LogiPlayConstantForce(0, (int)force);
    }

    void StopConstantForce()
    {
        LogitechGSDK.LogiStopConstantForce(0);
    }

    // Public Functions
    public float GetThrottleAmount()
    {
        return throttleValue;
    }

    public float GetBrakeAmount()
    {
        return brakeValue;
    }

    public float GetSteeringAmount()
    {
        return steeringValue;
    }

    public float GetClutch()
    {
        return clutchValue;
    }

    public bool UpShift()
    {
        return upShiftInput;
    }

    public bool DownShift()
    {
        return downShiftInput;
    }

    // Quit
    void OnApplicationQuit()
    {
        Debug.Log("SteeringShutdown:" + LogitechGSDK.LogiSteeringShutdown());
    }
}
