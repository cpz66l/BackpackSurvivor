using BS.Core;
using UnityEngine;

namespace BS.Presentation
{
    public class DamageNumberPoolProvider : MonoBehaviour
    {
        [SerializeField] private ObjectPool damageNumberPool;

        public ObjectPool DamageNumberPool => damageNumberPool;
    }
}
