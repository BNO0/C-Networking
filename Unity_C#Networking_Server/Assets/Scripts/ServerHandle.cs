using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ServerHandle
{
    /// <summary>welcome 잘받았다는 메세지 도착 시 클라이언트ID가 맞는지 확인 후 서버에 출력</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
        // TODO: send
    }

    /// <summary>플레이어의 움직임packet을 받아 움직임 처리</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void PlayerMovement(int _fromClient, Packet _packet)
    {
        bool[] _inputs = new bool[_packet.ReadInt()];
        for (int i = 0; i < _inputs.Length; i++)
        {
            _inputs[i] = _packet.ReadBool();
        }
        Quaternion _rotation = _packet.ReadQuaternion();

        Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
    }

    /// <summary>player 공격에 대한 패킷을 통해 shoot 처리</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void PlayerShootBullet(int _fromClient, Packet _packet)
    {
        Vector3 _shootDirection = _packet.ReadVector3();
        bool _EMPInstallFinished = _packet.ReadBool();

        Server.clients[_fromClient].player.Shoot(_shootDirection, _EMPInstallFinished);
    }

    /// <summary>투척에 대한 패킷을 통해 아이템버리는 것 처리</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void PlayerShootBomb(int _fromClient, Packet _packet)
    {
        Vector3 _throwDirection = _packet.ReadVector3();

        Server.clients[_fromClient].player.ThrowItem(_throwDirection);
    }

    /// <summary>아이템획득에 관한 패킷을 통해 아이템획득 처리</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void PlayerGetItem(int _fromClient, Packet _packet)
    {
        string _itemName = _packet.ReadString();
        int _spawnerId = _packet.ReadInt();

        ItemSpawner.spawners[_spawnerId].ItemPickedUp(Server.clients[_fromClient].player.id);
    }

    /// <summary>아이템버리기에 관한 패킷을 통해 아이템버리기 처리</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void PlayerThrowItem(int _fromClient, Packet _packet)
    {
        string _itemName = _packet.ReadString();
        int _spawnerId = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();

        ItemSpawner.spawners[_spawnerId].ItemThrow(Server.clients[_fromClient].player.id, _position);
    }
    /*
    /// <summary>welcome 잘받았다는 메세지 도착 시 클라이언트ID가 맞는지 확인 후 서버에 출력</summary>
    /// <param name="_fromClient"></param>
    /// <param name="_packet"></param>
    public static void UDPTestReceived(int _fromClient, Packet _packet)
    {
        string _msg = _packet.ReadString();

        Debug.Log($"Received packet via UDP. Contains message: {_msg}");
    }*/
}