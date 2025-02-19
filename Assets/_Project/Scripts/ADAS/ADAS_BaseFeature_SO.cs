using Sirenix.OdinInspector;
using UnityEngine;
using System;
using AYellowpaper.SerializedCollections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Ami.BroAudio;

[Serializable]
[CreateAssetMenu(fileName = "ADAS", menuName = "Scriptable Objects/ADAS_BaseFeature", order = 1)]
[GUIColor("@$value.SetGUIColor($value)")]
public abstract class ADAS_BaseFeature : ScriptableObject
{
    [Serializable]
    public abstract class ADAS_Settings { }

    public string Name;
    public string Description;

    public Color SetGUIColor(ADAS_BaseFeature feature)
    {
        if (feature is IActivable activable)
        {
            if (activable.IsOn)
                return Color.green;
            else
                return Color.white;
        }

        if (feature is IActivableByInputAction activableByInputAction)
        {
            if (activableByInputAction.IsOn)
                return Color.green;
            else
                return Color.white;
        }

        return Color.white;
    }

    //public Color SetGUIColor(ADAS_BaseFeature feature)
    //{
    //    return feature ? feature is IActivable activable ? activable.IsOn ? Color.green : Color.white : Color.green : Color.white;
    //}

    //public Color SetGUIColor(ADAS_BaseFeature feature)
    //{
    //    return feature ? feature is IActivableByInputAction activable ? activable.IsOn ? Color.green : Color.white : Color.green : Color.white;
    //}
}

public interface IInitializable
{
    public bool IsInitialized { get; set; }
    public void Init(bool reset = false);
}

public interface IActivable
{
    public KeyCode ActivationKey { get; set; }
    public bool IsOn { get; set; }
    public bool TurnOn();
    public bool TurnOff();
}

public interface IActivableByInputAction
{
    public InputActionAsset InputActionAsset { get; set; }
    public InputAction ActivationAction { get; set; }

    /// <summary>
    /// Property that returns the bindings formatted as "Key (Device)"
    /// </summary>
    public List<string> Bindings { get; }

    /// <summary>
    /// Default method that collects only the ActivationAction binding
    /// </summary>
    public List<string> GetBindings()
    {
        return GetBindings(new List<(string, InputAction)> { ("Activation", ActivationAction) });
    }

    /// <summary>
    /// Method that collects only the specified bindings and formats them
    /// </summary>
    public List<string> GetBindings(List<(string actionName, InputAction action)> actions)
    {
        List<string> bindingsList = new List<string>();

        foreach (var (actionName, action) in actions)
        {
            if (action != null)
            {
                foreach (var binding in action.bindings)
                {
                    string formattedBinding = FormatBinding(binding.effectivePath);
                    bindingsList.Add($"{actionName}: {formattedBinding}");
                }
            }
        }
        return bindingsList;
    }

    /// <summary>
    /// Formats the binding from "<Device>/Key" to "Key (Device)"
    /// </summary>
    private static string FormatBinding(string path)
    {
        if (string.IsNullOrEmpty(path)) 
            return "N/A";

        string[] parts = path.Split('/');
        if (parts.Length != 2) 
            return path;

        string device = parts[0].Trim('<', '>');
        string key = parts[1];

        return $"{key.ToUpper()} ({device})";
    }

    public bool IsOn { get; set; }

    public void OnEnable();
    public void OnDisable();

    public void Activation_action(InputAction.CallbackContext obj);
    public bool TurnOn();
    public bool TurnOff();
}

public interface IInputActionHandler
{
    public InputActionAsset InputActionAsset { get; set; }

    /// <summary>
    /// Property that returns the bindings formatted as "Key (Device)"
    /// </summary>
    public List<string> Bindings { get; }

    /// <summary>
    /// Method that collects only the specified bindings and formats them
    /// </summary>
    public List<string> GetBindings(List<(string actionName, InputAction action)> actions)
    {
        List<string> bindingsList = new List<string>();

        foreach (var (actionName, action) in actions)
        {
            if (action != null)
            {
                foreach (var binding in action.bindings)
                {
                    string formattedBinding = FormatBinding(binding.effectivePath);
                    bindingsList.Add($"{actionName}: {formattedBinding}");
                }
            }
        }
        return bindingsList;
    }

    /// <summary>
    /// Formats the binding from "<Device>/Key" to "Key (Device)"
    /// </summary>
    private static string FormatBinding(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "N/A";

        string[] parts = path.Split('/');
        if (parts.Length != 2)
            return path;

        string device = parts[0].Trim('<', '>');
        string key = parts[1];

        return $"{key.ToUpper()} ({device})";
    }

    public void OnEnable();
    public void OnDisable();
}

public interface IPlayerLoopConsumer
{
    public PlayerLoopEnabler Enabler { get; set; }
    public GameObject GameObject { get; }
    public Transform Transform { get; }

    public void RegisterCallbacks();
    public void UnregisterCallbacks();
}

//public interface ICarDataConsumer
//{
//    public ICarDataProvider CarDataProvider { get; set; }
//}

public interface ICarController
{
    public enum CarControlTypeEnum
    {
        OneTime,
        Continuous
    }

    public ICarControllerProvider CarControllerProvider { get; set; }
    public ICarControllerExecutor CarControllerExecutor { get; set; }

    public CarControlTypeEnum CarControlType { get; set; }

    public void ExecuteCarControl();
}

public interface ICarStateConsumer
{
    public ICarStateProvider CarStateProvider { get; set; }
}

public interface ICarFrontSensorConsumer
{
    public ICarFrontSensorProvider CarFrontSensorProvider { get; set; }
}

public interface ICarRearSensorConsumer
{
    public ICarRearSensorProvider CarRearSensorProvider { get; set; }
}

//public interface ICarSoundProvider
//{
//    public SerializedDictionary<string, AudioClip> AudioClips { get; set; }

//    public IEnumerator EmitSound();

//    //public void PlaySound(AudioSource audioSource, string audioClip, float startDelay = 0f, bool loop = false, int loopAmount = 0, float delayBetweenLoop = 0f);
//}

public interface ICarSoundProvider
{
    public AudioSource AudioSource { get; set; }
    public SerializedDictionary<string, SoundID> SoundsID { get; set; }
    public bool PlaySound { get; set; }

    public IEnumerator EmitSound();
}