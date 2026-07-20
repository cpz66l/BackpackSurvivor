using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //获取组件
    private CharacterController cct;
    private InputReader ir;
    //移动或视角
    public float moveSpeed = 5f;
    private Vector3 moveDirection;
    private Vector3 lookDirection;
    public float rotateSpeed = 360f;
    void Start()
    {
        cct = GetComponent<CharacterController>();
        ir = GetComponent<InputReader>();
    }

    void Update()
    {
        //移动
        if (ir.moveVector2 != Vector2.zero)
        {
            moveDirection.Set(ir.moveVector2.x, 0, ir.moveVector2.y);
            moveDirection.Normalize();//归一化，避免斜向移动速度过快；
            moveDirection = Quaternion.Euler(0, -45, 0) * moveDirection;//旋转45度
            cct.Move(moveDirection * moveSpeed * Time.deltaTime);//使用CharacterController移动
        }
        //转向
        if (ir.worldPoint != Vector3.zero)
        {
            lookDirection = ir.worldPoint - transform.position;
            lookDirection.y = 0;//忽略y轴
            if (lookDirection.sqrMagnitude > 0.001f)//避免除以0
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed * Time.deltaTime
                );
            }

        }

    }
}
