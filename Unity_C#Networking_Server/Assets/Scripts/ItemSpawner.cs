using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    /// <summary>아이템을 저장할 Dictionary</summary>
    public static Dictionary<int, ItemSpawner> spawners = new Dictionary<int, ItemSpawner>();
    private static int nextSpawnerId = 1;   //다음으로 스폰될 아이템의 ID

    public int spawnerId;                   //스폰되는 아이템의 ID
    public bool hasItem = false;            //아이템이 존재하는지 체크
    public MeshRenderer itemModel;          //아이템 mesh

    private void Start()
    {
        itemModel = GetComponent<MeshRenderer>();
        hasItem = false;
        itemModel.enabled = hasItem;
        spawnerId = nextSpawnerId;
        nextSpawnerId++;
        spawners.Add(spawnerId, this);

        StartCoroutine(SpawnItem());
    }

    /*
    private void OnTriggerEnter(Collider other)
    {
        //아이템이 존재하고 아이템의 트리거범위에 player가 들어오면 아이템을 획득함
        if (hasItem && other.CompareTag("Player"))
        {
            Player _player = other.GetComponent<Player>();
            if (_player.AttemptPickupItem())
            {
                ItemPickedUp(_player.id);
            }
        }
    }
    */

    /// <summary>10초뒤에 아이템생성하고 클라이언트들에게 아이템스폰정보 전송</summary>
    /// <returns></returns>
    private IEnumerator SpawnItem()
    {
        yield return new WaitForSeconds(10f);

        hasItem = true;
        itemModel.enabled = hasItem;
        ServerSend.ItemSpawned(spawnerId);
    }

    /// <summary>아이템을 먹었다는 정보를 클라이언트에게 전송</summary>
    /// <param name="_byPlayer">아이템을 획득한 플레이어</param>
    public void ItemPickedUp(int _byPlayer)
    {
        if (Server.clients[_byPlayer].player.AttemptPickupItem())
        {
            hasItem = false;
            itemModel.enabled = hasItem;
            ServerSend.ItemPickedUp(spawnerId, _byPlayer);

            //StartCoroutine(SpawnItem());
        }
    }

    /// <summary>아이템을 버렸다는 정보를 클라이언트에게 전송</summary>
    /// <param name="_byPlayer">아이템을 획득한 플레이어</param>
    /// <param name="_position">아이템을 버릴 위치</param>
    public void ItemThrow(int _byPlayer, Vector3 _position)
    {
        if (Server.clients[_byPlayer].player.AttemptThrowItem())
        {
            hasItem = true;
            itemModel.enabled = hasItem;
            this.transform.position = _position;
            ServerSend.ItemThrow(spawnerId, _byPlayer, _position);
        }
    }
}
