using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int dataBufferSize = 4096;

    public int id;
    public Player player;
    public TCP tcp;
    public UDP udp;

    public Client(int _clientID)
    {
        id = _clientID;
        tcp = new TCP(id);
        udp = new UDP(id);
    }

    public class TCP
    {
        public TcpClient socket;

        private readonly int id;
        private NetworkStream stream;
        private Packet receivedData;    //받은 패킷data
        private byte[] receiveBuffer;   //받을 패킷 data

        public TCP(int _id)
        {
            id = _id;
        }

        /// <summary>client 연결 (클라이언트가 서버로부터 메세지 받을때까지 대기 후 서버로 welcom 전송)</summary>
        /// <param name="_socket">새롭게 연결된 tcp client 소켓</param>
        public void Connect(TcpClient _socket)
        {
            socket = _socket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();

            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            //연결이 되면 server에서 welcome to the server 전송
            ServerSend.Welcome(id, "Welcome to the server!");
        }

        /// <summary>스트림에 패킷 쓰기(TCP 메세지 전송)</summary>
        /// <param name="_packet">발신할 packet</param>
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {_ex}");
            }
        }

        /// <summary>client에 도착할 수신data 대기 (stream에 있는 data 처리, 빈메세지 받으면 disconnect)</summary>
        /// <param name="_result"></param>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    Server.clients[id].Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                Array.Copy(receiveBuffer, _data, _byteLength);

                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error receiveig TCP data: {_ex}");
                Server.clients[id].Disconnect();
            }
        }

        /// <summary>패킷을 모두 받았는지 확인(남은 data의 size를 통해 데이터가 제대로 전달되었는지 파악)</summary>
        /// <param name="_data">client가 수신한 data</param>
        /// <returns></returns>
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            receivedData.SetBytes(_data);

            if (receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if (_packetLength <= 0)
                {
                    return true;
                }
            }

            while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });

                _packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (_packetLength <= 1)
            {
                return true;
            }

            //packetlength가 말도안되는 수로 남았을때 에러
            return false;
        }

        /// <summary>TCP 연결해제</summary>
        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        /// <summary>연결장치의 정보(IP, 포트 등)</summary>
        public IPEndPoint endPoint;

        private int id;

        public UDP(int _id)
        {
            id = _id;
        }

        /// <summary>Initializes the newly connected client's UDP-related info.</summary>
        /// <param name="_endPoint">새롭게 연결된 client의 IPEndPoint(연결장치의 IP, 포트 등의 정보)</param>
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
            //ServerSend.UDPTest(id);
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="_packet">발신할 패킷</param>
        public void SendData(Packet _packet)
        {
            Server.SendUDPData(endPoint, _packet);
        }

        /// <summary>data를 패킷단위로 처리</summary>
        /// <param name="_packetData">The packet containing the recieved data.</param>
        public void HandleData(Packet _packetData)
        {
            int _packetLength = _packetData.ReadInt();
            byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_packetBytes))
                {
                    int _packetId = _packet.ReadInt();
                    Server.packetHandlers[_packetId](id, _packet); // Call appropriate method to handle the packet
                }
            });
        }

        /// <summary>UDP 연결해제</summary>
        public void Disconnect()
        {
            endPoint = null;
        }
    }

    /// <summary>클라이언트를 게임으로 접속시키고 다른클라이언트에게 새로운 플레이어를 전송</summary>
    /// <param name="_playerName">The username of the new player.</param>
    public void SendIntoGame(string _playerName)
    {
        player = NetworkManager.instance.InstantiatePlayer();
        player.Initialize(id, _playerName);

        // 이미 연결된 다른 클라이언트들의 정보를 새로운 클라이언트에 전송
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.SpawnPlayer(id, _client.player);
                }
            }
        }

        // 자신을 포함하여 모든 클라이언트에 자신의 정보를 전송
        foreach (Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                ServerSend.SpawnPlayer(_client.id, player);
            }
        }

        // 아이템정보 전송
        foreach (ItemSpawner _itemSpawner in ItemSpawner.spawners.Values)
        {
            ServerSend.CreateItemSpawner(id, _itemSpawner.spawnerId, _itemSpawner.transform.position, _itemSpawner.hasItem);
        }

    }

    /// <summary>Disconnects the client and stops all network traffic.</summary>
    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;

        });

        tcp.Disconnect();
        udp.Disconnect();

        ServerSend.PlayerDisconnected(id);
    }
}
