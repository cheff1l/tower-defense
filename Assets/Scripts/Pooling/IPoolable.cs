public interface IPoolable
{
    void OnTakenFromPool();
    void OnReturnedToPool();
}
