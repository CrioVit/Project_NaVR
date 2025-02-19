using Sirenix.OdinInspector;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[Serializable]
[CreateAssetMenu(fileName = "CruiseControl_BaseFeature", menuName = "Scriptable Objects/CruiseControl", order = 1)]
public class CruiseControl_BaseFeature : ADAS_BaseFeature, IInitializable, IActivableByInputAction, ICarController
{
    [Serializable]
    public class CruiseControl_Settings : ADAS_Settings
    {
        public int CruiseSpeed;
        public float MinSpeed;
        public float MaxSpeed;
    }
    [FoldoutGroup("Cruise Control Settings"), HideLabel] public CruiseControl_Settings CruiseControlSettings;

    [FoldoutGroup("Debug"), ShowInInspector] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isOn;
    [FoldoutGroup("Cruise Control Settings"), SerializeField] private ICarController.CarControlTypeEnum _controlType;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;

    // IInitializable
    public bool IsInitialized { get => _isInitialized; set => _isInitialized = value; }

    // IActivableByInputAction
    public InputActionAsset InputActionAsset { get => _inputActionAsset; set => _inputActionAsset = value; }
    public InputAction ActivationAction { get => _activationAction; set => _activationAction = value; }

    [FoldoutGroup("Input Actions"), ShowInInspector, ReadOnly]
    public List<string> Bindings => ((IActivableByInputAction)this).GetBindings(new List<(string, InputAction)>
    {
        ("Activation", ActivationAction),
        ("IncreaseSpeed", IncreaseSpeedAction),
        ("DecreaseSpeed", DecreaseSpeedAction)
    });

    public bool IsOn { get => _isOn; set => _isOn = value; }

    // ICarController
    public ICarControllerProvider CarControllerProvider { get => _carControllerProvider; set => _carControllerProvider = value; }
    public ICarControllerExecutor CarControllerExecutor { get => _carControllerExecutor; set => _carControllerExecutor = value; }
    public ICarController.CarControlTypeEnum CarControlType { get => _controlType; set => _controlType = value; }

    // Variables
    [FoldoutGroup("Debug")] public int storedMaxSpeed;

    // Other InputAction
    private InputAction IncreaseSpeedAction;
    private InputAction DecreaseSpeedAction;

    public CruiseControl_BaseFeature()
    {
        Name = "Cruise Control";
        Description = "Feature that automatically keeps car at constant speed";
    }

    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
            CruiseControlSettings.CruiseSpeed = 0;
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

        ActivationAction = actionMap.FindAction("CruiseControlActivation");
        IncreaseSpeedAction = actionMap.FindAction("CruiseControl_IncreaseSpeed");
        DecreaseSpeedAction = actionMap.FindAction("CruiseControl_DecreaseSpeed");

        // Register Callback Events
        ActivationAction.performed += Activation_action;
        IncreaseSpeedAction.performed += IncreaseSpeed_action;
        DecreaseSpeedAction.performed += DecreaseSpeed_action;

        // Enable Action
        ActivationAction.Enable();
        IncreaseSpeedAction.Enable();
        DecreaseSpeedAction.Enable();
    }

    public void OnDisable()
    {
        //Register Callback Events
        ActivationAction.performed -= Activation_action;
        IncreaseSpeedAction.performed -= IncreaseSpeed_action;
        DecreaseSpeedAction.performed -= DecreaseSpeed_action;

        // Enable Action
        ActivationAction.Disable();
        IncreaseSpeedAction.Disable();
        DecreaseSpeedAction.Disable();
    }

    public void Activation_action(InputAction.CallbackContext obj)
    {
        if (IsOn)
            TurnOff();
        else
            TurnOn();
    }

    public void IncreaseSpeed_action(InputAction.CallbackContext obj)
    {
        IncreaseSpeed();
    }

    public void DecreaseSpeed_action(InputAction.CallbackContext obj)
    {
        DecreaseSpeed();
    }

    public bool TurnOn()
    {
        if (IsOn)
            return true;

        if (!CarControllerProvider.car.isEngineOn)
            return false;

        if (CruiseControlSettings.MinSpeed != 0 && (CarControllerProvider.car.currentSpeed < CruiseControlSettings.MinSpeed))
            return false;

        if (CarControllerProvider.car.currentGear <= 0)
            return false;

        //StoredMaxSpeed = Mathf.RoundToInt(CarControllerProvider.CC.maxForwardSpeed);
        storedMaxSpeed = Mathf.RoundToInt(CarControllerProvider.car.storedMaxForwardSpeed);

        CruiseControlSettings.CruiseSpeed = Mathf.RoundToInt(CarControllerProvider.car.currentSpeed);

        return IsOn = true;
    }

    public bool TurnOff()
    {
        if (!IsOn)
            return false;

        if (!CarControllerProvider.car.isEngineOn)
            return false;

        CarControllerProvider.car.maxForwardSpeed = storedMaxSpeed;

        if (!CarControllerProvider.car.brakeAction.inProgress)
            CarControllerProvider.car.Brake(0);
        if (!CarControllerProvider.car.throttleAction.inProgress)
            CarControllerProvider.car.Throttle(0);

        return IsOn = false;
    }
    #endregion

    public void ExecuteCarControl()
    {
        if (IsOn)
        {
            if (!CarControllerProvider.car.throttleAction.inProgress || !CarControllerProvider.car.brakeAction.inProgress)
            {
                CarControllerProvider.car.maxForwardSpeed = CruiseControlSettings.CruiseSpeed;

                if (CarControllerProvider.car.currentSpeed < CruiseControlSettings.CruiseSpeed)
                {
                    CarControllerProvider.car.Brake(0f);
                    CarControllerProvider.car.Throttle(1f);
                }
                else
                {
                    CarControllerProvider.car.Throttle(0f);
                    CarControllerProvider.car.Brake(1f);
                }
            }
            else
            {
                CarControllerProvider.car.maxForwardSpeed = storedMaxSpeed;

                if(!CarControllerProvider.car.brakeAction.inProgress)
                    CarControllerProvider.car.Brake(0);
                if (!CarControllerProvider.car.throttleAction.inProgress)
                    CarControllerProvider.car.Throttle(0);
            }

            EvaluateTurnOff();
        }
    }

    public void IncreaseSpeed()
    {
        if (IsOn && ((CruiseControlSettings.CruiseSpeed + 1) <= CruiseControlSettings.MaxSpeed))
            CruiseControlSettings.CruiseSpeed++;
    }

    public void DecreaseSpeed()
    {
        if (IsOn && ((CruiseControlSettings.CruiseSpeed - 1) >= CruiseControlSettings.MinSpeed))
            CruiseControlSettings.CruiseSpeed--;
    }

    public void EvaluateTurnOff()
    {
        // Memorizza i dati necessari in variabili locali per ridurre le chiamate ripetitive
        float currentSpeed = CarControllerProvider.car.currentSpeed;
        float cruiseControlMinSpeed = CruiseControlSettings.MinSpeed;
        bool upShiftInProgress = CarControllerProvider.car.upShiftAction.inProgress;
        bool downShiftInProgress = CarControllerProvider.car.downShiftAction.inProgress;
        bool brakeInProgress = CarControllerProvider.car.brakeAction.inProgress;
        bool handbrakeInProgress = CarControllerProvider.car.handbrakeAction.inProgress;

        // Verifica le condizioni di disattivazione
        if (upShiftInProgress || downShiftInProgress || currentSpeed < cruiseControlMinSpeed || currentSpeed < 0.1f || brakeInProgress || handbrakeInProgress)
            TurnOff();
    }
}
