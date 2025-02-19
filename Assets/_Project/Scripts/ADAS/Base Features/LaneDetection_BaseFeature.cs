using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using Ami.BroAudio;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[Serializable]
[CreateAssetMenu(fileName = "LaneDetection_BaseFeature", menuName = "Scriptable Objects/LaneDetection_BaseFeature")]
public class LaneDetection_BaseFeature : ADAS_BaseFeature, IInitializable, IActivableByInputAction, ICarController, ICarSoundProvider, ICarFrontSensorConsumer, ICarRearSensorConsumer
{
    [Serializable]
    public class LaneDetection_Settings : ADAS_Settings
    {
        public float RayLength;
    }
    [FoldoutGroup("Lane Detection Settings"), HideLabel] public LaneDetection_Settings LaneDetectionSettings;

    // Interfaces Variables
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isOn;
    [FoldoutGroup("Lane Detection Settings"), SerializeField] private ICarController.CarControlTypeEnum _carControlType;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;
    private ICarFrontSensorProvider _carFrontSensorProvider;
    private ICarRearSensorProvider _carRearSensorProvider;
    [FoldoutGroup("Sound"), SerializeField] private AudioSource _audioSource;
    [FoldoutGroup("Sound"), SerializeField] private SerializedDictionary<string, SoundID> _soundsID;
    [Header("Debug")]
    [FoldoutGroup("Sound"), SerializeField] private bool _playSound;

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
    public ICarController.CarControlTypeEnum CarControlType { get => _carControlType; set => _carControlType = value; }

    // ICarFrontSensorConsumer
    public ICarFrontSensorProvider CarFrontSensorProvider { get => _carFrontSensorProvider; set => _carFrontSensorProvider = value; }

    // ICarRearSensorConsumer
    public ICarRearSensorProvider CarRearSensorProvider { get => _carRearSensorProvider; set => _carRearSensorProvider = value; }

    // ICarSoundProvider
    public AudioSource AudioSource { get => _audioSource; set => _audioSource = value; }
    public SerializedDictionary<string, SoundID> SoundsID { get => _soundsID; set => _soundsID = value; }
    public bool PlaySound { get => _playSound; set => _playSound = value; }

    // Sensor Variables (Lane Detection)
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleBFL;
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleBFR;
    private RaycastHit BottomFrontLeftHitInfo;
    private RaycastHit BottomFrontRightHitInfo;
    private Transform BottomFrontLeftRayStartingPoint => CarFrontSensorProvider.FrontSensorData.FrontLeftBottomRayStartingPoint;
    private Transform BottomFrontRightRayStartingPoint => CarFrontSensorProvider.FrontSensorData.FrontRightBottomRayStartingPoint;
    private Vector3 BottomFrontLeftRayDirection => BottomFrontLeftRayStartingPoint.transform.forward;
    private Vector3 BottomFrontRightRayDirection => BottomFrontRightRayStartingPoint.transform.forward;

    // Sensor Variables (Distance Detection)
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleFL;
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleFR;
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleRL;
    [FoldoutGroup("Obstacle Detection")] public bool _DetectingObstacleRR;
    private RaycastHit FrontLeftHitInfo;
    private RaycastHit FrontRightHitInfo;
    private RaycastHit RearLeftHitInfo;
    private RaycastHit RearRightHitInfo;
    private Transform FrontLeftRayStartingPoint => CarFrontSensorProvider.FrontSensorData.FrontLeftRayStartingPoint;
    private Transform FrontRightRayStartingPoint => CarFrontSensorProvider.FrontSensorData.FrontRightRayStartingPoint;
    private Transform RearLeftRayStartingPoint => CarRearSensorProvider.RearSensorData.RearLeftRayStartingPoint;
    private Transform RearRightRayStartingPoint => CarRearSensorProvider.RearSensorData.RearRightRayStartingPoint;
    private Vector3 FrontLeftRayDirection => -FrontLeftRayStartingPoint.transform.right;
    private Vector3 FrontRightRayDirection => FrontRightRayStartingPoint.transform.right;
    private Vector3 RearLeftRayDirection => RearLeftRayStartingPoint.transform.right;
    private Vector3 RearRightRayDirection => -RearRightRayStartingPoint.transform.right;

    public LaneDetection_BaseFeature()
    {
        Name = "Lane Detection";
        Description = "Lane Detection is a mechanism designed to warn the driver when the vehicle begins to move out of its lane";
    }

    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
            _isOn = false;
            _carControllerProvider = null;
            _carControllerExecutor = null;

            _DetectingObstacleFL = false;
            _DetectingObstacleFR = false;
            _DetectingObstacleRL = false;
            _DetectingObstacleRR = false;
            _DetectingObstacleBFL = false;
            _DetectingObstacleBFR = false;

        }
    }

    #region Input Action
    public void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = InputActionAsset.FindActionMap("CLifeCarController");

        ActivationAction = actionMap.FindAction("LaneDetectionActivation");

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

        return IsOn = true;
    }

    public bool TurnOff()
    {
        if (!IsOn)
            return true;

        return IsOn = false;
    }
    #endregion

    public void ExecuteCarControl()
    {
        if (IsOn)
        {
            if (DetectingBottomFrontObstacle() && !CarControllerProvider.car.steerAction.inProgress && CarControllerProvider.car.currentSpeed > 0)
                PlaySound = true;
            else
                PlaySound = false;
        }
    }

    public bool DetectingBottomFrontObstacle()
    {
        var BottomFrontLeftRayStartingPointPosition = BottomFrontLeftRayStartingPoint.position;
        var BottomFrontRightRayStartingPointPosition = BottomFrontRightRayStartingPoint.position;

        LayerMask mask = LayerMask.GetMask("Lane Detection");

        // Detecting Obstacle
        _DetectingObstacleBFL = Physics.Raycast(BottomFrontLeftRayStartingPointPosition, BottomFrontLeftRayDirection, out BottomFrontLeftHitInfo, LaneDetectionSettings.RayLength, mask);
        _DetectingObstacleBFR = Physics.Raycast(BottomFrontRightRayStartingPointPosition, BottomFrontRightRayDirection, out BottomFrontRightHitInfo, LaneDetectionSettings.RayLength, mask);

        // Draw Rays
        Debug.DrawRay(BottomFrontLeftRayStartingPointPosition, BottomFrontLeftRayDirection * LaneDetectionSettings.RayLength, _DetectingObstacleBFL ? Color.red : Color.green);
        Debug.DrawRay(BottomFrontRightRayStartingPointPosition, BottomFrontRightRayDirection * LaneDetectionSettings.RayLength, _DetectingObstacleBFR ? Color.red : Color.green);

        if (_DetectingObstacleBFL || _DetectingObstacleBFR)
            return true;

        return false;
    }

    public float CalculateAngle()
    {
        var adjustedFrontLeftRayStartingPoint = FrontLeftRayStartingPoint.TransformPoint(0.6f, -0.36f, -0.5f);
        var adjustedFrontRightRayStartingPoint = FrontRightRayStartingPoint.TransformPoint(-0.6f, -0.36f, -0.5f);
        var adjustedRearLeftRayStartingPoint = RearLeftRayStartingPoint.TransformPoint(-0.6f, -0.36f, -0.5f);
        var adjustedRearRightRayStartingPoint = RearRightRayStartingPoint.TransformPoint(0.6f, -0.36f, -0.5f);

        var FrontLeftRayStartingPointPosition = new Vector3(adjustedFrontLeftRayStartingPoint.x, adjustedFrontLeftRayStartingPoint.y, adjustedFrontLeftRayStartingPoint.z);
        var FrontRightRayStartingPointPosition = new Vector3(adjustedFrontRightRayStartingPoint.x, adjustedFrontRightRayStartingPoint.y, adjustedFrontRightRayStartingPoint.z);
        var RearLeftRayStartingPointPosition = new Vector3(adjustedRearLeftRayStartingPoint.x, adjustedRearLeftRayStartingPoint.y, adjustedRearLeftRayStartingPoint.z);
        var RearRightRayStartingPointPosition = new Vector3(adjustedRearRightRayStartingPoint.x, adjustedRearRightRayStartingPoint.y, adjustedRearRightRayStartingPoint.z);

        LayerMask mask = LayerMask.GetMask("Lane Detection");

        // Detecting Obstacle
        _DetectingObstacleFL = Physics.Raycast(FrontLeftRayStartingPointPosition, FrontLeftRayDirection, out FrontLeftHitInfo, LaneDetectionSettings.RayLength, mask);
        _DetectingObstacleFR = Physics.Raycast(FrontRightRayStartingPointPosition, FrontRightRayDirection, out FrontRightHitInfo, LaneDetectionSettings.RayLength, mask);
        _DetectingObstacleRL = Physics.Raycast(RearLeftRayStartingPointPosition, RearLeftRayDirection, out RearLeftHitInfo, LaneDetectionSettings.RayLength * 1.5f, mask);
        _DetectingObstacleRR = Physics.Raycast(RearRightRayStartingPointPosition, RearRightRayDirection, out RearRightHitInfo, LaneDetectionSettings.RayLength * 1.5f, mask);

        // Draw Rays
        Debug.DrawRay(FrontLeftRayStartingPointPosition, FrontLeftRayDirection * LaneDetectionSettings.RayLength, _DetectingObstacleFL && _DetectingObstacleBFL ? Color.red : Color.green);
        Debug.DrawRay(FrontRightRayStartingPointPosition, FrontRightRayDirection * LaneDetectionSettings.RayLength, _DetectingObstacleFR && _DetectingObstacleBFR ? Color.red : Color.green);
        Debug.DrawRay(RearLeftRayStartingPointPosition, RearLeftRayDirection * LaneDetectionSettings.RayLength * 1.5f, _DetectingObstacleRL && _DetectingObstacleBFL ? Color.red : Color.green);
        Debug.DrawRay(RearRightRayStartingPointPosition, RearRightRayDirection * LaneDetectionSettings.RayLength * 1.5f, _DetectingObstacleRR && _DetectingObstacleBFR ? Color.red : Color.green);

        float angle = 0f;
        float direction = 0f;
        Vector3 carVector, laneVector;

        if (_DetectingObstacleFL && _DetectingObstacleRL && _DetectingObstacleBFL)
        {
            carVector = FrontLeftRayStartingPointPosition - RearLeftRayStartingPointPosition;
            laneVector = FrontLeftHitInfo.point - RearLeftHitInfo.point;

            angle = Vector3.Angle(laneVector, carVector);

            direction = -Vector3.Cross(carVector.normalized, laneVector.normalized).y;
            angle *= Mathf.Sign(direction); // Angolo positivo o negativo

            Debug.Log("LEFT ANGLE: " + angle);
        }
        else if (_DetectingObstacleFR && _DetectingObstacleRR && _DetectingObstacleBFR)
        {
            carVector = FrontRightRayStartingPointPosition - RearRightRayStartingPointPosition;
            laneVector = FrontRightHitInfo.point - RearRightHitInfo.point;

            angle = Vector3.Angle(laneVector, carVector);

            direction = -Vector3.Cross(carVector.normalized, laneVector.normalized).y;
            angle *= Mathf.Sign(direction); // Angolo positivo o negativo

            Debug.Log("RIGHT ANGLE: " + angle);
        }

        return angle;
    }

    public IEnumerator EmitSound()
    {
        if (AudioSource == null || SoundsID == null)
            yield break;

        if (!SoundsID.TryGetValue("LaneDetectionBeep", out SoundID soundID))
            yield break; // Exit if the sound is not found

        AudioSource.clip = soundID.GetAudioClip();

        while (true)
        {
            while (IsOn)
            {
                if (PlaySound)
                {
                    if (!AudioSource.isPlaying)
                        AudioSource.Play();
                }
                else if (AudioSource.isPlaying)
                {
                    Debug.Log("STOP");
                    AudioSource.Stop();
                }

                yield return null;
            }

            if (AudioSource.isPlaying)
                AudioSource.Stop();

            yield return null;
        }
    }
}
