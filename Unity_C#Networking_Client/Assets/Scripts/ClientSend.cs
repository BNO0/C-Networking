using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    /// <summary>클라이언트에서 서버로 TCP형태로 패킷전송</summary>
    /// <param name="_packet"></param>
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);
    }

    /// <summary>클라이언트에서 서버로 UDP형태로 패킷전송</summary>
    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);
    }
    
    #region Packets
    /// <summary>welcome메세지를 받고난 후 동작하는 함수 (잘 받았다고 서버로 전송)</summary>
    public static void WelcomeReceived()
    {
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }
    /// <summary>player 움직임에 대한 packet UDP전송(주기적으로 전송하기때문에 패킷의 끝을 확인해야함)</summary>
    public static void PlayerMovement(bool[] _inputs)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(_inputs.Length);
            foreach(bool _input in _inputs)
            {
                _packet.Write(_input);
            }
            _packet.Write(GameManager.players[Client.instance.myId].transform.rotation);

            //주기적으로 보내기때문에 데이터충돌발생확률이 높음 > UDP
            SendUDPData(_packet);
        }
    }
    /// <summary>player 공격에 대한 packet TCP전송(공격할 때 한번만 전송하므로 누락이 될지언정 오류가 발생하지는 않음)</summary>
    public static void PlayerShootBullet(Vector3 _facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerShootBullet))
        {
            _packet.Write(_facing);

            SendTCPData(_packet);
        }
    }

    /// <summary>아이템 버리기에 대한 packet TCP전송(한번만 전송하므로 누락이 될지언정 오류가 발생하지는 않음)</summary>
    /// <param name="_facing">버릴 위치</param>
    public static void PlayerThrowItem(Vector3 _facing)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerThrowItem))
        {
            _packet.Write(_facing);

            SendTCPData(_packet);
        }
    }

    /*
    public static void UDPTestReceived()
    {
        using (Packet _packet = new Packet((int)ClientPackets.udpTestReceived))
        {
            _packet.Write("Received a UDP packet.");
            SendUDPData(_packet);
        }
    }*/
    #endregion
}
