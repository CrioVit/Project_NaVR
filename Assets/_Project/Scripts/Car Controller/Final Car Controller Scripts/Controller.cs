using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{   
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Scripts"), SerializeField] private Car car;
    [FoldoutGroup("SETUP/Scripts"), SerializeField] private CLifeAutopilot autopilot;

    #region Input Action
    [FoldoutGroup("SETUP/Input Action"), SerializeField] InputActionAsset inputActionAsset;
    InputAction autopilotActivationAction;
    #endregion

    [FoldoutGroup("DEBUG"), SerializeField] bool useAutopilot = false;

    void Start()
    {
        if (autopilot == null) autopilot = GetComponent<CLifeAutopilot>();
        if (car == null) car = GetComponent<Car>();

        if (autopilot != null)
        {
            autopilot.showLineRenderer = true;
            autopilot.ShowGizmos = true;
            autopilot.NavMeshLayers[0] = "Walkable";
        }
    }

    void Update()
    {
    }

    void OnEnable()
    {
        // Find Action with Input Action Asset
        var actionMap = inputActionAsset.FindActionMap("CLifeCarController");

        autopilotActivationAction = actionMap.FindAction("AutopilotActivation");

        // Register Callback Events
        autopilotActivationAction.performed += AutopilotActivation_action;

        // Enable Action
        autopilotActivationAction.Enable();
    }

    void OnDisable()
    {
        //Register Callback Events
        autopilotActivationAction.performed -= AutopilotActivation_action;

        // Enable Action
        autopilotActivationAction.Disable();
    }

    private void AutopilotActivation_action(InputAction.CallbackContext obj)
    {
        if (!useAutopilot)
        {
            autopilot.EnableAutopilot();
            useAutopilot = true;
        }
        else
        {
            autopilot.DisableAutopilot();
            useAutopilot = false;
        }
    }
}
