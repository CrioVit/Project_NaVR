using UnityEngine;
using UnityEngine.Events;

public class PlayerLoopEnabler : MonoBehaviour
{
    private void Awake()
    {
        OnAwakeCallback.Invoke();
    }

    private void Start()
    {
        OnStartCallback.Invoke();
    }

    private void Update()
    {
        OnUpdateCallback.Invoke();
    }

    private void FixedUpdate()
    {
        OnFixedUpdateCallback.Invoke();
    }

    private void OnEnable()
    {
        OnEnableCallback.Invoke();
    }

    private void OnDisable()
    {
        OnDisableCallback.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionEnterCallback.Invoke(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        OnCollisionStayCallback.Invoke(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        OnCollisionExitCallback.Invoke(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterCallback.Invoke(other);
    }

    private void OnTriggerStay(Collider other)
    {
        OnTriggerStayCallback.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerExitCallback.Invoke(other);
    }

    private void OnDrawGizmos()
    {
        OnDrawGizomosCallback.Invoke();
    }

    private void OnDestroy()
    {
        OnDestroyCallback.Invoke();
    }

    public UnityEvent OnAwakeCallback { get; set; }
    public UnityEvent OnStartCallback { get; set; }
    public UnityEvent OnUpdateCallback { get; set; }
    public UnityEvent OnFixedUpdateCallback { get; set; }
    public UnityEvent OnEnableCallback { get; set; }
    public UnityEvent OnDisableCallback { get; set; }
    public UnityEvent<Collision> OnCollisionEnterCallback { get; set; }
    public UnityEvent<Collision> OnCollisionStayCallback { get; set; }
    public UnityEvent<Collision> OnCollisionExitCallback { get; set; }
    public UnityEvent<Collider> OnTriggerEnterCallback { get; set; }
    public UnityEvent<Collider> OnTriggerStayCallback { get; set; }
    public UnityEvent<Collider> OnTriggerExitCallback { get; set; }
    public UnityEvent OnDrawGizomosCallback { get; set; }
    public UnityEvent OnDestroyCallback { get; set; }
}
