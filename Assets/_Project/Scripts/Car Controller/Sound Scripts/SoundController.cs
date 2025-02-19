using Ezereal;
using Sirenix.OdinInspector;
using UnityEngine;

public class SoundController : MonoBehaviour
{
    [FoldoutGroup("Components")] public Car carController;
    [FoldoutGroup("Components")] public AudioSource engineSound;

    [FoldoutGroup("Debug")] public bool useSounds = false;
    [FoldoutGroup("Debug")] public bool isPlaying = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (useSounds)
        {
            isPlaying = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (useSounds)
        {
            if (carController != null && carController.rb != null && engineSound != null)
            {
                if (carController.isEngineOn && !isPlaying)
                {
                    engineSound.Play();
                    isPlaying = true;
                }
                else if (!carController.isEngineOn)
                {
                    engineSound.Stop();
                    isPlaying = false;
                }
                
                //Engine Pitch
                float engineSoundPitch = 0.8f + (Mathf.Abs(carController.currentRPM) / 2500f);
                engineSound.pitch = engineSoundPitch;

            }
        }
    }

    #region Turn On/Off engine sound
    public void TurnOnEngineSound()
    {
        if (useSounds)
        {
            if (engineSound != null)
                engineSound.Play();
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
