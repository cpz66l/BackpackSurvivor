using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BS.GamePlay.Player
{
    public class InputReader : MonoBehaviour
    {
        public event Action OnInteract;
        public event Action OnRotate;
        public event Action OnPause;
        public event Action OnOpenBag;
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
        //获取鼠标射线在指定高度水平面上的交点，用于让枪口高度的瞄准与屏幕鼠标位置一致。
        public bool TryGetMousePointOnPlane(float y, out Vector3 point)
        {
            point = default;

            if (mainCam == null)
                return false;

            Ray ray = mainCam.ScreenPointToRay(mouseVector2);
            Plane plane = new Plane(Vector3.up, new Vector3(0f, y, 0f));

            if (!plane.Raycast(ray, out float enter))
                return false;

            point = ray.GetPoint(enter);
            return true;
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
        public void Interact(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnInteract?.Invoke();
        }

        public void Rotate(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnRotate?.Invoke();
        }

        public void Pause(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnPause?.Invoke();
        }

        public void OpenBag(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) OnOpenBag?.Invoke();
        }
    }
}
