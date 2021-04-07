using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;                          //id
    public string username;                 //player 이름

    public CharacterController controller;  //player의 컨트롤러
    public Transform shootOrigin;           //총알방향
    public float gravity = -9.81f;          //중력가속도
    public float moveSpeed = 5f;            //움직임속도
    public float jumpSpeed = 5f;            //점프속도
    public float throwForce = 600f;         //폭탄 던지기속도
    public float hp;                        //체력
    public float maxHp = 100f;              //최대체력
    public int itemAmount = 0;              //아이템 소요개수
    public int maxItemAmount = 3;           //아이템 최대소요개수

    private bool[] inputs;
    private float yVelocity = 0;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        hp = maxHp;

        inputs = new bool[5];
    }

    private void Start()
    {
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    /// <summary>Processes player input and moves the player.</summary>
    public void FixedUpdate()
    {
        //채력이 0이면 움직이지 못함
        if(hp <= 0f)
        {
            return;
        }
        Vector2 _inputDirection = Vector2.zero;
        if (inputs[0])
        {
            _inputDirection.y += 1;
        }
        if (inputs[1])
        {
            _inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            _inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            _inputDirection.x += 1;
        }

        Move(_inputDirection);

    }

    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        transform.rotation = _rotation;
    }

    /// <summary>받은 데이터를 통해 플레이어의 움직임을 계산(점프 포함)</summary>
    /// <param name="_inputDirection"></param>
    private void Move(Vector2 _inputDirection)
    {
        /*Vector3 _forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
        //cross : 벡터외적 y축과 forward방향을 외적하면 오른쪽방향의 벡터가 나옴
        Vector3 _right = Vector3.Normalize(Vector3.Cross(_forward, new Vector3(0, 1, 0)));*/

        Vector3 _moveDirection = transform.right * _inputDirection.x + transform.forward * _inputDirection.y;
        _moveDirection *= moveSpeed;

        if (controller.isGrounded)
        {
            yVelocity = 0f;
            if (inputs[4]) //점프키
            {
                yVelocity = jumpSpeed;
            }
        }
        yVelocity += gravity;

        _moveDirection.y = yVelocity;
        controller.Move(_moveDirection);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    /// <summary>Physics.Raycast 를 통해 총알방향의 hit판정</summary>
    /// /// <param name="_viewDirection">총알의 방향</param>
    public void Shoot(Vector3 _viewDirection)
    {
        if (hp <= 0f)
        {
            return;
        }

        if (Physics.Raycast(shootOrigin.position, _viewDirection, out RaycastHit _hit, 25f))
        {
            if (_hit.collider.CompareTag("Player"))
            {
                _hit.collider.GetComponent<Player>().TakeDamage(50f);
            }
            /*else if (_hit.collider.CompareTag("Enemy"))
            {
                _hit.collider.GetComponent<Enemy>().TakeDamage(50f);
            }*/
        }
    }

    /// <summary>투척</summary>
    /// <param name="_viewDirection"></param>
    public void ThrowItem(Vector3 _viewDirection)
    {
        if (hp <= 0f)
        {
            return;
        }

        if (itemAmount > 0)
        {
            itemAmount--;
            NetworkManager.instance.InstantiateProjectile(shootOrigin).Initialize(_viewDirection, throwForce, id);
        }
    }

    /// <summary>받은 데이터를 통해 플레이어의 움직임을 계산(점프 포함)</summary>
    public void TakeDamage(float _damage)
    {
        //죽은 적을 맞췄을 때 그냥 반환
        if (hp <= 0f)
        {
            return;
        }

        hp -= _damage;
        //맞춘 적의 hp가 0일 때 해당 object를 정지하고 코루틴을 통해 리스폰실행
        if (hp <= 0f)
        {
            hp = 0f;
            controller.enabled = false;
            transform.position = new Vector3(0f, 25f, 0f);
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    /// <summary>리스폰</summary>
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        hp = maxHp;
        controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    /// <summary>아이템 소요개수 증가(최대소요개수 체크)</summary>
    /// <returns></returns>
    public bool AttemptPickupItem()
    {
        if (itemAmount >= maxItemAmount)
        {
            return false;
        }

        itemAmount++;
        return true;
    }

    /// <summary>아이템 소요개수 감소(0개인지 체크)</summary>
    /// <returns></returns>
    public bool AttemptThrowItem()
    {
        if (itemAmount <= 0 )
        {
            return false;
        }

        itemAmount--;
        return true;
    }
}