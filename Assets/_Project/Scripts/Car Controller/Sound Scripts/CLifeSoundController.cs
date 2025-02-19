using Ami.BroAudio;
using UnityEngine;

public class CLifeSoundController : MonoBehaviour
{
    [SerializeField] Car car;

    [SerializeField] private AudioSource audioSource;
    private AudioClip currentClip;

    [SerializeField] private SoundID engineSound;

    void Start()
    {
        car = GetComponent<Car>();
    }


    void Update()
    {
        PlayEngineSound();
    }

    public void PlayEngineSound()
    {
        if (audioSource.isActiveAndEnabled && audioSource != null)
        {
            if (currentClip == null || currentClip != engineSound.GetAudioClip())
                currentClip = engineSound.GetAudioClip();

            float newPitch = 0.3f + car.currentRPM / 2500;
            audioSource.pitch = newPitch;

            if (!audioSource.isPlaying && car.isEngineOn)
            {
                audioSource.clip = currentClip;
                audioSource.Play();
            }
        }
    }

    public void StopEngineSound()
    {
        if(currentClip == engineSound.GetAudioClip() && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
