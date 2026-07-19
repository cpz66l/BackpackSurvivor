using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour
{
    public Vector2 moveVector2;
    private Vector2 mouseVector2;
    public Vector3 worldPoint;

    private Camera mainCam;
    private Plane groundPlane;
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
}
