using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }
    public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
    public delegate void PacketHandler(int _fromClient, Packet _packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    /// <summary>최대인원, 포트설정, 서버에 연결될 클라이언트 초기 mapping (인덱스로 접근하기 위함), tcp리스너 실행</summary>
    /// <param name="_maxPlayers">최대 연결할 수 있는 클라이언트 수</param>
    /// <param name="_port">서버의 포트</param>
    public static void Start(int _maxPlayers, int _port)
    {
        MaxPlayers = _maxPlayers;
        Port = _port;

        Debug.Log("Starting server...");
        InitializeServerData();

        //tcp 리스너 실행
        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on {Port}.");
    }

    /// <summary>tcp가 연결되는것을 기다림</summary>
    /// <param name="_result"></param>
    private static void TCPConnectCallback(IAsyncResult _result)
    {
        TcpClient _client = tcpListener.EndAcceptTcpClient(_result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoding connection from {_client.Client.RemoteEndPoint}...");

        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (clients[i].tcp.socket == null)
            {
                clients[i].tcp.Connect(_client);
                return;
            }
        }

        Debug.Log($"{_client.Client.RemoteEndPoint} faliled to connect: Server full!");
    }

    /// <summary>불특정 클라이언트로부터 수신할때까지 기다리고 handleData를 통해 수신된 data처리</summary>
    /// <param name="_result"></param>
    private static void UDPReceiveCallback(IAsyncResult _result)
    {
        try
        {
            //불특정 클라이언트의 endpoint를 할당 (발신자의 정보를 담을 예정)
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            //불특정 클라이언트로부터 데이터를 받을때까지 기다림
            byte[] _data = udpListener.EndReceive(_result, ref _clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (_data.Length < 4)
            {
                return;
            }

            using (Packet _packet = new Packet(_data))
            {
                int _clientId = _packet.ReadInt();

                //존재하지 않는 클라이언트
                if (_clientId == 0)
                {
                    return;
                }

                //기존 연결된 udp가 없다면 새롭게 연결
                if (clients[_clientId].udp.endPoint == null)
                {
                    clients[_clientId].udp.Connect(_clientEndPoint);
                    return;
                }
                //현재 연결된 클라이언트 장치의 정보와 수신된 클라이언트 장치의 정보가 일지치하는지 확인(외부 사용자의 방해방지)
                if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                {
                    // Ensures that the client is not being impersonated by another by sending a false clientID
                    clients[_clientId].udp.HandleData(_packet);
                }
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error receiving UDP data: {_ex}");
        }
    }

    /// <summary>UDP data 전송</summary>
    /// <param name="_clientEndPoint">수신할 클라이언트의 IPEndPoint(IP,port 등)</param>
    /// <param name="_packet">발신할 패킷</param>
    public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
    {
        try
        {
            if (_clientEndPoint != null)
            {
                udpListener.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
            }
        }
        catch (Exception _ex)
        {
            Debug.Log($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
        }
    }

    /// <summary>
    /// 서버 데이터 초기세팅
    /// 서버에 연결된 클라이언트 mapping (인덱스로 접근하기 위함)
    /// 패킷핸들러에 delegate PacketHandler세팅
    /// </summary>
    private static void InitializeServerData()
    {
        for (int i = 1; i <= MaxPlayers; i++)
        {
            clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                {(int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
                {(int)ClientPackets.playerShootBullet, ServerHandle.PlayerShootBullet },
                {(int)ClientPackets.playerShootBomb, ServerHandle.PlayerShootBomb },
                {(int)ClientPackets.playerGetItem, ServerHandle.PlayerGetItem },
                {(int)ClientPackets.playerThrowItem, ServerHandle.PlayerThrowItem },
                //{(int)ClientPackets.udpTestReceived, ServerHandle.UDPTestReceived }
            };
        Debug.Log("Initialized packets.");
    }

    public static void Stop()
    {
        tcpListener.Stop();
        udpListener.Close();
    }
}