using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class Intersection_4Way : MonoBehaviour
{
    public enum State
    {
        Even,
        Odd
    }

    [FoldoutGroup("Settings")] public float greenDuration = 6;
    [FoldoutGroup("Settings")] public float yellowDuration = 2;
    [FoldoutGroup("Settings")] public float redDuration = 8;
    [FoldoutGroup("Settings")] public List<TrafficLight> trafficLights;

    [FoldoutGroup("Debug"), DisableInPlayMode] public State state;
    [FoldoutGroup("Debug"), ReadOnly] public float timer = 0;
      
    void Start()
    {
        for (int i = 0; i < trafficLights.Count; i++)
        {
            trafficLights[i].setDuration(greenDuration, yellowDuration, redDuration);
        }

        SetIntersectionState(state, force:true);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;        
    }

    public void SetIntersectionState(State state, bool force = false)
    {
        for (int i = 0; i < trafficLights.Count; i++)
        {
            if (state == State.Even)
            {
                if (i % 2 == 0)
                    ChangeTrafficLightState(trafficLights[i], TrafficLight.State.Green, force);
                else
                    ChangeTrafficLightState(trafficLights[i], TrafficLight.State.Red, force);
            }
            else
            {
                if (i % 2 != 0)
                    ChangeTrafficLightState(trafficLights[i], TrafficLight.State.Green, force);
                else
                    ChangeTrafficLightState(trafficLights[i], TrafficLight.State.Red, force);
            }
        }
    }

    private void ChangeTrafficLightState(TrafficLight trafficLight, TrafficLight.State state, bool force = false)
    {
        if (trafficLight.state != state || force)
            trafficLight.ChangeState(state);
    }
}
