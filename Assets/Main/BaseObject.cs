using UnityEngine;

public abstract class BaseObject : MonoBehaviour
{
    protected Transform CachedTransform { get; private set; }
    public bool IsInitialized { get; private set; }

    protected virtual void Awake()
    {
        CachedTransform = transform;
    }

    public virtual void InitializeBase()
    {
        IsInitialized = true;
    }

    protected virtual void Update()
    {
        if (!IsInitialized) {
            return;
        }

        Tick(Time.deltaTime);
    }

    protected virtual void Tick(float deltaTime)
    {
    }

    public virtual void Shutdown()
    {
        IsInitialized = false;
    }
}
