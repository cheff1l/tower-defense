using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField, Min(0)] private int prewarmCount = 16;

    private readonly Queue<GameObject> available = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            available.Enqueue(CreateInstance());
        }
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject instance = available.Count > 0 ? available.Dequeue() : CreateInstance();
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);

        foreach (IPoolable poolable in instance.GetComponents<IPoolable>())
        {
            poolable.OnTakenFromPool();
        }

        return instance;
    }

    public void Return(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        foreach (IPoolable poolable in instance.GetComponents<IPoolable>())
        {
            poolable.OnReturnedToPool();
        }

        instance.SetActive(false);
        available.Enqueue(instance);
    }

    private GameObject CreateInstance()
    {
        GameObject instance = Instantiate(prefab, transform);
        instance.SetActive(false);

        PoolMember member = instance.GetComponent<PoolMember>();
        if (member == null)
        {
            member = instance.AddComponent<PoolMember>();
        }

        member.OriginPool = this;
        return instance;
    }
}
