using UnityEngine;
using Ami.BroAudio;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem.XR;

public class BroAudioScript : MonoBehaviour
{
    #region Components
    [FoldoutGroup("Components")] public Car carController;
    [FoldoutGroup("Components/Sounds"), SerializeField] SoundSource engineSound;
    #endregion

    #region Debug
    [FoldoutGroup("Debug")] public bool useSounds = false;
    [FoldoutGroup("Debug")] public bool isPlaying = false;
    #endregion

    void Start()
    {
        if (useSounds)
        {
            isPlaying = false;
        }
    }

    void Update()
    {
        if (useSounds)
        {
            if (carController != null && carController.rb != null)
            {
                if(engineSound != null)
                {
                    if (carController.isEngineOn && !isPlaying)
                    {
                        engineSound.Play(carController.transform);
                        isPlaying = true;
                    }
                    else if (!carController.isEngineOn)
                    {
                        engineSound.Stop();
                        isPlaying = false;
                    }

                    //Engine Pitch
                    float engineSoundPitch = 0.3f + (Mathf.Abs(carController.currentRPM) / 2500f);
                    engineSound.SetPitch(engineSoundPitch);
                }
            }
        }
    }

    #region Turn On/Off engine sound
    public void TurnOnEngineSound()
    {
        if (useSounds)
        {
            if (engineSound != null)
                engineSound.Play(carController.transform);
        }
    }

    public void TurnOffEngineSound()
    {
        if (useSounds)
        {
            if (engineSound != null)
                engineSound.Stop();
        }
    }
    #endregion
}
