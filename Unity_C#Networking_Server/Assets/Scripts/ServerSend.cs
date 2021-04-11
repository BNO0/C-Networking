using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 서버에서 클라이언트로 패킷전송 (한번만 전송하는것은 TCP, 지속적인 전송은 UDP)
/// </summary>
public class ServerSend
{
    /// <summary>_toClient에 해당하는 클라이언트에 패킷전송 _TCP</summary>
    /// <param name="_toClient">수신할 클라이언트</param>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].tcp.SendData(_packet);
    }

    /// <summary>_toClient에 해당하는 클라이언트에 패킷전송 _UDP</summary>
    /// <param name="_toClient">수신할 클라이언트</param>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.clients[_toClient].udp.SendData(_packet);
    }

    /// <summary>연결된 모든 클라이언트에 패킷전송 _TCP</summary>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }

    /// <summary>_exceptClient를 제외한 모든 클라이언트에 패킷 전송 _TCP</summary>
    /// <param name="_exceptClient">예외 클라이언트번호</param>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    /// <summary>연결된 모든 클라이언트에 패킷전송 _UDP</summary>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }

    /// <summary>_exceptClient를 제외한 모든 클라이언트에 패킷 전송 _UDP</summary>
    /// <param name="_exceptClient">예외 클라이언트번호</param>
    /// <param name="_packet">발신할 패킷</param>
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    #region Packets
    /// <summary>server 에서 _toClient에 해당하는 클라이언트로 메세지(welcome) 전송</summary>
    /// <param name="_toClient">수신할 클라이언트</param>
    /// <param name="_msg">패킷화 할 string data</param>
    public static void Welcome(int _toClient, string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>클라이언트들에게 스폰된 플레이어를 전달(한번만 전달하기 때문에 TCP)</summary>
    /// <param name="_toClient">플레이어를 스폰해야하는 클라이언트</param>
    /// <param name="_player">스폰되는 플레이어</param>
    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            //한번만 전달하기 때문에 데이터가 충돌할일이 거의 없음
            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>플레이어의 위치를 모든 클라이언트에 UDP전송</summary>
    /// <param name="_player">The player whose position to update.</param>
    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    /// <summary>플레이어의 rotation을 자기자신을 제외한(localplayer에 중복으로 써지는것을 막기위함) 모든 클라이언트에 UDP전송</summary>
    /// <param name="_player">The player whose rotation to update.</param>
    public static void PlayerRotation(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    /// <summary>특정 플레이어가 연결이 끊길때 모든 클라이언트에게 해당사항 TCP전송</summary>
    /// <param name="_playerId">연결이 끊긴 플레이어ID</param>
    public static void PlayerDisconnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>특정 플레이어의 HP정보를 모든 클라이언트에게 TCP전송</summary>
    /// <param name="_player">전송하고싶은 Player</param>
    public static void PlayerHealth(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerHealth))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.hp);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>리스폰된 특정 플레이어를 모든 클라이언트에게 TCP전송</summary>
    /// <param name="_player">전송하고싶은 Player</param>
    public static void PlayerRespawned(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawned))
        {
            _packet.Write(_player.id);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>아이템ID, position, 아이템존재여부 TCP전송</summary>
    /// <param name="_toClient">수신할 클라이언트</param>
    /// <param name="_spawnerId">아이템ID</param>
    /// <param name="_spawnerPosition">아이템 position</param>
    /// <param name="_hasItem">아이템 존재여부</param>
    public static void CreateItemSpawner(int _toClient, int _spawnerId, Vector3 _spawnerPosition, bool _hasItem)
    {
        using (Packet _packet = new Packet((int)ServerPackets.createItemSpawner))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_spawnerPosition);
            _packet.Write(_hasItem);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>스폰된 아이템 TCP전송</summary>
    /// <param name="_spawnerId">스폰된 아이템</param>
    public static void ItemSpawned(int _spawnerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemSpawned))
        {
            _packet.Write(_spawnerId);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>획득한 아이템ID, 획득한 플레이어 TCP전송</summary>
    /// <param name="_spawnerId">획득한 아이템ID</param>
    /// <param name="_byPlayer">아이템을 획득한 플레이어</param>
    public static void ItemPickedUp(int _spawnerId, int _byPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemPickedUp))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>버린 아이템ID, 버린 플레이어 TCP전송</summary>
    /// <param name="_spawnerId">버린 아이템ID</param>
    /// <param name="_byPlayer">아이템을 버린 플레이어</param>
    /// <param name="_position">아이템을 버린 위치</param>
    public static void ItemThrow(int _spawnerId, int _byPlayer, Vector3 _position)
    {
        using (Packet _packet = new Packet((int)ServerPackets.itemThrow))
        {
            _packet.Write(_spawnerId);
            _packet.Write(_byPlayer);
            _packet.Write(_position);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>폭탄 생성 TCP전송</summary>
    /// <param name="_projectile">폭탄</param>
    /// <param name="_thrownByPlayer">폭탄을 던진 player</param>
    public static void SpawnProjectile(Projectile _projectile, int _thrownByPlayer)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnProjectile))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);
            _packet.Write(_thrownByPlayer);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>폭탄 위치 UDP전송(여러번 반복전송)</summary>
    /// <param name="_projectile">폭탄</param>
    public static void ProjectilePosition(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectilePosition))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendUDPDataToAll(_packet);
        }
    }

    /// <summary>폭탄 폭발 TCP전송</summary>
    /// <param name="_projectile">폭탄</param>
    public static void ProjectileExploded(Projectile _projectile)
    {
        using (Packet _packet = new Packet((int)ServerPackets.projectileExploded))
        {
            _packet.Write(_projectile.id);
            _packet.Write(_projectile.transform.position);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>Key Change 실행</summary>
    /// <param name="_playerID">플레이어 ID</param>
    public static void KeyChange(int _playerID)
    {
        using (Packet _packet = new Packet((int)ServerPackets.keyChange))
        {
            _packet.Write(_playerID);

            SendTCPData(_playerID, _packet);
        }
    }
    /*
    public static void UDPTest(int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.udpTest))
        {
            _packet.Write("A test packet for UDP.");

            SendUDPData(_toClient, _packet);
        }
    }*/
    #endregion
}