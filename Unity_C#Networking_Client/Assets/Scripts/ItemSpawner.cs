using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public int spawnerId;                   //아이템ID
    public bool hasItem;                    //아이템존재여부
    public MeshRenderer itemModel;          //아이템 mesh

    public float itemRotationSpeed = 50f;   //아이템 회전속도
    public float itemBobSpeed = 2f;         //아이템 이동속도
    private Vector3 basePosition;           //아이템 기본포지션

    private void Update()
    {
        //아이템이 존재하면 회전, 이동
        if (hasItem)
        {
            transform.Rotate(Vector3.up, itemRotationSpeed * Time.deltaTime, Space.World);
            transform.position = basePosition + new Vector3(0f, 0.25f * Mathf.Sin(Time.time * itemBobSpeed), 0f);
        }
    }

    /// <summary>아이템초기화</summary>
    /// <param name="_spawnerId">아이템ID</param>
    /// <param name="_hasItem">아이템 존재여부</param>
    public void Initialize(int _spawnerId, bool _hasItem)
    {
        spawnerId = _spawnerId;
        hasItem = _hasItem;
        itemModel.enabled = _hasItem;

        basePosition = transform.position;
    }

    /// <summary>아이템 생성됨 > 맵에 표시</summary>
    public void ItemSpawned()
    {
        hasItem = true;
        itemModel.enabled = true;
    }

    /// <summary>아이템 획득 > 맵에 표시X</summary>
    public void ItemPickedUp()
    {
        hasItem = false;
        itemModel.enabled = false;
    }
}
