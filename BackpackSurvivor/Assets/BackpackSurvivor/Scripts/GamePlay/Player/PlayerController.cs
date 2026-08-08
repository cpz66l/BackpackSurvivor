using BS.Core;
using BS.GamePlay.Stats;
using UnityEngine;

namespace BS.GamePlay.Player
{
    public class PlayerController : MonoBehaviour
    {
        //获取引用
        private CharacterController cct;
        private InputReader ir;
        private PlayerRunStats stats;
        //移动或视角
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector3 moveDirection;
        [SerializeField] private Vector3 lookDirection;
        [SerializeField] private float rotateSpeed = 360f;
        [SerializeField] private Transform bodyPivot;

        private const float CameraYawOffset = -45f; // 等距相机的固定偏航角
        private MapBounds mapBounds;

        private void Awake()
        {
            cct = GetComponent<CharacterController>();
            ir = GetComponent<InputReader>();
            mapBounds = FindAnyObjectByType<MapBounds>();
            stats = GetComponent<PlayerRunStats>();
        }
        void Start()
        {
            //获取子模型transform
            bodyPivot = transform.Find("Model");
        }

        void Update()
        {
            //移动
            if (ir.moveVector2 != Vector2.zero)
            {
                moveDirection.Set(ir.moveVector2.x, 0, ir.moveVector2.y);
                moveDirection.Normalize();//归一化，避免斜向移动速度过快；
                moveDirection = Quaternion.Euler(0, CameraYawOffset, 0) * moveDirection;//旋转45度
                float finalMoveSpeed = moveSpeed * stats.MoveSpeedMultiplier;
                cct.Move(moveDirection * finalMoveSpeed * Time.deltaTime);//使用CharacterController移动
                transform.position = mapBounds.ClampToInside(transform.position);//将玩家位置限制在地图内
            }
            //转向
            if (ir.TryGetMousePointOnPlane(bodyPivot.position.y, out Vector3 aimPoint))
            {
                lookDirection = aimPoint - bodyPivot.position;
                lookDirection.y = 0f;

                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                    bodyPivot.rotation = Quaternion.RotateTowards(
                        bodyPivot.rotation,
                        targetRotation,
                        rotateSpeed * Time.deltaTime
                    );
                }
            }

        }
    }
}
