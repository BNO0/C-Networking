using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    /// <summary>서버로 부터 welcome data를 받음</summary>
    public static void Welcome(Packet _packet)
    {
        string _msg = _packet.ReadString();
        int _myId = _packet.ReadInt();

        Debug.Log($"Message from server: {_msg}");
        Client.instance.myId = _myId;
        ClientSend.WelcomeReceived();

        ///Client.instance.tcp.socket.Client : 클라이언트의 소켓
        ///클라이언트의 소켓의 endpoint의 포트를 통해 udp 인스턴스에 연결
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void SpawnPlayer(Packet _packet)
    {
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.instance.SpawnPlayer(_id, _username, _position, _rotation);
    }
    /// <summary>player position을 담은 패킷을 받음</summary>
    /// <param name="_packet"></param>
    public static void PlayerPosition(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            _player.transform.position = _position;
        }
    }
    /// <summary>player Rotation을 담은 패킷을 받음</summary>
    /// <param name="_packet"></param>
    public static void PlayerRotation(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Quaternion _rotation = _packet.ReadQuaternion();

        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            _player.transform.rotation = _rotation;
        }
    }

    /// <summary>연결 해제 > dictionary 에 남아있는 player 정보 삭제</summary>
    /// <param name="_packet"></param>
    public static void PlayerDisconnected(Packet _packet)
    {
        int _id = _packet.ReadInt();

        Destroy(GameManager.players[_id].gameObject);
        GameManager.players.Remove(_id);
    }

    /// <summary>패킷에 담긴 HP정보 update</summary>
    /// <param name="_packet"></param>
    public static void PlayerHealth(Packet _packet)
    {
        int _id = _packet.ReadInt();
        float _health = _packet.ReadFloat();

        GameManager.players[_id].SetHealth(_health);
    }

    /// <summary>패킷에 담긴 리스폰정보 update</summary>
    /// <param name="_packet"></param>
    public static void PlayerRespawned(Packet _packet)
    {
        int _id = _packet.ReadInt();

        GameManager.players[_id].Respawn();
    }

    /// <summary>패킷에 담긴 아이템 생성정보(id, position, 아이템존재여부)를 통해 아이템 생성</summary>
    /// <param name="_packet"></param>
    public static void CreateItemSpawner(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();
        Vector3 _spawnerPosition = _packet.ReadVector3();
        bool _hasItem = _packet.ReadBool();

        GameManager.instance.CreateItemSpawner(_spawnerId, _spawnerPosition, _hasItem);
    }

    /// <summary>패킷에 담긴 아이템 스폰정보(id) update</summary>
    /// <param name="_packet"></param>
    public static void ItemSpawned(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();

        GameManager.itemSpawners[_spawnerId].ItemSpawned();
    }

    /// <summary>아이템 획득정보 update</summary>
    /// <param name="_packet"></param>
    public static void ItemPickedUp(Packet _packet)
    {
        int _spawnerId = _packet.ReadInt();
        int _byPlayer = _packet.ReadInt();

        GameManager.itemSpawners[_spawnerId].ItemPickedUp();
        GameManager.players[_byPlayer].itemCount++;
    }

    /// <summary>패킷에 담긴 폭탄 생성정보(ID,position,던진player)를 통해 폭탄생성</summary>
    /// <param name="_packet"></param>
    public static void SpawnProjectile(Packet _packet)
    {
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();
        int _thrownByPlayer = _packet.ReadInt();

        GameManager.instance.SpawnProjectile(_projectileId, _position);
        GameManager.players[_thrownByPlayer].itemCount--;
    }

    /// <summary>패킷에 담긴 정보를 통해(position) 폭탄 위치 update</summary>
    /// <param name="_packet"></param>
    public static void ProjectilePosition(Packet _packet)
    {
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        //dictionary에 id가 같은 폭탄이 있는지 확인 후 position update
        if (GameManager.projectiles.TryGetValue(_projectileId, out ProjectileManager _projectile))
        {
            _projectile.transform.position = _position;
        }
    }

    /// <summary>패킷에 담긴 정보(id, position)를 통해 폭탄 폭발</summary>
    /// <param name="_packet"></param>
    public static void ProjectileExploded(Packet _packet)
    {
        int _projectileId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        GameManager.projectiles[_projectileId].Explode(_position);
    }

    /*
    /// <summary>서버로 부터 UDPTest data를 받음</summary>
    public static void UDPTest(Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received packet via UDP. Contains message: {_msg}");
        ClientSend.UDPTestReceived();
    }*/
}
