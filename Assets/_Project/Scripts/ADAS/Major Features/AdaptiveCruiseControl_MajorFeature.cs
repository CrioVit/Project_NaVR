using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AdaptiveCruiseControl", menuName = "Scriptable Objects/AdaptiveCruiseControl")]
public class AdaptiveCruiseControl_MajorFeature : ADAS_MajorFeature, IInitializable, IActivableByInputAction, ICarController
{
    [Serializable]
    public class AdaptiveCruiseControl_Settings : ADAS_Settings
    {
        [Range(0, 30)] public int distance;
        [Range(0, 30)] public int maxDistance;
        [Range(0, 30)] public int minDistance;
        public float minSpeed;
    }
    [FoldoutGroup("Adaptive Cruise Control Settings"), HideLabel] public AdaptiveCruiseControl_Settings AdaptiveCruiseControlSettings;

    [FoldoutGroup("ADAS: Base Features"), ShowInInspector] public BrakeAssist_BaseFeature BrakeAssistBaseFeature => (BrakeAssist_BaseFeature)BaseFeaturesList.FirstOrDefault((f) => f is BrakeAssist_BaseFeature feature);
    [FoldoutGroup("ADAS: Base Features"), ShowInInspector] public CruiseControl_BaseFeature CruiseControlBaseFeature => (CruiseControl_BaseFeature)BaseFeaturesList.FirstOrDefault((f) => f is CruiseControl_BaseFeature feature);

    [FoldoutGroup("Debug"), SerializeField] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), SerializeField] private bool _isOn;
    [FoldoutGroup("Adaptive Cruise Control Settings"), SerializeField] private ICarController.CarControlTypeEnum _controlType;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;
    //[FoldoutGroup("Inputs"), SerializeField] private SerializedDictionary<KeyMapping, UnityEvent> _inputs;

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

    // ICarInputHandler
    //public SerializedDictionary<KeyMapping, UnityEvent> Inputs { get => _inputs; set => _inputs  = value; }

    //Obstacle Detection
    [FoldoutGroup("Debug")] public float DistanceToFO;
    [FoldoutGroup("Debug")] public float BrakeAssistSecurityDistance;

    //Speed
    [FoldoutGroup("Debug")] public int StoredCruiseSpeed;
    [FoldoutGroup("Debug")] public int StoredMaxSpeed;

    public AdaptiveCruiseControl_MajorFeature()
    {
        Name = "Adaptive Cruise Control";
        Description = "Feature that automatically adjusts the vehicle speed to maintain a safe distance from vehicles ahead";
    }

    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
            _isOn = false;
            _carControllerProvider = null;
            _carControllerExecutor = null;

            BrakeAssistSecurityDistance = 0;
            DistanceToFO = 0;

            StoredCruiseSpeed = 0;
        }
    }

    #region Input Action
    public void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = InputActionAsset.FindActionMap("CLifeCarController");

        ActivationAction = actionMap.FindAction("AdaptiveCruiseControlActivation");

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

        if (CruiseControlBaseFeature.TurnOn() && BrakeAssistBaseFeature.TurnOn())
        {
            AdaptiveCruiseControlSettings.minSpeed = CruiseControlBaseFeature.CruiseControlSettings.MinSpeed;
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

        if (!CruiseControlBaseFeature.TurnOff() && !BrakeAssistBaseFeature.TurnOff())
            IsOn = false;
        else
            IsOn = true;

        return IsOn;
    }
    #endregion

    //public void ExecuteCarControl()
    //{
    //    BrakeAssistBaseFeature.ExecuteCarControl();
    //    CruiseControlBaseFeature.ExecuteCarControl();

    //    StoredCruiseSpeed = CruiseControlBaseFeature.CruiseControlSettings.CruiseSpeed;
    //    StoredMaxSpeed = CruiseControlBaseFeature.storedMaxSpeed;

    //    if (BrakeAssistBaseFeature.DetectingObstacleForward())
    //    {
    //        DistanceToFO = BrakeAssistBaseFeature.DistanceToForwardObstacle();

    //        if (DistanceToFO < BrakeAssistBaseFeature.BrakeAssistSettings.SecurityDistance || DistanceToFO < AdaptiveCruiseControlSettings.MaxDistance)
    //        {
    //            CruiseControlBaseFeature.TurnOff();
    //        }
    //        else
    //        {
    //            if (!CruiseControlBaseFeature.IsOn)
    //            {
    //                CruiseControlBaseFeature.CruiseControlSettings.CruiseSpeed = StoredCruiseSpeed;
    //                CruiseControlBaseFeature.storedMaxSpeed = StoredMaxSpeed;
    //            }
    //            else
    //            {
    //                CruiseControlBaseFeature.TurnOn();
    //                CruiseControlBaseFeature.CruiseControlSettings.CruiseSpeed = StoredCruiseSpeed;
    //            }

    //        }
    //    }

    //    if(CarControllerProvider.CC.currentSpeed <= 0)
    //    {
    //        TurnOff();
    //    }

    //}


    public void ExecuteCarControl()
    {
        if (IsOn)
        {
            DistanceToFO = BrakeAssistBaseFeature.DistanceToForwardObstacle();
            BrakeAssistSecurityDistance = BrakeAssistBaseFeature.CalculateSecurityDistance();
            float AdaptiveCruiseControlDistance = BrakeAssistSecurityDistance + AdaptiveCruiseControlSettings.distance;

            if (BrakeAssistBaseFeature.DetectingObstacleForward())
            {
                if (AdaptiveCruiseControlDistance != BrakeAssistSecurityDistance && AdaptiveCruiseControlDistance > BrakeAssistSecurityDistance)
                {
                    if (DistanceToFO >= AdaptiveCruiseControlDistance)
                        CruiseControlBaseFeature.IncreaseSpeed();
                    else
                        CruiseControlBaseFeature.DecreaseSpeed();
                }
            }

            EvaluateTurnOff();
        }   
    }

    //public void HandleInputs()
    //{
    //    foreach (KeyMapping mapping in _inputs.Keys)
    //    {
    //        switch (mapping.KeyPressType)
    //        {
    //            case KeyPressTypeEnum.Key:
    //                if (Input.GetKey(mapping.Key))
    //                    _inputs[mapping].Invoke();
    //                break;

    //            case KeyPressTypeEnum.KeyDown:
    //                if (Input.GetKeyDown(mapping.Key))
    //                    _inputs[mapping].Invoke();
    //                break;

    //            case KeyPressTypeEnum.KeyUp:
    //                if (Input.GetKeyUp(mapping.Key))
    //                    _inputs[mapping].Invoke();
    //                break;
    //        }
    //    }
    //}

    //public void IncreaseDistance()
    //{
    //    if (IsOn && ((AdaptiveCruiseControlSettings.distance + 1) <= AdaptiveCruiseControlSettings.maxDistance))
    //        AdaptiveCruiseControlSettings.distance ++;
    //}

    //public void DecreaseDistance()
    //{
    //    if (IsOn && ((AdaptiveCruiseControlSettings.distance - 1) <= AdaptiveCruiseControlSettings.minDistance))
    //        AdaptiveCruiseControlSettings.distance --;
    //}

    //public void TurnOff_action()
    //{
    //    TurnOff();
    //}

    public void EvaluateTurnOff()
    {
        // Memorizza i dati necessari in variabili locali per ridurre le chiamate ripetitive
        float currentSpeed = CarControllerProvider.car.currentSpeed;
        bool upShiftInProgress = CarControllerProvider.car.upShiftAction.inProgress;
        bool downShiftInProgress = CarControllerProvider.car.downShiftAction.inProgress;
        bool brakeInProgress = CarControllerProvider.car.brakeAction.inProgress;
        bool handbrakeInProgress = CarControllerProvider.car.handbrakeAction.inProgress;

        // Verifica le condizioni di disattivazione
        if (currentSpeed <= AdaptiveCruiseControlSettings.minSpeed || upShiftInProgress || downShiftInProgress || currentSpeed < 0.1f || brakeInProgress || handbrakeInProgress)
            TurnOff();
    }

}
