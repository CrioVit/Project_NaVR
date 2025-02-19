using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public List<GameObject> cars = new List<GameObject>();
    private GameObject carModel;

    public int numberOfCars;
    public int spwaningTime;
    public bool isReadyToSpawn = false;

    void Start()
    {
        StartCoroutine(SpawnCars());
    }

    IEnumerator SpawnCars()
    {
        int i = 0;

        while (i < numberOfCars)
        {
            if (isReadyToSpawn)
            {
                carModel = cars[Random.Range(0, cars.Count)];
            
                carModel.transform.position = transform.position;
                carModel.transform.rotation = transform.rotation;

                // Activate autopilot
                Car car = carModel.GetComponent<Car>();
                car.isStarted = true;
                car.isEngineOn = true;
                car.gearType = Car.GearType.Automatic;
                car.automaticGear = Car.AutomaticGear.Drive;
                car.currentGear = 1;

                CLifeAutopilot autopilot = carModel.GetComponent<CLifeAutopilot>();
                autopilot.CustomDestination = null;

                Instantiate(carModel);

                i++;

                yield return new WaitForSeconds(spwaningTime);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }

        yield return 0;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponent<Car>())
        {
            isReadyToSpawn = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<Car>())
        {
            isReadyToSpawn = true;
        }
    }
}
