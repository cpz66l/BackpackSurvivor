using UnityEngine;

namespace BS.Core
{
    public class MapBounds : MonoBehaviour
    {
        [SerializeField] private float radius = 40f;
        public float Radius => radius;
        public Vector3 Center => transform.position;// 自己的位置=圆心（白嫖的配置）


        // 地图知识全部归地图管：
        public Vector3 GetRandomPoint()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
            Vector3 randomPoint = transform.position + new Vector3(offset.x, 0, offset.y);
            randomPoint.y = 0;
            return randomPoint;
        }
        public bool IsInside(Vector3 pos) 
        {
            bool inside = false;
            pos.y = 0;
            float distance = (Center - pos).sqrMagnitude;
            if(distance < radius * radius)
            {
                inside = true;
                return inside;
            }
            return inside;

        }
        public Vector3 ClampToInside(Vector3 pos) 
        {
            Vector3 offset = pos - Center;
            offset.y = 0f;                                  // 拍平：边界是二维概念，只算水平距离

            if (offset.sqrMagnitude <= radius * radius)
                return pos;                                 // 没出界，原样返回（早返回，不嵌套）

            Vector3 clamped = Center + offset.normalized * radius;  // 方向不变，长度压到半径
            clamped.y = pos.y;                              // y 还回去：高度是物理的事，地图不管
            return clamped;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
