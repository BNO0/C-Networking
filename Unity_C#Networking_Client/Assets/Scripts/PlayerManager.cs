using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;                 //player 이름
    public float health;                    //체력
    public float maxHealth = 100f;          //최대체력
    public int itemCount = 0;               //아이템 소요개수
    public MeshRenderer model;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;
    }

    /// <summary>HP 세팅</summary>
    /// <param name="_health"></param>
    public void SetHealth(float _health)
    {
        health = _health;

        //HP가 0이 되면 죽음
        if (health <= 0f)
        {
            Die();
        }
    }

    //3Dobject renderer off
    public void Die()
    {
        model.enabled = false;
    }

    //3Dobject renderer on, 체력 리셋
    public void Respawn()
    {
        model.enabled = true;
        SetHealth(maxHealth);
    }
}
