using UnityEngine;
using UnityEngine.InputSystem;

namespace BS.GamePlay.Player
{
    public class InputReader : MonoBehaviour
    {
        //外部读取属性
        public Vector2 moveVector2 { get; private set; }
        public Vector3 worldPoint { get; private set; }
        public bool AttackHeld { get; private set; }

        //内部处理字段
        private Camera mainCam;
        private Plane groundPlane;
        private Vector2 mouseVector2;
        void Start()
        {
            mainCam = Camera.main;
            groundPlane = new Plane(Vector3.up, Vector3.zero);
        }
        void Update()
        {
            //摄像机发射Ray射线，获取鼠标在地面上的位置
            Ray ray = mainCam.ScreenPointToRay(mouseVector2);
            if (groundPlane.Raycast(ray, out float enter))
            {
                worldPoint = ray.GetPoint(enter);
            }
        }
        public void Move(InputAction.CallbackContext ctx)
        {
            moveVector2 = ctx.ReadValue<Vector2>();
        }
        public void MousePosition(InputAction.CallbackContext ctx)
        {
            mouseVector2 = ctx.ReadValue<Vector2>();

        }
        public void Attack(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) AttackHeld = true;
            else if (ctx.canceled) AttackHeld = false;
        }
    }
}
