using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>client 설정 (ip,port client id)</summary>
public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 26950;
    public int myId = 0;
    public TCP tcp;
    public UDP udp;

    private bool isConnected = false;

    private delegate void PacketHandler(Packet _packet);
    /// <summary>패킷단위로 data를 저장할 곳</summary>
    private static Dictionary<int, PacketHandler> packetHandlers;

    // Start is called before the first frame update
    /// <summary>이미 존재하는지 체크</summary>
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Debug.Log("Instance already exists, destorying object!");
            Destroy(this);
        }
    }

    private void OnApplicationQuit()
    {
        Disconnect(); // Disconnect when the game is closed
    }

    private void Start()
    {
        {
            tcp = new TCP();
            udp = new UDP();
        }
    }

    /// <summary>client 데이터초기화 후 server에 연결</summary>
    public void ConnectToServer()
    {
        InitializeClientData();

        isConnected = true;
        tcp.Connect();
    }
    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;    //받은 패킷data
        private byte[] receiveBuffer;   //받을 패킷 data

        /// <summary>tcp 연결</summary>
        public void Connect()
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, socket);
        }

        /// <summary>연결이 끝난 후 실행 (연결 종료확정 후 메세지 받을준비)</summary>
        /// <param name="_result"></param>
        private void ConnectCallback(System.IAsyncResult _result) // IAsyncResult > System.IAsyncResult
        {
            socket.EndConnect(_result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_packet"></param>
        public void SendData(Packet _packet)
        {
            try
            {
                if(socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }

            catch(Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
            }
        }

        /// <summary>메세지를 받은 후 실행(recievedData(받은데이터) 리셋 후 다시 받을 준비), 받은 packet처리후 다음 packet 받을준비</summary>
        /// <param name="_result"></param>
        private void ReceiveCallback(System.IAsyncResult _result) // IAsyncResult > System.IAsyncResult
        {
            try
            {
                int _byteLength = stream.EndRead(_result);
                if (_byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                byte[] _data = new byte[_byteLength];
                System.Array.Copy(receiveBuffer, _data, _byteLength); // Array > System.Array

                //다음 패킷을 받기위해 리셋
                receivedData.Reset(HandleData(_data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
            {
                Disconnect();
            }
        }
        /// <summary>패킷을 모두 받았는지 확인하고(남은 data의 size를 통해 데이터가 제대로 전달되었는지 파악), data를 패킷단위로 저장</summary>
        /// <param name="_data"></param>
        /// <returns></returns>
        private bool HandleData(byte[] _data)
        {
            int _packetLength = 0;

            receivedData.SetBytes(_data);

            if(receivedData.UnreadLength() >= 4)
            {
                _packetLength = receivedData.ReadInt();
                if(_packetLength <= 0)
                {
                    return true;
                }
            }

            while(_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
            {
                byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        packetHandlers[_packetId](_packet);
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

            if(_packetLength <= 1)
            {
                return true;
            }

            //packetlength가 말도안되는 수로 남았을때 에러
            return false;
        }

        /// <summary>TCP연결해제</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int _localPort)
        {
            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet _packet = new Packet())
            {
                SendData(_packet);
            }
        }

        public void SendData(Packet _packet)
        {
            try
            {
                //패킷 제일처음에 클라이언트 자신의 id를 넣음
                _packet.InsertInt(instance.myId);
                if(socket != null){
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch(Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }
        }

        //???? 
        /// <summary>메세지를 받은 후 실행 잘받았다고 전송</summary>
        /// <param name="_result"></param>
        public void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                //받은 메세지가 없을때 연결끊음
                if(_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }
                HandleData(_data);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>data를 패킷단위로 저장</summary>
        /// <param name="_data"></param>
        public void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.ReadInt();
                _data = _packet.ReadBytes(_packetLength);
            }

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet);
                }
            });
        }

        /// <summary>UDP연결해제</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    /// <summary>클라이언트 데이터 초기세팅 (패킷핸들러에 delegate PacketHandler세팅)</summary>
    private void InitializeClientData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, ClientHandle.Welcome },
            { (int)ServerPackets.spawnPlayer, ClientHandle.SpawnPlayer},
            { (int)ServerPackets.playerPosition, ClientHandle.PlayerPosition},
            { (int)ServerPackets.playerRotation, ClientHandle.PlayerRotation},
            { (int)ServerPackets.playerDisconnected, ClientHandle.PlayerDisconnected},
            { (int)ServerPackets.playerHealth, ClientHandle.PlayerHealth},
            { (int)ServerPackets.playerRespawned, ClientHandle.PlayerRespawned},
            { (int)ServerPackets.createItemSpawner, ClientHandle.CreateItemSpawner},
            { (int)ServerPackets.itemSpawned, ClientHandle.ItemSpawned},
            { (int)ServerPackets.itemPickedUp, ClientHandle.ItemPickedUp},
            { (int)ServerPackets.spawnProjectile, ClientHandle.SpawnProjectile},
            { (int)ServerPackets.projectilePosition, ClientHandle.ProjectilePosition},
            { (int)ServerPackets.projectileExploded, ClientHandle.ProjectileExploded},
            //{ (int)ServerPackets.udpTest, ClientHandle.UDPTest }
        };
        Debug.Log("Initialized packets.");
    }

    /// <summary>모든연결해제</summary>
    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Disconnected from server.");
        }
    }
}
