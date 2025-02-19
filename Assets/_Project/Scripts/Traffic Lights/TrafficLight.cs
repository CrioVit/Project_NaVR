using Sirenix.OdinInspector;
using UnityEngine;
using System;

public class TrafficLight : MonoBehaviour
{
    public enum State
    {
        Green,
        Yellow,
        Red
    }

    [FoldoutGroup("Settings"), DisableInPlayMode] public MeshRenderer greenSphere;
    [FoldoutGroup("Settings"), DisableInPlayMode] public MeshRenderer yellowSphere;
    [FoldoutGroup("Settings"), DisableInPlayMode] public MeshRenderer redSphere;

    [FoldoutGroup("Settings"), DisableInPlayMode] public float greenDuration = 6;
    [FoldoutGroup("Settings"), DisableInPlayMode] public float yellowDuration = 2;
    [FoldoutGroup("Settings"), DisableInPlayMode] public float redDuration = 8;

    [FoldoutGroup("Debug"), ReadOnly] public State state;
    [FoldoutGroup("Debug"), ReadOnly] public float timer = 0;
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public float remainingTime => (state == State.Green ? greenDuration : state == State.Yellow ? yellowDuration : redDuration) - timer;

    private void Update()
    {
        timer += Time.deltaTime;

        if (remainingTime <= 0)
            ChangeState(state = (State)(((int)state + 1) % Enum.GetNames(typeof(State)).Length));
    }

    public void ChangeState(State state)
    {
        this.state = state;
        timer = 0;

        switch (state)
        {
            case State.Green:
                ToggleEmission(greenSphere.material, true);
                ToggleEmission(yellowSphere.material, false);
                ToggleEmission(redSphere.material, false);
                break;

            case State.Yellow:
                ToggleEmission(greenSphere.material, false);
                ToggleEmission(yellowSphere.material, true);
                ToggleEmission(redSphere.material, false);
                break;

            case State.Red:
                ToggleEmission(greenSphere.material, false);
                ToggleEmission(yellowSphere.material, false);
                ToggleEmission(redSphere.material, true);
                break;
        }
    }

    private void ToggleEmission(Material material, bool on)
    {
        if (on)
            material.EnableKeyword("_EMISSION");
        else
            material.DisableKeyword("_EMISSION");
    }

    public void setDuration(float greenDuration, float yellowDuration, float redDuration)
    {
        this.greenDuration = greenDuration;
        this.yellowDuration = yellowDuration;
        this.redDuration = redDuration; 
    }
}
