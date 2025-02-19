/*
MESSAGE FROM CREATOR: This script was coded by Mena. You can use it in your games either these are commercial or
personal projects. You can even add or remove functions as you wish. However, you cannot sell copies of this
script by itself, since it is originally distributed as a free product.
I wish you the best for your project. Good luck!

P.S: If you need more cars, you can check my other vehicle assets on the Unity Asset Store, perhaps you could find
something useful for your game. Best regards, Mena.
*/

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PrometeoCarController : MonoBehaviour
{
    //CAR SETUP

    //[Space(20)]
    //[Header("CAR SETUP")]
    //[Space(10)]

    [FoldoutGroup("CAR PARAMETERS"), Range(0, 190)] public int maxSpeed = 90; //The maximum speed that the car can reach in km/h.
    [FoldoutGroup("CAR PARAMETERS"), Range(10, 120)] public int maxReverseSpeed = 45; //The maximum speed that the car can reach while going on reverse in km/h.
    [FoldoutGroup("CAR PARAMETERS"), Range(1, 10)] public int accelerationMultiplier = 2; // How fast the car can accelerate. 1 is a slow acceleration and 10 is the fastest.
    
    [Space(10)]
    
    [FoldoutGroup("CAR PARAMETERS"), Range(10, 45)] public int maxSteeringAngle = 27; // The maximum angle that the tires can reach while rotating the steering wheel.
    [FoldoutGroup("CAR PARAMETERS"), Range(0.1f, 1f)] public float steeringSpeed = 0.5f; // How fast the steering wheel turns.
    
    [Space(10)]
    
    [FoldoutGroup("CAR PARAMETERS"), Range(100, 600)] public int brakeForce = 350; // The strength of the wheel brakes.
    [FoldoutGroup("CAR PARAMETERS"), Range(1, 10)] public int decelerationMultiplier = 2; // How fast the car decelerates when the user is not using the throttle.
    [FoldoutGroup("CAR PARAMETERS"), Range(1, 10)] public int handbrakeDriftMultiplier = 5; // How much grip the car loses when the user hit the handbrake.
    
    [Space(10)]
    [FoldoutGroup("CAR PARAMETERS")] public Vector3 bodyMassCenter; // This is a vector that contains the center of mass of the car. I recommend to set this value
                                                                    // in the points x = 0 and z = 0 of your car. You can select the value that you want in the y axis,
                                                                    // however, you must notice that the higher this value is, the more unstable the car becomes.
                                                                    // Usually the y value goes from 0 to 1.5.


    //WHEELS

    //[Header("WHEELS")]

    /*
    The following variables are used to store the wheels' data of the car. We need both the mesh-only game objects and wheel
    collider components of the wheels. The wheel collider components and 3D meshes of the wheels cannot come from the same
    game object; they must be separate game objects.
    */

    [FoldoutGroup("CAR SETUP")]

    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public GameObject frontLeftMesh;
    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public WheelCollider frontLeftCollider;

    [Space(10)]

    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public GameObject frontRightMesh;
    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public WheelCollider frontRightCollider;

    [Space(10)]

    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public GameObject rearLeftMesh;
    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public WheelCollider rearLeftCollider;

    [Space(10)]

    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public GameObject rearRightMesh;
    [FoldoutGroup("CAR SETUP/Mesh-Collider")] public WheelCollider rearRightCollider;

    //PARTICLE SYSTEMS

    //[Space(20)]
    //[Header("EFFECTS")]
    //[Space(10)]
    //The following variable lets you to set up particle systems in your car
    [FoldoutGroup("CAR SETUP/Effects")] public bool useEffects = false;

    // The following particle systems are used as tire smoke when the car drifts.
    [FoldoutGroup("CAR SETUP/Effects")] public ParticleSystem RLWParticleSystem;
    [FoldoutGroup("CAR SETUP/Effects")] public ParticleSystem RRWParticleSystem;

    [Space(10)]
    // The following trail renderers are used as tire skids when the car loses traction.
    [FoldoutGroup("CAR SETUP/Effects")] public TrailRenderer RLWTireSkid;
    [FoldoutGroup("CAR SETUP/Effects")] public TrailRenderer RRWTireSkid;

    //SPEED TEXT (UI)

    //[Space(20)]
    //[Header("UI")]
    //[Space(10)]
    //The following variable lets you to set up a UI text to display the speed of your car.
    [FoldoutGroup("CAR SETUP/UI")] public bool useUI = false;
    [FoldoutGroup("CAR SETUP/UI")] public Text carSpeedText; // Used to store the UI object that is going to show the speed of the car.

    //SOUNDS

    //[Space(20)]
    //[Header("Sounds")]
    //[Space(10)]
    //The following variable lets you to set up sounds for your car such as the car engine or tire screech sounds.
    [FoldoutGroup("CAR SETUP/Sounds")] public bool useSounds = false;
    [FoldoutGroup("CAR SETUP/Sounds")] public AudioSource carEngineStartUpSound;// by me
    [FoldoutGroup("CAR SETUP/Sounds")] public AudioSource carEngineSound; // This variable stores the sound of the car engine.
    [FoldoutGroup("CAR SETUP/Sounds")] public AudioSource tireScreechSound; // This variable stores the sound of the tire screech (when the car is drifting).
    [FoldoutGroup("CAR SETUP/Sounds")] float initialCarEngineSoundPitch; // Used to store the initial pitch of the car engine sound.

    //CONTROLS

    //[Space(20)]
    //[Header("CONTROLS")]
    //[Space(10)]
    //The following variables lets you to set up touch controls for mobile devices.
    [FoldoutGroup("CAR SETUP/Touch Controls")] public bool useTouchControls = false;
    [FoldoutGroup("CAR SETUP/Touch Controls")] public GameObject throttleButton;
    [FoldoutGroup("CAR SETUP/Touch Controls")] PrometeoTouchInput throttlePTI;
    [FoldoutGroup("CAR SETUP/Touch Controls")] public GameObject reverseButton;
    [FoldoutGroup("CAR SETUP/Touch Controls")] PrometeoTouchInput reversePTI;
    [FoldoutGroup("CAR SETUP/Touch Controls")] public GameObject turnRightButton;
    [FoldoutGroup("CAR SETUP/Touch Controls")] PrometeoTouchInput turnRightPTI;
    [FoldoutGroup("CAR SETUP/Touch Controls")] public GameObject turnLeftButton;
    [FoldoutGroup("CAR SETUP/Touch Controls")] PrometeoTouchInput turnLeftPTI;
    [FoldoutGroup("CAR SETUP/Touch Controls")] public GameObject handbrakeButton;
    [FoldoutGroup("CAR SETUP/Touch Controls")] PrometeoTouchInput handbrakePTI;

    //CAR DATA

    [Serializable]
    public class CarData
    {
        public bool isEngineOn;
        public float carSpeed; // Used to store the speed of the car.
        public bool isDrifting; // Used to know whether the car is drifting or not.
        public bool isTractionLocked; // Used to know whether the traction of the car is locked or not.

        public Rigidbody carRigidbody; // Stores the car's rigidbody.
        public float steeringAxis; // Used to know whether the steering wheel has reached the maximum value. It goes from -1 to 1.
        public float throttleAxis; // Used to know whether the throttle has reached the maximum value. It goes from -1 to 1.
        public float driftingAxis;
        public float localVelocityZ;
        public float localVelocityX;
        public bool deceleratingCar;
        public bool touchControlsSetup = false;

        /*
          The following variables are used to store information about sideways friction of the wheels (such as
          extremumSlip,extremumValue, asymptoteSlip, asymptoteValue and stiffness). We change this values to
          make the car to start drifting.
          */
        public WheelFrictionCurve FLwheelFriction;
        public float FLWextremumSlip;
        public WheelFrictionCurve FRwheelFriction;
        public float FRWextremumSlip;
        public WheelFrictionCurve RLwheelFriction;
        public float RLWextremumSlip;
        public WheelFrictionCurve RRwheelFriction;
        public float RRWextremumSlip;
    }
    [FoldoutGroup("CAR DATA"), HideLabel] public CarData carData;

    //List<Material> BrakeLights;

    // Start is called before the first frame update
    void Start()
    {
        //BrakeLights = GameObject.Find("Car/Exterior/Body").GetComponent<MeshRenderer>().materials.ToList();

        //In this part, we set the 'carData.carRigidbody' value with the Rigidbody attached to this
        //gameObject. Also, we define the center of mass of the car with the Vector3 given
        //in the inspector.
        carData.carRigidbody = gameObject.GetComponent<Rigidbody>();
        carData.carRigidbody.centerOfMass = bodyMassCenter;

        //Initial setup to calculate the drift value of the car. This part could look a bit
        //complicated, but do not be afraid, the only thing we're doing here is to save the default
        //friction values of the car wheels so we can set an appropiate drifting value later.
        carData.FLwheelFriction = new WheelFrictionCurve();
        carData.FLwheelFriction.extremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        carData.FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        carData.FLwheelFriction.extremumValue = frontLeftCollider.sidewaysFriction.extremumValue;
        carData.FLwheelFriction.asymptoteSlip = frontLeftCollider.sidewaysFriction.asymptoteSlip;
        carData.FLwheelFriction.asymptoteValue = frontLeftCollider.sidewaysFriction.asymptoteValue;
        carData.FLwheelFriction.stiffness = frontLeftCollider.sidewaysFriction.stiffness;
        carData.FRwheelFriction = new WheelFrictionCurve();
        carData.FRwheelFriction.extremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        carData.FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        carData.FRwheelFriction.extremumValue = frontRightCollider.sidewaysFriction.extremumValue;
        carData.FRwheelFriction.asymptoteSlip = frontRightCollider.sidewaysFriction.asymptoteSlip;
        carData.FRwheelFriction.asymptoteValue = frontRightCollider.sidewaysFriction.asymptoteValue;
        carData.FRwheelFriction.stiffness = frontRightCollider.sidewaysFriction.stiffness;
        carData.RLwheelFriction = new WheelFrictionCurve();
        carData.RLwheelFriction.extremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        carData.RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        carData.RLwheelFriction.extremumValue = rearLeftCollider.sidewaysFriction.extremumValue;
        carData.RLwheelFriction.asymptoteSlip = rearLeftCollider.sidewaysFriction.asymptoteSlip;
        carData.RLwheelFriction.asymptoteValue = rearLeftCollider.sidewaysFriction.asymptoteValue;
        carData.RLwheelFriction.stiffness = rearLeftCollider.sidewaysFriction.stiffness;
        carData.RRwheelFriction = new WheelFrictionCurve();
        carData.RRwheelFriction.extremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        carData.RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        carData.RRwheelFriction.extremumValue = rearRightCollider.sidewaysFriction.extremumValue;
        carData.RRwheelFriction.asymptoteSlip = rearRightCollider.sidewaysFriction.asymptoteSlip;
        carData.RRwheelFriction.asymptoteValue = rearRightCollider.sidewaysFriction.asymptoteValue;
        carData.RRwheelFriction.stiffness = rearRightCollider.sidewaysFriction.stiffness;

        // We save the initial pitch of the car engine sound.
        if (carEngineSound != null)
        {
            initialCarEngineSoundPitch = carEngineSound.pitch;
        }

        // We invoke 2 methods inside this script. carData.carSpeedUI() changes the text of the UI object that stores
        // the speed of the car and CarSounds() controls the engine and drifting sounds. Both methods are invoked
        // in 0 seconds, and repeatedly called every 0.1 seconds.
        if (useUI)
        {
            InvokeRepeating("carData.carSpeedUI", 0f, 0.1f);
        }
        else if (!useUI)
        {
            if (carSpeedText != null)
            {
                carSpeedText.text = "0";
            }
        }


        if (useSounds)
        {
            InvokeRepeating("CarSounds", 0f, 0.1f);
        }
        else if (!useSounds)
        {
            if (carEngineSound != null)
            {
                carEngineStartUpSound.Stop();
            }
            if (carEngineSound != null)
            {
                carEngineSound.Stop();
            }
            if (tireScreechSound != null)
            {
                tireScreechSound.Stop();
            }
        }

        if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

        if (useTouchControls)
        {
            if (throttleButton != null && reverseButton != null &&
            turnRightButton != null && turnLeftButton != null
            && handbrakeButton != null)
            {

                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
                carData.touchControlsSetup = true;

            }
            else
            {
                String ex = "Touch controls are not completely set up. You must drag and drop your scene buttons in the" +
                " PrometeoCarController component.";
                Debug.LogWarning(ex);
            }
        }

    }

    //private bool _brakeLights;

    // Update is called once per frame
    void Update()
    {
        /*if (_brakeLights)
        {
            BrakeLights[6].EnableKeyword("_EMISSION");
            _brakeLights = false;
        }
        else
        {
            BrakeLights[6].DisableKeyword("_EMISSION");
        }*/

        //CAR DATA

        // We determine the speed of the car.
        carData.carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        // Save the local velocity of the car in the x axis. Used to know if the car is drifting.
        carData.localVelocityX = transform.InverseTransformDirection(carData.carRigidbody.linearVelocity).x;
        // Save the local velocity of the car in the z axis. Used to know if the car is going forward or backwards.
        carData.localVelocityZ = transform.InverseTransformDirection(carData.carRigidbody.linearVelocity).z;

        //CAR PHYSICS

        /*
        The next part is regarding to the car controller. First, it checks if the user wants to use touch controls (for
        mobile devices) or analog input controls (WASD + Space).

        The following methods are called whenever a certain key is pressed. For example, in the first 'if' we call the
        method GoForward() if the user has pressed W.

        In this part of the code we specify what the car needs to do if the user presses W (throttle), S (reverse),
        A (turn left), D (turn right) or Space bar (handbrake).
        */
        
        
        //if (useTouchControls && carData.touchControlsSetup)
        //{

        //    if (throttlePTI.buttonPressed)
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        GoForward();
        //    }
        //    if (reversePTI.buttonPressed)
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        GoReverse();
        //    }

        //    if (turnLeftPTI.buttonPressed)
        //    {
        //        TurnLeft();
        //    }
        //    if (turnRightPTI.buttonPressed)
        //    {
        //        TurnRight();
        //    }
        //    if (handbrakePTI.buttonPressed)
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        Handbrake();
        //    }
        //    if (!handbrakePTI.buttonPressed)
        //    {
        //        RecoverTraction();
        //    }
        //    if ((!throttlePTI.buttonPressed && !reversePTI.buttonPressed))
        //    {
        //        ThrottleOff();
        //    }
        //    if ((!reversePTI.buttonPressed && !throttlePTI.buttonPressed) && !handbrakePTI.buttonPressed && !carData.deceleratingCar)
        //    {
        //        InvokeRepeating("DecelerateCar", 0f, 0.1f);
        //        carData.deceleratingCar = true;
        //    }
        //    if (!turnLeftPTI.buttonPressed && !turnRightPTI.buttonPressed && carData.steeringAxis != 0f)
        //    {
        //        ResetSteeringAngle();
        //    }

        //}
        //else
        //{

        //    if (Input.GetKey(KeyCode.W))
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        GoForward();
        //    }
        //    if (Input.GetKey(KeyCode.S))
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        GoReverse();
        //    }

        //    if (Input.GetKey(KeyCode.A))
        //    {
        //        TurnLeft();
        //    }
        //    if (Input.GetKey(KeyCode.D))
        //    {
        //        TurnRight();
        //    }
        //    if (Input.GetKey(KeyCode.Space))
        //    {
        //        CancelInvoke("DecelerateCar");
        //        carData.deceleratingCar = false;
        //        Handbrake();
        //    }
        //    if (Input.GetKeyUp(KeyCode.Space))
        //    {
        //        RecoverTraction();
        //    }
        //    if ((!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W)))
        //    {
        //        ThrottleOff();
        //    }
        //    if ((!Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.W)) && !Input.GetKey(KeyCode.Space) && !carData.deceleratingCar)
        //    {
        //        InvokeRepeating("DecelerateCar", 0f, 0.1f);
        //        carData.deceleratingCar = true;
        //    }
        //    if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) && carData.steeringAxis != 0f)
        //    {
        //        ResetSteeringAngle();
        //    }

        //}

        // We call the method AnimateWheelMeshes() in order to match the wheel collider movements with the 3D meshes of the wheels.
        AnimateWheelMeshes();

    }

    public void TurnEngineOn()
    {
        carData.isEngineOn = true;

        if(useSounds && carData.isEngineOn)
            carEngineStartUpSound.Play();

        // cose grafiche
    }

    // This method converts the car speed data from float to string, and then set the text of the UI carSpeedText with this value.
    public void carSpeedUI()
    {

        if (useUI)
        {
            try
            {
                float absolutecarSpeed = Mathf.Abs(carData.carSpeed);
                carSpeedText.text = Mathf.RoundToInt(absolutecarSpeed).ToString();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }

    }

    // This method controls the car sounds. For example, the car engine will sound slow when the car speed is low because the
    // pitch of the sound will be at its lowest point. On the other hand, it will sound fast when the car speed is high because
    // the pitch of the sound will be the sum of the initial pitch + the car speed divided by 100f.
    // Apart from that, the tireScreechSound will play whenever the car starts drifting or losing traction.
    public void CarSounds()
    {

        if (useSounds && carData.isEngineOn)
        {
            try
            {
                if (!carEngineStartUpSound.isPlaying)
                {
                    if (carEngineSound != null)
                    {
                        float engineSoundPitch = initialCarEngineSoundPitch + (Mathf.Abs(carData.carRigidbody.linearVelocity.magnitude) / 25f);
                        carEngineSound.pitch = engineSoundPitch;
                        carEngineSound.Play();
                    }
                    if ((carData.isDrifting) || (carData.isTractionLocked && Mathf.Abs(carData.carSpeed) > 12f))
                    {
                        if (!tireScreechSound.isPlaying)
                        {
                            tireScreechSound.Play();
                        }
                    }
                    else if ((!carData.isDrifting) && (!carData.isTractionLocked || Mathf.Abs(carData.carSpeed) < 12f))
                    {
                        tireScreechSound.Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
        else if (!useSounds)
        {
            if (carEngineSound != null && carEngineSound.isPlaying)
            {
                carEngineSound.Stop();
            }
            if (tireScreechSound != null && tireScreechSound.isPlaying)
            {
                tireScreechSound.Stop();
            }
        }


    }

    //
    //STEERING METHODS
    //

    //The following method turns the front car wheels to the left. The speed of this movement will depend on the steeringSpeed variable.
    public void TurnLeft()
    {
        carData.steeringAxis = carData.steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        if (carData.steeringAxis < -1f)
        {
            carData.steeringAxis = -1f;
        }
        var steeringAngle = carData.steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    //The following method turns the front car wheels to the right. The speed of this movement will depend on the steeringSpeed variable.
    public void TurnRight()
    {
        carData.steeringAxis = carData.steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        if (carData.steeringAxis > 1f)
        {
            carData.steeringAxis = 1f;
        }
        var steeringAngle = carData.steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    //The following method takes the front car wheels to their default position (rotation = 0). The speed of this movement will depend
    // on the steeringSpeed variable.
    public void ResetSteeringAngle()
    {
        if (carData.steeringAxis < 0f)
        {
            carData.steeringAxis = carData.steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        }
        else if (carData.steeringAxis > 0f)
        {
            carData.steeringAxis = carData.steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        }
        if (Mathf.Abs(frontLeftCollider.steerAngle) < 1f)
        {
            carData.steeringAxis = 0f;
        }
        var steeringAngle = carData.steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    // This method matches both the position and rotation of the WheelColliders with the WheelMeshes.
    void AnimateWheelMeshes()
    {
        try
        {
            Quaternion FLWRotation;
            Vector3 FLWPosition;
            frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
            frontLeftMesh.transform.position = FLWPosition;
            frontLeftMesh.transform.rotation = FLWRotation;

            Quaternion FRWRotation;
            Vector3 FRWPosition;
            frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
            frontRightMesh.transform.position = FRWPosition;
            frontRightMesh.transform.rotation = FRWRotation;

            Quaternion RLWRotation;
            Vector3 RLWPosition;
            rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
            rearLeftMesh.transform.position = RLWPosition;
            rearLeftMesh.transform.rotation = RLWRotation;

            Quaternion RRWRotation;
            Vector3 RRWPosition;
            rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
            rearRightMesh.transform.position = RRWPosition;
            rearRightMesh.transform.rotation = RRWRotation;
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex);
        }
    }

    //
    //ENGINE AND BRAKING METHODS
    //

    // This method apply positive torque to the wheels in order to go forward.
    public void GoForward()
    {
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car is losing traction, then the car will start emitting particle systems.
        if (Mathf.Abs(carData.localVelocityX) > 2.5f)
        {
            carData.isDrifting = true;
            DriftCarPS();
        }
        else
        {
            carData.isDrifting = false;
            DriftCarPS();
        }
        // The following part sets the throttle power to 1 smoothly.
        carData.throttleAxis = carData.throttleAxis + (Time.deltaTime * 3f);
        if (carData.throttleAxis > 1f)
        {
            carData.throttleAxis = 1f;
        }
        //If the car is going backwards, then apply brakes in order to avoid strange
        //behaviours. If the local velocity in the 'z' axis is less than -1f, then it
        //is safe to apply positive torque to go forward.
        if (carData.localVelocityZ < -1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.RoundToInt(carData.carSpeed) < maxSpeed)
            {
                //Apply positive torque in all wheels to go forward if maxSpeed has not been reached.
                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
            }
            else
            {
                // If the maxSpeed has been reached, then stop applying torque to the wheels.
                // IMPORTANT: The maxSpeed variable should be considered as an approximation; the speed of the car
                // could be a bit higher than expected.
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    // This method apply negative torque to the wheels in order to go backwards.
    public void GoReverse()
    {
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car is losing traction, then the car will start emitting particle systems.
        if (Mathf.Abs(carData.localVelocityX) > 2.5f)
        {
            carData.isDrifting = true;
            DriftCarPS();
        }
        else
        {
            carData.isDrifting = false;
            DriftCarPS();
        }
        // The following part sets the throttle power to -1 smoothly.
        carData.throttleAxis = carData.throttleAxis - (Time.deltaTime * 3f);
        if (carData.throttleAxis < -1f)
        {
            carData.throttleAxis = -1f;
        }
        //If the car is still going forward, then apply brakes in order to avoid strange
        //behaviours. If the local velocity in the 'z' axis is greater than 1f, then it
        //is safe to apply negative torque to go reverse.
        if (carData.localVelocityZ > 1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carData.carSpeed)) < maxReverseSpeed)
            {
                //Apply negative torque in all wheels to go in reverse if maxReverseSpeed has not been reached.
                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * carData.throttleAxis;
            }
            else
            {
                //If the maxReverseSpeed has been reached, then stop applying torque to the wheels.
                // IMPORTANT: The maxReverseSpeed variable should be considered as an approximation; the speed of the car
                // could be a bit higher than expected.
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    //The following function set the motor torque to 0 (in case the user is not pressing either W or S).
    public void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    // The following method decelerates the speed of the car according to the decelerationMultiplier variable, where
    // 1 is the slowest and 10 is the fastest deceleration. This method is called by the function InvokeRepeating,
    // usually every 0.1f when the user is not pressing W (throttle), S (reverse) or Space bar (handbrake).
    public void DecelerateCar()
    {
        if (Mathf.Abs(carData.localVelocityX) > 2.5f)
        {
            carData.isDrifting = true;
            DriftCarPS();
        }
        else
        {
            carData.isDrifting = false;
            DriftCarPS();
        }
        // The following part resets the throttle power to 0 smoothly.
        if (carData.throttleAxis != 0f)
        {
            if (carData.throttleAxis > 0f)
            {
                carData.throttleAxis = carData.throttleAxis - (Time.deltaTime * 10f);
            }
            else if (carData.throttleAxis < 0f)
            {
                carData.throttleAxis = carData.throttleAxis + (Time.deltaTime * 10f);
            }
            if (Mathf.Abs(carData.throttleAxis) < 0.15f)
            {
                carData.throttleAxis = 0f;
            }
        }
        carData.carRigidbody.linearVelocity = carData.carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));
        // Since we want to decelerate the car, we are going to remove the torque from the wheels of the car.
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
        // If the magnitude of the car's velocity is less than 0.25f (very slow velocity), then stop the car completely and
        // also cancel the invoke of this method.
        if (carData.carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carData.carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    // This function applies brake torque to the wheels according to the brake force given by the user.
    public void Brakes()
    {
        //_brakeLights = true;
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    // This function is used to make the car lose traction. By using this, the car will start drifting. The amount of traction lost
    // will depend on the handbrakeDriftMultiplier variable. If this value is small, then the car will not drift too much, but if
    // it is high, then you could make the car to feel like going on ice.
    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");
        // We are going to start losing traction smoothly, there is were our 'carData.driftingAxis' variable takes
        // place. This variable will start from 0 and will reach a top value of 1, which means that the maximum
        // drifting value has been reached. It will increase smoothly by using the variable Time.deltaTime.
        carData.driftingAxis = carData.driftingAxis + (Time.deltaTime);
        float secureStartingPoint = carData.driftingAxis * carData.FLWextremumSlip * handbrakeDriftMultiplier;

        if (secureStartingPoint < carData.FLWextremumSlip)
        {
            carData.driftingAxis = carData.FLWextremumSlip / (carData.FLWextremumSlip * handbrakeDriftMultiplier);
        }
        if (carData.driftingAxis > 1f)
        {
            carData.driftingAxis = 1f;
        }
        //If the forces aplied to the rigidbody in the 'x' asis are greater than
        //3f, it means that the car lost its traction, then the car will start emitting particle systems.
        if (Mathf.Abs(carData.localVelocityX) > 2.5f)
        {
            carData.isDrifting = true;
        }
        else
        {
            carData.isDrifting = false;
        }
        //If the 'carData.driftingAxis' value is not 1f, it means that the wheels have not reach their maximum drifting
        //value, so, we are going to continue increasing the sideways friction of the wheels until carData.driftingAxis
        // = 1f.
        if (carData.driftingAxis < 1f)
        {
            carData.FLwheelFriction.extremumSlip = carData.FLWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            frontLeftCollider.sidewaysFriction = carData.FLwheelFriction;

            carData.FRwheelFriction.extremumSlip = carData.FRWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            frontRightCollider.sidewaysFriction = carData.FRwheelFriction;

            carData.RLwheelFriction.extremumSlip = carData.RLWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            rearLeftCollider.sidewaysFriction = carData.RLwheelFriction;

            carData.RRwheelFriction.extremumSlip = carData.RRWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            rearRightCollider.sidewaysFriction = carData.RRwheelFriction;
        }

        // Whenever the player uses the handbrake, it means that the wheels are locked, so we set 'carData.isTractionLocked = true'
        // and, as a consequense, the car starts to emit trails to simulate the wheel skids.
        carData.isTractionLocked = true;
        DriftCarPS();

    }

    // This function is used to emit both the particle systems of the tires' smoke and the trail renderers of the tire skids
    // depending on the value of the bool variables 'carData.isDrifting' and 'carData.isTractionLocked'.
    public void DriftCarPS()
    {

        if (useEffects)
        {
            try
            {
                if (carData.isDrifting)
                {
                    RLWParticleSystem.Play();
                    RRWParticleSystem.Play();
                }
                else if (!carData.isDrifting)
                {
                    RLWParticleSystem.Stop();
                    RRWParticleSystem.Stop();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }

            try
            {
                if ((carData.isTractionLocked || Mathf.Abs(carData.localVelocityX) > 5f) && Mathf.Abs(carData.carSpeed) > 12f)
                {
                    RLWTireSkid.emitting = true;
                    RRWTireSkid.emitting = true;
                }
                else
                {
                    RLWTireSkid.emitting = false;
                    RRWTireSkid.emitting = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(ex);
            }
        }
        else if (!useEffects)
        {
            if (RLWParticleSystem != null)
            {
                RLWParticleSystem.Stop();
            }
            if (RRWParticleSystem != null)
            {
                RRWParticleSystem.Stop();
            }
            if (RLWTireSkid != null)
            {
                RLWTireSkid.emitting = false;
            }
            if (RRWTireSkid != null)
            {
                RRWTireSkid.emitting = false;
            }
        }

    }

    // This function is used to recover the traction of the car when the user has stopped using the car's handbrake.
    public void RecoverTraction()
    {
        carData.isTractionLocked = false;
        carData.driftingAxis = carData.driftingAxis - (Time.deltaTime / 1.5f);
        if (carData.driftingAxis < 0f)
        {
            carData.driftingAxis = 0f;
        }

        //If the 'carData.driftingAxis' value is not 0f, it means that the wheels have not recovered their traction.
        //We are going to continue decreasing the sideways friction of the wheels until we reach the initial
        // car's grip.
        if (carData.FLwheelFriction.extremumSlip > carData.FLWextremumSlip)
        {
            carData.FLwheelFriction.extremumSlip = carData.FLWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            frontLeftCollider.sidewaysFriction = carData.FLwheelFriction;

            carData.FRwheelFriction.extremumSlip = carData.FRWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            frontRightCollider.sidewaysFriction = carData.FRwheelFriction;

            carData.RLwheelFriction.extremumSlip = carData.RLWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            rearLeftCollider.sidewaysFriction = carData.RLwheelFriction;

            carData.RRwheelFriction.extremumSlip = carData.RRWextremumSlip * handbrakeDriftMultiplier * carData.driftingAxis;
            rearRightCollider.sidewaysFriction = carData.RRwheelFriction;

            Invoke("RecoverTraction", Time.deltaTime);

        }
        else if (carData.FLwheelFriction.extremumSlip < carData.FLWextremumSlip)
        {
            carData.FLwheelFriction.extremumSlip = carData.FLWextremumSlip;
            frontLeftCollider.sidewaysFriction = carData.FLwheelFriction;

            carData.FRwheelFriction.extremumSlip = carData.FRWextremumSlip;
            frontRightCollider.sidewaysFriction = carData.FRwheelFriction;

            carData.RLwheelFriction.extremumSlip = carData.RLWextremumSlip;
            rearLeftCollider.sidewaysFriction = carData.RLwheelFriction;

            carData.RRwheelFriction.extremumSlip = carData.RRWextremumSlip;
            rearRightCollider.sidewaysFriction = carData.RRwheelFriction;

            carData.driftingAxis = 0f;
        }
    }

}
