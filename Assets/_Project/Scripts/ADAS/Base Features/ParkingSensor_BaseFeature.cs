using Ami.BroAudio;
using AYellowpaper.SerializedCollections;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
[CreateAssetMenu(fileName = "ParkingSensor_BaseFeature", menuName = "Scriptable Objects/ParkingSensor")]
public class ParkingSensor_BaseFeature : ADAS_BaseFeature, IInitializable, IActivableByInputAction, ICarController, ICarRearSensorConsumer, ICarStateConsumer, ICarSoundProvider
{
    [Serializable]
    public class ParkingSensor_Settings : ADAS_Settings
    {
        public float PS_ActivationDistance;
        public float PS_FarDistance;
        public float PS_MidDistance;
        public float PS_NearDistance;
    }
    [FoldoutGroup("Parking Sensor Settings"), HideLabel] public ParkingSensor_Settings ParkingSensorSettings;

    public enum ObstaclePresenceEnum
    {
        None,
        Near,
        Mid,
        Far
    }

    [FoldoutGroup("Obstacle Detection"), SerializeField] private ObstaclePresenceEnum _obstaclePresence;

    // Initialize Parking Sensor
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isInitialized;
    [FoldoutGroup("Input Actions"), SerializeField] private InputActionAsset _inputActionAsset;
    private InputAction _activationAction;
    [FoldoutGroup("Debug"), ShowInInspector] private bool _isOn;
    private ICarControllerProvider _carControllerProvider;
    private ICarControllerExecutor _carControllerExecutor;
    [FoldoutGroup("Parkin Sensor Settings"), SerializeField] private ICarController.CarControlTypeEnum _carControlType;
    private ICarRearSensorProvider _carRearSensorProvider;
    private ICarStateProvider _carStateProvider;
    //[FoldoutGroup("Sounds"), SerializeField] private SerializedDictionary<string, AudioClip> _sounds;
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
    public ICarRearSensorProvider CarRearSensorProvider { get => _carRearSensorProvider; set => _carRearSensorProvider = value; }

    // ICarStateConsumer
    public ICarStateProvider CarStateProvider { get => _carStateProvider; set => _carStateProvider = value; }

    // ICarSoundProvider
    //public SerializedDictionary<string, AudioClip> AudioClips { get => _sounds; set => _sounds = value; }
    public AudioSource AudioSource { get => _audioSource; set => _audioSource = value; }
    public SerializedDictionary<string, SoundID> SoundsID { get => _soundsID; set => _soundsID = value; }
    public bool PlaySound { get => _playSound; set => _playSound = value; }

    // Obstacle Detection
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleRL;
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleRC;
    [FoldoutGroup("Obstacle Detection"), SerializeField] private bool _DetectingObstacleRR;

    [FoldoutGroup("Obstacle Detection"), SerializeField] private float _distanceToBackwardObstacle;

    private RaycastHit RearLeftHitInfo;
    private RaycastHit RearCentralHitInfo;
    private RaycastHit RearRightHitInfo;
    public Vector3 RayDirectionBackward => -CarControllerProvider.car.rb.transform.forward;

    public ParkingSensor_BaseFeature ()
    {
        Name = "Parking Sensor";
        Description = "Proximity sensors for road vehicles designed to alert the driver of obstacles while parking";
    }

    public void Init(bool reset = false)
    {
        _isInitialized = !reset;

        if (reset)
        {
            _isOn = false;
            _carControllerProvider = null;
            _carControllerExecutor = null;
            _obstaclePresence = ObstaclePresenceEnum.None;
        }
    }

    #region Input Action
    public void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = InputActionAsset.FindActionMap("CLifeCarController");

        ActivationAction = actionMap.FindAction("ParkingSensorActivation");

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

        //if (CarStateProvider.CarState == ICarStateProvider.CarStateEnum.GoingReverse)
        //{
        //    return IsOn = true;
        //}
        //else
        //{
        //    _obstaclePresence = ObstaclePresenceEnum.None;
        //    return IsOn = false;
        //}

        _obstaclePresence = ObstaclePresenceEnum.None;
        return IsOn = false;
    }
    #endregion

    public bool DetectingObstacleBackward()
    {
        var RearLeftRayStartingPointPosition = CarRearSensorProvider.RearSensorData.RearLeftRayStartingPoint.position;
        var RearCentralRayStartingPointPosition = CarRearSensorProvider.RearSensorData.RearCentralRayStartingPoint.position;
        var RearRightRayStartingPointPosition = CarRearSensorProvider.RearSensorData.RearRightRayStartingPoint.position;

        _DetectingObstacleRL = Physics.Raycast(RearLeftRayStartingPointPosition, RayDirectionBackward, out RearLeftHitInfo, ParkingSensorSettings.PS_ActivationDistance);
        _DetectingObstacleRC = Physics.Raycast(RearCentralRayStartingPointPosition, RayDirectionBackward, out RearCentralHitInfo, ParkingSensorSettings.PS_ActivationDistance);
        _DetectingObstacleRR = Physics.Raycast(RearRightRayStartingPointPosition, RayDirectionBackward, out RearRightHitInfo, ParkingSensorSettings.PS_ActivationDistance);

        Debug.DrawRay(RearLeftRayStartingPointPosition, CarRearSensorProvider.RearSensorData.RearLeftRayStartingPoint.forward * ParkingSensorSettings.PS_ActivationDistance, _DetectingObstacleRL ? Color.red : Color.green);
        Debug.DrawRay(RearCentralRayStartingPointPosition, CarRearSensorProvider.RearSensorData.RearCentralRayStartingPoint.forward * ParkingSensorSettings.PS_ActivationDistance, _DetectingObstacleRC ? Color.red : Color.green);
        Debug.DrawRay(RearRightRayStartingPointPosition, CarRearSensorProvider.RearSensorData.RearRightRayStartingPoint.forward * ParkingSensorSettings.PS_ActivationDistance, _DetectingObstacleRR ? Color.red : Color.green);

        //da sistemare
        //if (_DetectingObstacleRL)
        //{
        //    if (RearLeftHitInfo.collider.GetComponent<Car>() || RearLeftHitInfo.collider.GetComponent<PrometeoCarController>() || RearLeftHitInfo.collider.GetComponent<Vehicle>())
        //        return true;
        //}
        //else if (_DetectingObstacleRC)
        //{
        //    if (RearCentralHitInfo.collider.GetComponent<Car>() || RearCentralHitInfo.collider.GetComponent<PrometeoCarController>() || RearCentralHitInfo.collider.GetComponent<Vehicle>())
        //        return true;
        //}
        //else if (_DetectingObstacleRR)
        //{
        //    if (RearRightHitInfo.collider.GetComponent<Car>() || RearRightHitInfo.collider.GetComponent<PrometeoCarController>() || RearRightHitInfo.collider.GetComponent<Vehicle>())
        //        return true;
        //}

        if (_DetectingObstacleRL || _DetectingObstacleRC || _DetectingObstacleRR)
            return true;

        return false;
    }

    public float DistanceToBackwardObstacle()
    {
        var RL_Distance = RearLeftHitInfo.distance;
        var RC_Distance = RearCentralHitInfo.distance;
        var RR_Distance = RearRightHitInfo.distance;

        if (RL_Distance > 0 && RC_Distance <= 0 && RR_Distance <= 0)
            return RL_Distance;
        if (RL_Distance <= 0 && RC_Distance > 0 && RR_Distance <= 0)
            return RC_Distance;
        if (RL_Distance <= 0 && RC_Distance <= 0 && RR_Distance > 0)
            return RR_Distance;

        if (RL_Distance > 0 && RC_Distance > 0 && RR_Distance <= 0)
            return Mathf.Min(RL_Distance, RC_Distance);
        if (RL_Distance > 0 && RC_Distance <= 0 && RR_Distance > 0)
            return Mathf.Min(RL_Distance, RR_Distance);
        if (RL_Distance <= 0 && RC_Distance > 0 && RR_Distance > 0)
            return Mathf.Min(RC_Distance, RR_Distance);

        else
            return Mathf.Min(RL_Distance, RC_Distance, RR_Distance);
    }


    public void ExecuteCarControl()
    {
        if (IsOn)
        {
            // if is not going reverse
            if (CarStateProvider.CarState != ICarStateProvider.CarStateEnum.GoingReverse)
            {
                _obstaclePresence = ObstaclePresenceEnum.None;
                //TurnOff();
            }
            else
            {
                if (DetectingObstacleBackward())
                {
                    _distanceToBackwardObstacle = DistanceToBackwardObstacle();

                    if (DistanceToBackwardObstacle() < ParkingSensorSettings.PS_ActivationDistance)
                    {
                        if (_distanceToBackwardObstacle <= ParkingSensorSettings.PS_FarDistance && _distanceToBackwardObstacle > ParkingSensorSettings.PS_MidDistance)
                            _obstaclePresence = ObstaclePresenceEnum.Far;
                        else if (_distanceToBackwardObstacle <= ParkingSensorSettings.PS_MidDistance && _distanceToBackwardObstacle > ParkingSensorSettings.PS_NearDistance)
                            _obstaclePresence = ObstaclePresenceEnum.Mid;
                        else if (_distanceToBackwardObstacle < ParkingSensorSettings.PS_NearDistance && _distanceToBackwardObstacle >= 0)
                            _obstaclePresence = ObstaclePresenceEnum.Near;
                    }
                    else
                        _obstaclePresence = ObstaclePresenceEnum.None;
                }
                else
                    _obstaclePresence = ObstaclePresenceEnum.None;
            }
        }
    }

    public IEnumerator EmitSound()
    {
        if (AudioSource == null || SoundsID == null)
            yield break;

        if (!SoundsID.TryGetValue("ParkingSensorBeep", out SoundID soundID))
            yield break;

        AudioSource.clip = soundID.GetAudioClip();

        while (true)
        {
            if (!IsOn || _obstaclePresence == ObstaclePresenceEnum.None)
            {
                if (AudioSource.isPlaying)
                    AudioSource.Stop();

                yield return null;
                continue;
            }

            // Calcola l'intervallo tra i beep in base alla distanza
            float interval = Mathf.Clamp(_distanceToBackwardObstacle * 0.3f, 0.05f, 1.0f);
            float beepDuration = AudioSource.clip.length; // Durata del beep

            if (_obstaclePresence == ObstaclePresenceEnum.Near)
            {
                // Se l'ostacolo è molto vicino, facciamo suonare il beep continuo senza fermarlo
                if (!AudioSource.isPlaying)
                {
                    AudioSource.loop = true;
                    AudioSource.Play();
                }
            }
            else
            {
                // Se il beep è più breve dell'intervallo, lo suoniamo normalmente
                if (beepDuration < interval)
                {
                    AudioSource.PlayOneShot(AudioSource.clip);
                }
                else
                {
                    // Se il beep è più lungo dell'intervallo, fermiamo il precedente per evitarne la sovrapposizione
                    AudioSource.Stop();
                    AudioSource.Play();
                }
            }

            yield return new WaitForSeconds(interval); // Aspetta l'intervallo calcolato
        }
    }

    // THIS
    //public IEnumerator EmitSound()
    //{
    //    AudioClip clip = null;
    //    AudioClip currentClip;
    //    long _playTimestamp = 0;
    //    long _elaspedTime = 0;

    //    while (true)
    //    {
    //        while (IsOn)
    //        {
    //            currentClip = SoundSource.clip;
    //            clip = null;

    //            switch (_obstaclePresence)
    //            {
    //                case ObstaclePresenceEnum.Near:
    //                    AudioClips.TryGetValue("Near Beep", out clip);
    //                    break;
    //                case ObstaclePresenceEnum.Mid:
    //                    AudioClips.TryGetValue("Mid Beep", out clip);
    //                    break;
    //                case ObstaclePresenceEnum.Far:
    //                    AudioClips.TryGetValue("Far Beep", out clip);
    //                    break;

    //                default:
    //                case ObstaclePresenceEnum.None:
    //                    _playTimestamp = 0;
    //                    _elaspedTime = 0;
    //                    break;
    //            }

    //            if (clip != null)
    //            {
    //                if (currentClip != clip || (!SoundSource.isPlaying || _obstaclePresence != ObstaclePresenceEnum.Near && ((float)(_elaspedTime / 1000)) >= _distanceToBackwardObstacle * 0.3f))
    //                {
    //                    SoundSource.clip = clip;
    //                    SoundSource.loop = _obstaclePresence == ObstaclePresenceEnum.Near;

    //                    SoundSource.Play();

    //                    _playTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    //                }

    //                _elaspedTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - _playTimestamp;
    //            }
    //            else
    //                SoundSource.Stop();

    //            yield return new WaitForEndOfFrame();
    //        }

    //        if (SoundSource.isPlaying)
    //            SoundSource.Stop();

    //        yield return null;
    //    }
    //}
}