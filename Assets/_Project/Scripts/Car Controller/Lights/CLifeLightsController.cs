using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

public class CLifeLightsController : MonoBehaviour
{
    [FoldoutGroup("SETUP")]
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] leftBlinkers;
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] rightBlinkers;
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] brakeLights;
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] reverseLights;
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] headlights;
    [FoldoutGroup("SETUP/Components"), SerializeField] GameObject[] runningLights;


    [FoldoutGroup("SETUP/Lights Parameters"), SerializeField] float blinkDelay = 0.5f;

    private bool leftBlinkerActive = false;
    private bool rightBlinkerActive = false;
    private Coroutine leftBlinkerCoroutine;
    private Coroutine rightBlinkerCoroutine;


    void Start()
    {
        TurnOffAllLights();
    }

    private void TurnOffAllLights()
    {
        SetBlinkersOff();
        TurnOffBrakeLights();
    }

    public void TurnOnLeftBlinkers()
    {
        StopAllCoroutines();
        SetBlinkersOff();

        rightBlinkerActive = false;
        leftBlinkerActive = !leftBlinkerActive;

        if (leftBlinkerActive)
        {
            leftBlinkerCoroutine = StartCoroutine(BlinkersController(leftBlinkers, leftBlinkerActive));
        }
    }

    public void TurnOnRightBlinkers()
    {
        StopAllCoroutines();
        SetBlinkersOff();

        leftBlinkerActive = false;
        rightBlinkerActive = !rightBlinkerActive;

        if (rightBlinkerActive)
        {
            rightBlinkerCoroutine = StartCoroutine(BlinkersController(rightBlinkers, rightBlinkerActive));
        }
    }

    public void TurnOffBlinkers()
    {
        StopAllCoroutines();
        SetBlinkersOff();
    }

    IEnumerator BlinkersController(GameObject[] blinkers, bool isActive)
    {
        while (isActive)
        {
            SetLight(blinkers, true);
            yield return new WaitForSeconds(blinkDelay);

            SetLight(blinkers, false);
            yield return new WaitForSeconds(blinkDelay);
        }
    }

    private void SetLight(GameObject[] lights, bool isActive)
    {
        if (isActive)
        {
            foreach (GameObject light in lights)
            {
                light.SetActive(true);
            }
        }
        else
        {
            foreach (GameObject light in lights)
            {
                light.SetActive(false);
            }
        }
    }

    private void SetBlinkersOff()
    {
        SetLight(leftBlinkers, false);
        SetLight(rightBlinkers, false);
    }

    private void SetBlinkersOn()
    {
        SetLight(leftBlinkers, true);
        SetLight(rightBlinkers, true);
    }

    public void TurnOffBrakeLights()
    {
        SetLight(brakeLights, false);
    }

    public void TurnOnBrakeLights()
    {
        SetLight(brakeLights, true);
    }

    public void TurnOffreverseLights()
    {
        SetLight(reverseLights, false);
    }

    public void TurnOnreverseLights()
    {
        SetLight(reverseLights, true);
    }

    public void TurnOffHeadlights()
    {
        SetLight(headlights, false);
    }

    public void TurnOnHeadlights()
    {
        SetLight(headlights, true);
    }

    public void TurnOffRunningLights()
    {
        SetLight(runningLights, false);
    }

    public void TurnOnRunningLights()
    {
        SetLight(runningLights, true);
    }

}
