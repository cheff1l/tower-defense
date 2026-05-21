using UnityEngine;

public sealed class PoolMember : MonoBehaviour
{
    public ObjectPool OriginPool { get; set; }

    public void Release()
    {
        if (OriginPool != null)
        {
            OriginPool.Return(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }
}
