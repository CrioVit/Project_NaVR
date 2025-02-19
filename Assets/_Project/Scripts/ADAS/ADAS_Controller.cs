using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;
using static ICarInputHandler;

public class ADAS_Controller : MonoBehaviour, IInitializer, ICarControllerProvider, ICarControllerExecutor, ICarInputHandler, ICarStateProvider, ICarFrontSensorProvider, ICarRearSensorProvider, ICarSoundEmitter
{
    #region Classes & Enums
    [Serializable]
    public class FrontSensorDataClass
    {
        public Transform FrontLeftRayStartingPoint;
        public Transform FrontCentralRayStartingPoint;
        public Transform FrontRightRayStartingPoint;

        public Transform FrontRightBottomRayStartingPoint;
        public Transform FrontLeftBottomRayStartingPoint;
    }

    [Serializable]
    public class RearSensorDataClass
    {
        public Transform RearLeftRayStartingPoint;
        public Transform RearCentralRayStartingPoint;
        public Transform RearRightRayStartingPoint;
    }
    #endregion

    #region Feature Validation
    [FoldoutGroup("ADAS"), InlineEditor, OnValueChanged("ValidateFeatures")] public List<ADAS_MajorFeature> ADAS_MajorFeatures;
    [FoldoutGroup("ADAS"), InlineEditor, OnValueChanged("ValidateFeatures")] public List<ADAS_BaseFeature> ADAS_BaseFeatures;

    private void ValidateFeatures()
    {
        // Validate Feature List
        for (int i = 0; i < ADAS_MajorFeatures.Count; i++)
        {
            ADAS_MajorFeature majorFeature = ADAS_MajorFeatures[i];

            for (int j = 0; j < majorFeature.BaseFeaturesList.Count; j++)
            {
                ADAS_BaseFeature feature = majorFeature.BaseFeaturesList[j];

                if (!ADAS_BaseFeatures.Contains(feature))
                {
                    ADAS_BaseFeatures.Add(feature);
                }
            }
        }

        if (ADAS_BaseFeatures.Count > 0)
            ADAS_BaseFeatures = ADAS_BaseFeatures.Distinct().ToList();
    }
    #endregion

    #region Key Variables
    // Check Key
    [FoldoutGroup("INPUT MAPPING"), SerializeField] private SerializedDictionary<KeyMapping, UnityEvent> _inputs = new();

    public SerializedDictionary<KeyMapping, UnityEvent> Inputs { get => _inputs; set => _inputs = value; }
    #endregion

    #region Interfaces Variables/Properties
    [FoldoutGroup("CAR STATE")] public ICarStateProvider.CarStateEnum _carState;

    [FoldoutGroup("SENSOR DATA")]
    [FoldoutGroup("SENSOR DATA/Setup")] public FrontSensorDataClass _frontSensorData;
    [FoldoutGroup("SENSOR DATA/Setup")] public RearSensorDataClass _rearSensorData;

    public ICarStateProvider.CarStateEnum CarState { get => _carState; set => _carState = value; }
    [field: SerializeField, FoldoutGroup("CAR CONTROLLER"), InlineEditor] public Car car { get; set; }
    public FrontSensorDataClass FrontSensorData { get => _frontSensorData; set => _frontSensorData = value; }
    public RearSensorDataClass RearSensorData { get => _rearSensorData; set => _rearSensorData = value; }
    #endregion

    private void Start()
    {
        car = GetComponent<Car>();

        Initialize(reset: false);
        InjectCarControllerProviders(this);
        InjectCarControllerExecutors(this);
        InjectCarStateProviders(this);
        InjectCarFrontSensorProvider(this);
        InjectCarRearSensorProvider(this);
        StartCarSoundProviders();
    }

    // Update is called once per frame
    void Update()
    {
        HandleInputs();
        EvaluateCarState();
        ExecuteContinuousCarControls();
    }

    private void OnDisable()
    {
        Initialize(reset: true);
    }

    private void EvaluateCarState()
    {
        if (car.currentGear >= 1)
            CarState = ICarStateProvider.CarStateEnum.GoingForward;
        else if (car.currentGear == 0)
            CarState = ICarStateProvider.CarStateEnum.Neutral;
        else if (car.currentGear == -1)
            CarState = ICarStateProvider.CarStateEnum.GoingReverse;
    }
 
    #region Interfaces Methods
    // IInitializer
    public void Initialize(bool reset = false)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is IInitializable initializable)
                initializable.Init(reset);

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is IInitializable initializable)
                initializable.Init(reset);
    }

    // ICarInputHandler
    public void HandleInputs()
    {
        // Check the input

        foreach (KeyMapping mapping in _inputs.Keys)
        {
            switch (mapping.KeyPressType)
            {
                case KeyPressTypeEnum.Key:
                    if (Input.GetKey(mapping.Key))
                        _inputs[mapping].Invoke();
                    break;

                case KeyPressTypeEnum.KeyDown:
                    if (Input.GetKeyDown(mapping.Key))
                        _inputs[mapping].Invoke();
                    break;

                case KeyPressTypeEnum.KeyUp:
                    if (Input.GetKeyUp(mapping.Key))
                        _inputs[mapping].Invoke();
                    break;

                case KeyPressTypeEnum.NotPressed:
                    if (!Input.GetKey(mapping.Key))
                        _inputs[mapping].Invoke();
                    break;
            }
        }

        // Check for feature activation & inputs
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
        {
            IActivable activable = majorFeature as IActivable;

            if (activable != null)
                if (Input.GetKeyDown(activable.ActivationKey))
                {
                    if (!activable.IsOn)
                        activable.TurnOn();
                    else
                        activable.TurnOff();
                }

            if (activable == null || activable.IsOn)
                if (majorFeature is ICarInputHandler inputHandler)
                    inputHandler.HandleInputs();
        }

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
        {
            IActivable activable = baseFeature as IActivable;

            if (activable != null)
                if (Input.GetKeyDown(activable.ActivationKey))
                {
                    if (!activable.IsOn)
                        activable.TurnOn();
                    else
                        activable.TurnOff();
                }

            if (activable == null || activable.IsOn)
                if (baseFeature is ICarInputHandler inputHandler)
                    inputHandler.HandleInputs();
        }
    } 

    // ICarControllerProvider
    public void InjectCarControllerProviders(ICarControllerProvider provider)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarController consumer)
                consumer.CarControllerProvider = provider;

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarController consumer)
                consumer.CarControllerProvider = provider;
    }

    // ICarControllerExecutor
    public void InjectCarControllerExecutors(ICarControllerExecutor executor)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarController consumer)
                consumer.CarControllerExecutor = executor;

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarController consumer)
                consumer.CarControllerExecutor = executor;
    }

    public void ExecuteContinuousCarControls()
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
        {
            if (majorFeature is IActivable activable)
                if (activable == null || activable.IsOn)
                    if (majorFeature is ICarController carController)
                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
                            carController.ExecuteCarControl();
        }

        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
        {
            if (majorFeature is IActivableByInputAction activable)
                if (activable == null || activable.IsOn)
                    if (majorFeature is ICarController carController)
                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
                            carController.ExecuteCarControl();
        }

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
        {
            if (baseFeature is IActivable activable)
                if (activable == null || activable.IsOn)
                    if (baseFeature is ICarController carController)
                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
                            carController.ExecuteCarControl();
        }

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
        {
            if (baseFeature is IActivableByInputAction activable)
                if (activable == null || activable.IsOn)
                    if (baseFeature is ICarController carController)
                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
                            carController.ExecuteCarControl();
        }
    }

    public void ExecuteCarControl(ICarController controller)
    {
        controller.ExecuteCarControl();
    }

    // ICarStateProvider
    public void InjectCarStateProviders(ICarStateProvider provider)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarStateConsumer consumer)
                consumer.CarStateProvider = provider;

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarStateConsumer consumer)
                consumer.CarStateProvider = provider;
    }

    // ICarFrontSensorProvider
    public void InjectCarFrontSensorProvider(ICarFrontSensorProvider provider)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarFrontSensorConsumer consumer)
                consumer.CarFrontSensorProvider = provider;

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarFrontSensorConsumer consumer)
                consumer.CarFrontSensorProvider = provider;
    }

    // ICarRearSensorProvider
    public void InjectCarRearSensorProvider(ICarRearSensorProvider provider)
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarRearSensorConsumer consumer)
                consumer.CarRearSensorProvider = provider;

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarRearSensorConsumer consumer)
                consumer.CarRearSensorProvider = provider;
    }

    public void StartCarSoundProviders()
    {
        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
            if (majorFeature is ICarSoundProvider provider)
                StartCoroutine(provider.EmitSound());

        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
            if (baseFeature is ICarSoundProvider provider)
                StartCoroutine(provider.EmitSound());
    }
    #endregion
}

#region Interfaces
public interface IInitializer
{
    public void Initialize(bool reset = false);
}

public interface ICarInputHandler
{
    public enum KeyPressTypeEnum
    {
        NotPressed,
        Key,
        KeyDown,
        KeyUp
    }

    [Serializable]
    public class KeyMapping
    {
        public KeyCode Key;
        public KeyPressTypeEnum KeyPressType;
    }

    public SerializedDictionary<KeyMapping, UnityEvent> Inputs { get; set; }

    public void HandleInputs();
}

public interface ICarControllerProvider
{
    public Car car { get; }

    public void InjectCarControllerProviders(ICarControllerProvider provider);
}

public interface ICarControllerExecutor
{
    public void InjectCarControllerExecutors(ICarControllerExecutor executor);

    public void ExecuteContinuousCarControls();

    public void ExecuteCarControl(ICarController controller);
}

public interface ICarStateProvider
{
    //public enum CarStateEnum
    //{
    //    Idle,
    //    PowerOn,
    //    EngineOn,
    //    EngineOff,
    //    GoingForward,
    //    GoingReverse
    //}

    public enum CarStateEnum
    {
        Neutral,
        GoingForward,
        GoingReverse
    }

    public CarStateEnum CarState { get; set; }

    public void InjectCarStateProviders(ICarStateProvider provider);
}

public interface ICarFrontSensorProvider
{
    public ADAS_Controller.FrontSensorDataClass FrontSensorData { get; set; }

    public void InjectCarFrontSensorProvider(ICarFrontSensorProvider provider);
}

public interface ICarRearSensorProvider
{
    public ADAS_Controller.RearSensorDataClass RearSensorData { get; set; }

    public void InjectCarRearSensorProvider(ICarRearSensorProvider provider);
}

public interface ICarSoundEmitter
{
    public void StartCarSoundProviders();
}

#endregion
