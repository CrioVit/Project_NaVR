using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "LaneAssist", menuName = "Scriptable Objects/LaneAssist")]
public class LaneAssist_MajorFeature : ADAS_MajorFeature, IInitializable ,IActivableByInputAction, ICarController
{
    [Serializable]
    public class LaneAssist_Settings : ADAS_Settings
    {
        public float SteeringSpeed;
    }
    [FoldoutGroup("Lane Assist Settings"), HideLabel] public LaneAssist_Settings LaneAssistSettings;

    [FoldoutGroup("ADAS: Base Features"), ShowInInspector] public LaneDetection_BaseFeature LaneDetectionBaseFeature => (LaneDetection_BaseFeature)BaseFeaturesList.FirstOrDefault((f) => f is LaneDetection_BaseFeature feature);
    [FoldoutGroup("ADAS: Base Features"), ShowInInspector] public CruiseControl_BaseFeature CruiseControlBaseFeature => (CruiseControl_BaseFeature)BaseFeaturesList.FirstOrDefault((f) => f is CruiseControl_BaseFeature feature);


    // Interfaces Variables
    [FoldoutGroup("Debug"), SerializeField] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), SerializeField] private bool _isOn;
    [FoldoutGroup("Lane Assist Settings"), SerializeField] private ICarController.CarControlTypeEnum _controlType;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;

    // IInitializable
    public bool IsInitialized { get => _isInitialized; set => _isInitialized = value; }

    // IActivableByInputAction
    public InputActionAsset InputActionAsset { get => _inputActionAsset; set => _inputActionAsset = value; }
    public InputAction ActivationAction { get => _activationAction; set => _activationAction = value; }
    [FoldoutGroup("Input Actions"), ShowInInspector, ReadOnly] public List<string> Bindings => ((IActivableByInputAction)this).GetBindings();
    public bool IsOn { get => _isOn; set => _isOn = value; }

    // ICarController
    public ICarController.CarControlTypeEnum CarControlType { get => _controlType; set => _controlType = value; }
    public ICarControllerProvider CarControllerProvider { get => _carControllerProvider; set => _carControllerProvider = value; }
    public ICarControllerExecutor CarControllerExecutor { get => _carControllerExecutor; set => _carControllerExecutor = value; }

    // Lane Assist Variables
    [SerializeField, FoldoutGroup("Debug")] private float StoredSteeringSpeed;

    public LaneAssist_MajorFeature()
    {
        Name = "Lane Assist";
        Description = "Lane Assist is an advanced driver-assistance system that keeps a road vehicle centered in the lane, relieving the driver of the task of steering.";
    }
    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
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

        ActivationAction = actionMap.FindAction("LaneAssistActivation");

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

        if (LaneDetectionBaseFeature.TurnOn())
        {
            StoredSteeringSpeed = CarControllerProvider.car.steeringSpeed;
            IsOn = true;
        }
        else
            IsOn = false;

        return IsOn;
    }

    public bool TurnOff()
    {
        if (!IsOn)
            return false;

        if (!LaneDetectionBaseFeature.TurnOff())
        {
            CarControllerProvider.car.steeringSpeed = StoredSteeringSpeed;
            IsOn = false;
        }
        else
            IsOn = true;

        return IsOn;
    }
    #endregion

    public void ExecuteCarControl()
    {
        if (!IsOn || !CarControllerProvider.car.isEngineOn) return;

        float currentSpeed = CarControllerProvider.car.currentSpeed;
        float incidentAngle = LaneDetectionBaseFeature.CalculateAngle();

        if (Input.anyKey || currentSpeed <= 0)
        {
            ResetSteering();
            return;
        }
          
        bool isLeftObstacle = LaneDetectionBaseFeature._DetectingObstacleBFL && incidentAngle < 0f;
        bool isRightObstacle = LaneDetectionBaseFeature._DetectingObstacleBFR && incidentAngle > 0f;

        if (isLeftObstacle || isRightObstacle)
        {
            float angleFactor = Mathf.Abs(Mathf.Clamp(incidentAngle, 0, 45)) / 45f;
            float speedFactor = currentSpeed / (CruiseControlBaseFeature.IsOn ? CruiseControlBaseFeature.storedMaxSpeed : CarControllerProvider.car.maxForwardSpeed);
            float steeringSpeed = Mathf.Clamp(Mathf.Lerp(0.1f, 1f, Mathf.Clamp01(angleFactor + speedFactor)), 0.1f, 0.5f);

            Debug.Log("Steering Speed: " + steeringSpeed);

            CarControllerProvider.car.steeringSpeed = steeringSpeed;
            CarControllerProvider.car.Steer(isLeftObstacle ? steeringSpeed : -steeringSpeed, Mathf.Abs(incidentAngle));
        }
        else
            ResetSteering();
    }

    public void ResetSteering()
    {
        CarControllerProvider.car.steeringSpeed = StoredSteeringSpeed;
        if (!CarControllerProvider.car.steerAction.inProgress)
            CarControllerProvider.car.Steer(0);
    }

}
