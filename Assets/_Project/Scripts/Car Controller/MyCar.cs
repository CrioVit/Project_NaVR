//using AYellowpaper.SerializedCollections;
//using System.Collections.Generic;
//using Sirenix.OdinInspector;
//using UnityEngine;
//using System.Linq;
//using System;
//using UnityEngine.Events;
//using static PrometeoCarController;
//using static ICarInputHandler;
//using Unity.VisualScripting;

//public class MyCar : MonoBehaviour, IInitializer, ICarDataProvider, ICarControllerProvider, ICarControllerExecutor, ICarInputHandler, ICarStateProvider, ICarFrontSensorProvider, ICarRearSensorProvider, ICarSoundEmitter
//{
//    #region Classes & Enums

//    [Serializable]
//    public class FrontSensorDataClass
//    {
//        public Transform FrontLeftRayStartingPoint;
//        public Transform FrontCentralRayStartingPoint;
//        public Transform FrontRightRayStartingPoint;

//        public Transform FrontRightBottomRayStartingPoint;
//        public Transform FrontLeftBottomRayStartingPoint;
//    }

//    [Serializable]
//    public class RearSensorDataClass
//    {
//        public Transform RearLeftRayStartingPoint;
//        public Transform RearCentralRayStartingPoint;
//        public Transform RearRightRayStartingPoint;
//    }

//    #endregion

//    #region Feature Validation

//    [FoldoutGroup("ADAS"), InlineEditor, OnValueChanged("ValidateFeatures")] public List<ADAS_MajorFeature> ADAS_MajorFeatures;
//    [FoldoutGroup("ADAS"), InlineEditor, OnValueChanged("ValidateFeatures")] public List<ADAS_BaseFeature> ADAS_BaseFeatures;

//    private void ValidateFeatures()
//    {
//        // Validate Feature List
//        for (int i = 0; i < ADAS_MajorFeatures.Count; i++)
//        {
//            //if (ADAS_MajorFeatures[i] is AutoPilot autopilot)
//            //{
//            //    //if ADAS_MajorFeatures is different from autopilot, will remove it
//            //    ADAS_MajorFeatures.RemoveAll(amf => amf != autopilot);
//            //    ADAS_BaseFeatures.Clear();
//            //    break;
//            //}

//            ADAS_MajorFeature majorFeature = ADAS_MajorFeatures[i];

//            for (int j = 0; j < majorFeature.BaseFeaturesList.Count; j++)
//            {
//                ADAS_BaseFeature feature = majorFeature.BaseFeaturesList[j];

//                if (!ADAS_BaseFeatures.Contains(feature))
//                {
//                    ADAS_BaseFeatures.Add(feature);
//                }
//            }
//        }

//        if (ADAS_BaseFeatures.Count > 0)
//            ADAS_BaseFeatures = ADAS_BaseFeatures.Distinct().ToList();
//    }

//    #endregion

//    #region Key Variables

//    // Check Key
//    [FoldoutGroup("INPUT MAPPING"), SerializeField] private SerializedDictionary<KeyMapping, UnityEvent> _inputs = new();

//    public SerializedDictionary<KeyMapping, UnityEvent> Inputs { get => _inputs; set => _inputs = value; }

//    [HideInInspector] public bool key_W_NotPressed;
//    [HideInInspector] public bool key_A_NotPressed;
//    [HideInInspector] public bool key_S_NotPressed;
//    [HideInInspector] public bool key_D_NotPressed;
//    [HideInInspector] public bool key_SPACE_NotPressed;

//    #endregion

//    #region Interfaces Variables/Properties

//    public CarData CarData => PCC?.carData;
//    [FoldoutGroup("CAR STATE")] public ICarStateProvider.CarStateEnum _carState;

//    [FoldoutGroup("SENSOR DATA")]
//    [FoldoutGroup("SENSOR DATA/Setup")] public FrontSensorDataClass _frontSensorData;
//    [FoldoutGroup("SENSOR DATA/Setup")] public RearSensorDataClass _rearSensorData;

//    public ICarStateProvider.CarStateEnum CarState { get => _carState; set => _carState = value; }
//    [field: SerializeField, FoldoutGroup("CAR CONTROLLER - by Prometeo"), InlineEditor] public PrometeoCarController PCC { get; set; }
//    public FrontSensorDataClass FrontSensorData { get => _frontSensorData; set => _frontSensorData = value; }
//    public RearSensorDataClass RearSensorData { get => _rearSensorData; set => _rearSensorData = value; }

//    #endregion

//    #region Variables

//    // Debug
//    [FoldoutGroup("STEERING WHEEL")]
//    [FoldoutGroup("STEERING WHEEL/Setup")] public Transform steeringWheel;
//    [FoldoutGroup("STEERING WHEEL/Debug")] public float steeringWheelAngle;

//    #endregion

//    private void Start()
//    {
//        Initialize(reset: false);
//        InjectCarDataProviders(this);
//        InjectCarControllerProviders(this);
//        InjectCarControllerExecutors(this);
//        InjectCarStateProviders(this);
//        InjectCarFrontSensorProvider(this);
//        InjectCarRearSensorProvider(this);
//        StartCarSoundProviders();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        HandleInputs();

//        if (key_S_NotPressed && key_W_NotPressed)
//        {
//            ThrottleOff();
//        }
//        if ((key_S_NotPressed && key_W_NotPressed) && key_SPACE_NotPressed && !CarData.deceleratingCar)
//        {
//            PCC.InvokeRepeating("DecelerateCar", 0f, 0.1f);
//            CarData.deceleratingCar = true;
//        }
//        if (key_A_NotPressed && key_D_NotPressed && CarData.steeringAxis != 0f)
//        {
//            ResetSteeringAngle();
//        }

//        ExecuteContinuousCarControls();
//        AnimateSteeringWheel();
//    }

//    private void OnDisable()
//    {
//        Initialize(reset: true);
//    }

//    #region Keys Methods
//    public void Key_W_NotPressed() { key_W_NotPressed = true; }
//    public void Key_A_NotPressed() { key_A_NotPressed = true; }
//    public void Key_S_NotPressed() { key_S_NotPressed = true; }
//    public void Key_D_NotPressed() { key_D_NotPressed = true; }
//    public void Key_SPACE_NotPressed() { key_SPACE_NotPressed = true; }
//    #endregion

//    #region Car Methods
//    //This method changes the rotation of the steeringwheel
//    public void AnimateSteeringWheel()
//    {
//        try
//        {
//            if (PCC.carData.steeringAxis >= 0)
//                steeringWheelAngle = Mathf.Lerp(0, 90, PCC.carData.steeringAxis);
//            else
//                steeringWheelAngle = Mathf.Lerp(-90, 0, 1 + PCC.carData.steeringAxis);

//            steeringWheel.localRotation = Quaternion.Euler(steeringWheelAngle, 0f, 0f);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogWarning(ex);
//        }
//    }

//    public void PowerOn()
//    {
//        CarState = ICarStateProvider.CarStateEnum.PowerOn;
//    }

//    public void TurnEngineOnOff()
//    {
//        if (PCC.carData.isEngineOn == false)
//        {
//            PCC.carData.isEngineOn = true;
//            CarState = ICarStateProvider.CarStateEnum.EngineOn;
//        }
//        else
//        {
//            PCC.carData.isEngineOn = false;
//            CarState = ICarStateProvider.CarStateEnum.EngineOff;
//        }

//        // cose grafiche
//    }

//    public void GoForward()
//    {
//        if (CarData.isEngineOn)
//        {
//            key_W_NotPressed = false;

//            PCC.CancelInvoke("DecelerateCar");
//            CarData.deceleratingCar = false;
//            PCC.GoForward();

//            CarState = ICarStateProvider.CarStateEnum.GoingForward;
//        }
//    }

//    public void GoReverse()
//    {
//        if (CarData.isEngineOn)
//        {
//            key_S_NotPressed = false;

//            PCC.CancelInvoke("DecelerateCar");
//            CarData.deceleratingCar = false;
//            PCC.GoReverse();

//            CarState = ICarStateProvider.CarStateEnum.GoingReverse;
//        }
//    }

//    public void TurnLeft()
//    {
//        if (CarData.isEngineOn)
//        {
//            key_A_NotPressed = false;

//            PCC.TurnLeft();
//        }
//    }

//    public void TurnRight()
//    {
//        if (CarData.isEngineOn)
//        {
//            key_D_NotPressed = false;

//            PCC.TurnRight();
//        }
//    }

//    public void Brakes()
//    {
//        if (CarData.isEngineOn)
//        {
//            PCC.Brakes();
//        }
//    }

//    public void Handbrake()
//    {
//        if (CarData.isEngineOn)
//        {
//            key_SPACE_NotPressed = false;

//            PCC.CancelInvoke("DecelerateCar");
//            CarData.deceleratingCar = false;
//            PCC.Handbrake();
//        }
//    }

//    public void RecoverTraction()
//    {
//        if (CarData.isEngineOn)
//        {
//            PCC.RecoverTraction();
//        }
//    }

//    public void ThrottleOff()
//    {
//        if (CarData.isEngineOn)
//        {
//            PCC.ThrottleOff();
//        }
//    }

//    public void DeceleratingCar()
//    {
//        if (CarData.isEngineOn)
//        {
//            PCC.InvokeRepeating("DecelerateCar", 0f, 0.1f);
//            PCC.carData.deceleratingCar = true;
//        }
//    }

//    public void ResetSteeringAngle()
//    {
//        if (CarData.isEngineOn)
//        {
//            PCC.ResetSteeringAngle();
//        }
//    }
//    #endregion

//    #region Interfaces Methods
//    // IInitializer
//    public void Initialize(bool reset = false)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is IInitializable initializable)
//                initializable.Init(reset);

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is IInitializable initializable)
//                initializable.Init(reset);
//    }

//    // ICarInputHandler
//    public void HandleInputs()
//    {
//        // Check the input

//        foreach (KeyMapping mapping in _inputs.Keys)
//        {
//            switch (mapping.KeyPressType)
//            {
//                case KeyPressTypeEnum.Key:
//                    if (Input.GetKey(mapping.Key))
//                        _inputs[mapping].Invoke();
//                    break;

//                case KeyPressTypeEnum.KeyDown:
//                    if (Input.GetKeyDown(mapping.Key))
//                        _inputs[mapping].Invoke();
//                    break;

//                case KeyPressTypeEnum.KeyUp:
//                    if (Input.GetKeyUp(mapping.Key))
//                        _inputs[mapping].Invoke();
//                    break;

//                case KeyPressTypeEnum.NotPressed:
//                    if (!Input.GetKey(mapping.Key))
//                        _inputs[mapping].Invoke();
//                    break;
//            }
//        }

//        // Check for feature activation & inputs
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//        {
//            IActivable activable = majorFeature as IActivable;

//            if (activable != null)
//                if (Input.GetKeyDown(activable.ActivationKey))
//                {
//                    if (!activable.IsOn)
//                        activable.TurnOn();
//                    else
//                        activable.TurnOff();
//                }

//            if (activable == null || activable.IsOn)
//                if (majorFeature is ICarInputHandler inputHandler)
//                    inputHandler.HandleInputs();
//        }

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//        {
//            IActivable activable = baseFeature as IActivable;

//            if (activable != null)
//                if (Input.GetKeyDown(activable.ActivationKey))
//                {
//                    if (!activable.IsOn)
//                        activable.TurnOn();
//                    else
//                        activable.TurnOff();
//                }

//            if (activable == null || activable.IsOn)
//                if (baseFeature is ICarInputHandler inputHandler)
//                    inputHandler.HandleInputs();
//        }
//    }

//    // ICarDataProvider
//    public void InjectCarDataProviders(ICarDataProvider provider)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarDataConsumer consumer)
//                consumer.CarDataProvider = provider;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarDataConsumer consumer)
//                consumer.CarDataProvider = provider;
//    }

//    // ICarControllerProvider
//    public void InjectCarControllerProviders(ICarControllerProvider provider)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarController consumer)
//                consumer.CarControllerProvider = provider;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarController consumer)
//                consumer.CarControllerProvider = provider;
//    }

//    // ICarControllerExecutor
//    public void InjectCarControllerExecutors(ICarControllerExecutor executor)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarController consumer)
//                consumer.CarControllerExecutor = executor;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarController consumer)
//                consumer.CarControllerExecutor = executor;
//    }

//    public void ExecuteContinuousCarControls()
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//        {
//            if (majorFeature is IActivable activable)
//                if (activable == null || activable.IsOn)
//                    if (majorFeature is ICarController carController)
//                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
//                            carController.ExecuteCarControl();
//        }

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//        {
//            if (baseFeature is IActivable activable)
//                if (activable == null || activable.IsOn)
//                    if (baseFeature is ICarController carController)
//                        if (carController.CarControlType == ICarController.CarControlTypeEnum.Continuous)
//                            carController.ExecuteCarControl();
//        }
//    }

//    public void ExecuteCarControl(ICarController controller)
//    {
//        controller.ExecuteCarControl();
//    }

//    // ICarStateProvider
//    public void InjectCarStateProviders(ICarStateProvider provider)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarStateConsumer consumer)
//                consumer.CarStateProvider = provider;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarStateConsumer consumer)
//                consumer.CarStateProvider = provider;
//    }

//    // ICarFrontSensorProvider
//    public void InjectCarFrontSensorProvider(ICarFrontSensorProvider provider)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarFrontSensorConsumer consumer)
//                consumer.CarFrontSensorProvider = provider;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarFrontSensorConsumer consumer)
//                consumer.CarFrontSensorProvider = provider;
//    }

//    // ICarRearSensorProvider
//    public void InjectCarRearSensorProvider(ICarRearSensorProvider provider)
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarRearSensorConsumer consumer)
//                consumer.CarRearSensorProvider = provider;

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarRearSensorConsumer consumer)
//                consumer.CarRearSensorProvider = provider;
//    }

//    public void StartCarSoundProviders()
//    {
//        foreach (ADAS_MajorFeature majorFeature in ADAS_MajorFeatures)
//            if (majorFeature is ICarSoundProvider provider)
//                StartCoroutine(provider.EmitSound());

//        foreach (ADAS_BaseFeature baseFeature in ADAS_BaseFeatures)
//            if (baseFeature is ICarSoundProvider provider)
//                StartCoroutine(provider.EmitSound());
//    }
//    #endregion
//}

//#region Interfaces
//public interface IInitializer
//{
//    public void Initialize(bool reset = false);
//}

//public interface ICarInputHandler
//{
//    public enum KeyPressTypeEnum
//    {
//        NotPressed,
//        Key,
//        KeyDown,
//        KeyUp
//    }

//    [Serializable]
//    public class KeyMapping
//    {
//        public KeyCode Key;
//        public KeyPressTypeEnum KeyPressType;
//    }

//    public SerializedDictionary<KeyMapping, UnityEvent> Inputs { get; set; }

//    public void HandleInputs();
//}

//public interface ICarDataProvider
//{
//    public CarData CarData { get; }

//    public void InjectCarDataProviders(ICarDataProvider provider);
//}

//public interface ICarControllerProvider
//{
//    public PrometeoCarController PCC { get; }

//    public void InjectCarControllerProviders(ICarControllerProvider provider);
//}

//public interface ICarControllerExecutor
//{
//    public void InjectCarControllerExecutors(ICarControllerExecutor executor);

//    public void ExecuteContinuousCarControls();

//    public void ExecuteCarControl(ICarController controller);
//}

//public interface ICarStateProvider
//{
//    public enum CarStateEnum
//    {
//        Idle,
//        PowerOn,
//        EngineOn,
//        EngineOff,
//        GoingForward,
//        GoingReverse
//    }

//    public CarStateEnum CarState { get; set; }

//    public void InjectCarStateProviders(ICarStateProvider provider);
//}

//public interface ICarFrontSensorProvider
//{
//    public MyCar.FrontSensorDataClass FrontSensorData { get; set; }

//    public void InjectCarFrontSensorProvider(ICarFrontSensorProvider provider);
//}

//public interface ICarRearSensorProvider
//{
//    public MyCar.RearSensorDataClass RearSensorData { get; set; }

//    public void InjectCarRearSensorProvider(ICarRearSensorProvider provider);
//}

//public interface ICarSoundEmitter
//{
//    public void StartCarSoundProviders();
//}

//#endregion