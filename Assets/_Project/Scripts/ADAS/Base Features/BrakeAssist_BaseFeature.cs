using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[Serializable]
[CreateAssetMenu(fileName = "BrakeAssist_BaseFeature", menuName = "Scriptable Objects/BrakeAssist")]
public class BrakeAssist_BaseFeature : ADAS_BaseFeature, IInitializable, IActivableByInputAction, ICarController, ICarFrontSensorConsumer
{
    [Serializable]
    public class BrakeAssist_Settings : ADAS_Settings
    {
        public float SecurityDistance;
        public float securityDistanceFactor;
        public float ObstacleDetectionRange;
        public float MinSpeed;
    }
    [FoldoutGroup("Brake Assist Settings"), HideLabel] public BrakeAssist_Settings BrakeAssistSettings;

    [FoldoutGroup("Debug"), ShowInInspector] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isOn;
    [FoldoutGroup("Brake Assist Settings"), SerializeField] private ICarController.CarControlTypeEnum _controlType;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;
    private ICarFrontSensorProvider _carFrontSensorProvider;

    // IInitializable
    public bool IsInitialized { get => _isInitialized; set => _isInitialized = value; }

    // IActivableByInputAction
    public InputActionAsset InputActionAsset { get => _inputActionAsset; set => _inputActionAsset = value; }
    public InputAction ActivationAction { get => _activationAction; set => _activationAction = value; }
    [FoldoutGroup("Input Actions"), ShowInInspector, ReadOnly] public List<string> Bindings => ((IActivableByInputAction)this).GetBindings();
    public bool IsOn { get => _isOn; set => _isOn = value; }

    // ICarController
    public ICarControllerProvider CarControllerProvider { get => _carControllerProvider; set => _carControllerProvider = value; }
    public ICarControllerExecutor CarControllerExecutor { get => _carControllerExecutor; set => _carControllerExecutor = value; }
    public ICarController.CarControlTypeEnum CarControlType { get => _controlType; set => _controlType = value; }

    // ICarFrontSensorConsumer
    public ICarFrontSensorProvider CarFrontSensorProvider { get => _carFrontSensorProvider; set => _carFrontSensorProvider = value; }

    // Obstacle Detection
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleFL;
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleFC;
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleFR;
    public Vector3 RayDirectionForward => CarControllerProvider.car.rb.transform.forward;

    private RaycastHit FrontLeftHitInfo;
    private RaycastHit FrontCentralHitInfo;
    private RaycastHit FrontRightHitInfo;

    [FoldoutGroup("Obstacle Detection"), SerializeField] private float distanceToFO;
    [FoldoutGroup("Obstacle Detection")] public int storedMaxSpeed;


    public BrakeAssist_BaseFeature()
    {
        Name = "Brake Assist";
        Description = "Braking technology that increases braking pressure in an emergency";
    }

    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
            BrakeAssistSettings.SecurityDistance = 0;
            distanceToFO = 0;
            _isOn = false;
            _carControllerProvider = null;
            _carControllerExecutor = null;
        }
    }

    #region Input Action
    public void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = InputActionAsset.FindActionMap("CLifeCarController");

        ActivationAction = actionMap.FindAction("BrakeAssistActivation");
        
        // Register Callback Events
        ActivationAction.performed += Activation_action;

        // Enable Action
        ActivationAction.Enable();
    }

    public void OnDisable()
    {
        //Register Callback Events
        ActivationAction.performed -= Activation_action;

        // Enable Action
        ActivationAction.Disable();
    }

    public void Activation_action(InputAction.CallbackContext obj)
    {
        if (IsOn)
            TurnOff();
        else
            TurnOn();
    }

    public bool TurnOn()
    {
        if (IsOn)
            return true;

        storedMaxSpeed = Mathf.RoundToInt(CarControllerProvider.car.storedMaxForwardSpeed);
        //storedMaxSpeed = Mathf.RoundToInt(CarControllerProvider.CC.maxForwardSpeed);

        return IsOn = true;
    }

    public bool TurnOff()
    {
        if (!IsOn)
            return false;

        CarControllerProvider.car.maxForwardSpeed = storedMaxSpeed;

        if(!CarControllerProvider.car.brakeAction.enabled)
            CarControllerProvider.car.brakeAction.Enable();
        if (!CarControllerProvider.car.throttleAction.enabled)
            CarControllerProvider.car.throttleAction.Enable();

        if (!CarControllerProvider.car.brakeAction.inProgress)
            CarControllerProvider.car.Brake(0);

        return IsOn = false;
    }
    #endregion

    public void ExecuteCarControl()
    {
        if (IsOn)
        {
            if (CarControllerProvider.car.currentSpeed > 0.1f)
            {
                if (DetectingObstacleForward())
                {
                    // Distance to forward obstacle
                    distanceToFO = DistanceToForwardObstacle();

                    // Security distance Formula
                    //if(BrakeAssistSettings.SecurityDistance == 0)
                    //    BrakeAssistSettings.SecurityDistance = CalculateSecurityDistance();  // Forse conviene calcolarla costantemente

                    BrakeAssistSettings.SecurityDistance = CalculateSecurityDistance();

                    if (distanceToFO < BrakeAssistSettings.SecurityDistance)
                    {
                        CarControllerProvider.car.maxForwardSpeed = 0;
                        
                        CarControllerProvider.car.throttleAction.Disable();
                        CarControllerProvider.car.Throttle(0);
                        CarControllerProvider.car.Brake(1);
                    }
                    else
                    {
                        CarControllerProvider.car.maxForwardSpeed = storedMaxSpeed;
                        if(!CarControllerProvider.car.brakeAction.inProgress)
                            CarControllerProvider.car.Brake(0);
                        CarControllerProvider.car.throttleAction.Enable();
                    }
                }
            }
            else
            {
                CarControllerProvider.car.maxForwardSpeed = storedMaxSpeed;
                if (!CarControllerProvider.car.brakeAction.inProgress)
                    CarControllerProvider.car.Brake(0);
                CarControllerProvider.car.throttleAction.Enable();

                TurnOff();
            }
        }
    }

    public float DistanceToForwardObstacle()
    {
        var FL_Distance = FrontLeftHitInfo.distance;
        var FC_Distance = FrontCentralHitInfo.distance;
        var FR_Distance = FrontRightHitInfo.distance;

        if (FL_Distance > 0 && FC_Distance <= 0 && FR_Distance <= 0)
            return FL_Distance;
        if (FL_Distance <= 0 && FC_Distance > 0 && FR_Distance <= 0)
            return FC_Distance;
        if (FL_Distance <= 0 && FC_Distance <= 0 && FR_Distance > 0)
            return FR_Distance;

        if (FL_Distance > 0 && FC_Distance > 0 && FR_Distance <= 0)
            return Mathf.Min(FL_Distance, FC_Distance);
        if (FL_Distance > 0 && FC_Distance <= 0 && FR_Distance > 0)
            return Mathf.Min(FL_Distance, FR_Distance);
        if (FL_Distance <= 0 && FC_Distance > 0 && FR_Distance > 0)
            return Mathf.Min(FC_Distance, FR_Distance);

        else
            return Mathf.Min(FL_Distance, FC_Distance, FR_Distance);
    }

    public bool DetectingObstacleForward()
    {
        var FrontLeftRayStartingPointPosition = CarFrontSensorProvider.FrontSensorData.FrontLeftRayStartingPoint.position;
        var FrontCentralRayStartingPointPosition = CarFrontSensorProvider.FrontSensorData.FrontCentralRayStartingPoint.position;
        var FrontRightRayStartingPointPosition = CarFrontSensorProvider.FrontSensorData.FrontRightRayStartingPoint.position;

        LayerMask mask = LayerMask.GetMask("Vehicle");

        // Detecting Obstacle
        _DetectingObstacleFL = Physics.Raycast(FrontLeftRayStartingPointPosition, RayDirectionForward, out FrontLeftHitInfo, BrakeAssistSettings.ObstacleDetectionRange, mask);
        _DetectingObstacleFC = Physics.Raycast(FrontCentralRayStartingPointPosition, RayDirectionForward, out FrontCentralHitInfo, BrakeAssistSettings.ObstacleDetectionRange, mask);
        _DetectingObstacleFR = Physics.Raycast(FrontRightRayStartingPointPosition, RayDirectionForward, out FrontRightHitInfo, BrakeAssistSettings.ObstacleDetectionRange, mask);

        // Draw Rays
        Debug.DrawRay(FrontLeftRayStartingPointPosition, RayDirectionForward * BrakeAssistSettings.ObstacleDetectionRange, _DetectingObstacleFL ? Color.red : Color.green);
        Debug.DrawRay(FrontCentralRayStartingPointPosition, RayDirectionForward * BrakeAssistSettings.ObstacleDetectionRange, _DetectingObstacleFC ? Color.red : Color.green);
        Debug.DrawRay(FrontRightRayStartingPointPosition, RayDirectionForward * BrakeAssistSettings.ObstacleDetectionRange, _DetectingObstacleFR ? Color.red : Color.green);

        if (_DetectingObstacleFL || _DetectingObstacleFC || _DetectingObstacleFR)
            return true;

        return false;
    }

    public float CalculateSecurityDistance()
    {
        return Mathf.Pow((CarControllerProvider.car.currentSpeed / 10), BrakeAssistSettings.securityDistanceFactor);
    }
    
}