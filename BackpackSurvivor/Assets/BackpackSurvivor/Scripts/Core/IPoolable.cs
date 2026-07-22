using UnityEngine;

namespace BS.Core
{
    public interface IPoolable
    {
        void SetPool(ObjectPool pool);
        void OnGetFromPool();
        void OnReturnPool();
    }
}
