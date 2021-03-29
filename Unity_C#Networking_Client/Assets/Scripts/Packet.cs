using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

//GameServer VisualStudio에도 똑같이 수정(패킷형식 동일화)
/// <summary>서버가 보내는packet이 어떤정보인지 알려주는 역할</summary>
public enum ServerPackets
{
    welcome = 1,        //접속
    spawnPlayer,        //player 스폰
    playerPosition,     //player position
    playerRotation,     //player rotation
    playerDisconnected, //plyaer 연결끊김
    playerHealth,       //player hp
    playerRespawned,    //player 리스폰
    createItemSpawner,  //아이템 생성
    itemSpawned,        //아이템 스폰
    itemPickedUp,       //아이템 획득
    spawnProjectile,    //폭탄 생성
    projectilePosition, //폭탄 위치
    projectileExploded, //폭탄 폭발여부
    //udpTest
}

/// <summary>클라이언트가 보내는 packet이 어떤정보인지 알려주는 역할</summary>
public enum ClientPackets
{
    welcomeReceived = 1,//접속
    playerMovement,     //움직임
    playerShoot,        //공격?
    playerThrowItem,    //아이템 버리기
    // udpTestReceived
}

/// <summary>이 class가 존재하는 cs파일에서 보낼 Packet에 대한 정의를 하는게 좋을듯?</summary>
public class Packet : IDisposable
{
    private List<byte> buffer;
    private byte[] readableBuffer;
    private int readPos;

    /// <summary>Creates a new empty packet (without an ID).</summary>
    public Packet()
    {
        buffer = new List<byte>(); // Initialize buffer
        readPos = 0; // Set readPos to 0
    }

    /// <summary>Creates a new packet with a given ID. Used for sending.</summary>
    /// <param name="_id">The packet ID.</param>
    public Packet(int _id)
    {
        buffer = new List<byte>(); // Initialize buffer
        readPos = 0; // Set readPos to 0

        Write(_id); // Write packet id to the buffer
    }

    /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
    /// <param name="_data">The bytes to add to the packet.</param>
    public Packet(byte[] _data)
    {
        buffer = new List<byte>(); // Initialize buffer
        readPos = 0; // Set readPos to 0

        SetBytes(_data);
    }

    #region Functions
    /// <summary>Sets the packet's content and prepares it to be read.</summary>
    /// <param name="_data">The bytes to add to the packet.</param>
    public void SetBytes(byte[] _data)
    {
        Write(_data);
        readableBuffer = buffer.ToArray();
    }

    /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
    public void WriteLength()
    {
        buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
    }

    /// <summary>Inserts the given int at the start of the buffer.</summary>
    /// <param name="_value">The int to insert.</param>
    public void InsertInt(int _value)
    {
        buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
    }

    /// <summary>Gets the packet's content in array form.</summary>
    public byte[] ToArray()
    {
        readableBuffer = buffer.ToArray();
        return readableBuffer;
    }

    /// <summary>Gets the length of the packet's content.</summary>
    public int Length()
    {
        return buffer.Count; // Return the length of buffer
    }

    /// <summary>Gets the length of the unread data contained in the packet.</summary>
    public int UnreadLength()
    {
        return Length() - readPos; // Return the remaining length (unread)
    }

    /// <summary>Resets the packet instance to allow it to be reused.</summary>
    /// <param name="_shouldReset">Whether or not to reset the packet.</param>
    public void Reset(bool _shouldReset = true)
    {
        if (_shouldReset)
        {
            buffer.Clear(); // Clear buffer
            readableBuffer = null;
            readPos = 0; // Reset readPos
        }
        else
        {
            readPos -= 4; // "Unread" the last read int
        }
    }
    #endregion

    //packet data send message 보낼 데이터 형식 여기에서 정의
    #region Write Data
    /// <summary>Adds a byte to the packet.</summary>
    /// <param name="_value">The byte to add.</param>
    public void Write(byte _value)
    {
        buffer.Add(_value);
    }
    /// <summary>Adds an array of bytes to the packet.</summary>
    /// <param name="_value">The byte array to add.</param>
    public void Write(byte[] _value)
    {
        buffer.AddRange(_value);
    }
    /// <summary>Adds a short to the packet.</summary>
    /// <param name="_value">The short to add.</param>
    public void Write(short _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds an int to the packet.</summary>
    /// <param name="_value">The int to add.</param>
    public void Write(int _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a long to the packet.</summary>
    /// <param name="_value">The long to add.</param>
    public void Write(long _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a float to the packet.</summary>
    /// <param name="_value">The float to add.</param>
    public void Write(float _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a bool to the packet.</summary>
    /// <param name="_value">The bool to add.</param>
    public void Write(bool _value)
    {
        buffer.AddRange(BitConverter.GetBytes(_value));
    }
    /// <summary>Adds a string to the packet.</summary>
    /// <param name="_value">The string to add.</param>
    public void Write(string _value)
    {
        Write(_value.Length); // Add the length of the string to the packet
        buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
    }
    /// <summary>Adds a Vector3 to the packet.</summary>
    /// <param name="_value">The Vector3 to add.</param>
    public void Write(Vector3 _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);
    }
    /// <summary>Adds a Quaternion to the packet.</summary>
    /// <param name="_value">The Quaternion to add.</param>
    public void Write(Quaternion _value)
    {
        Write(_value.x);
        Write(_value.y);
        Write(_value.z);
        Write(_value.w);
    }
    #endregion

    #region Read Data
    /// <summary>
    /// Reads a byte from the packet.
    /// packet data를 byte data로 변환
    /// byte는 1byte이기 때문에 readPos+=1
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public byte ReadByte(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            byte _value = readableBuffer[readPos]; // Get the byte at readPos' position
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the byte
        }
        else
        {
            throw new Exception("Could not read value of type 'byte'!");
        }
    }

    /// <summary>Reads an array of bytes from the packet.</summary>
    /// <param name="_length">The length of the byte array.</param>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public byte[] ReadBytes(int _length, bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            byte[] _value = buffer.GetRange(readPos, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += _length; // Increase readPos by _length
            }
            return _value; // Return the bytes
        }
        else
        {
            throw new Exception("Could not read value of type 'byte[]'!");
        }
    }

    /// <summary>
    /// Reads a short from the packet.
    /// packet data를 short data로 변환
    /// short는 2byte이기 때문에 readPos+=2
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public short ReadShort(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            short _value = BitConverter.ToInt16(readableBuffer, readPos); // Convert the bytes to a short
            if (_moveReadPos)
            {
                // If _moveReadPos is true and there are unread bytes
                readPos += 2; // Increase readPos by 2
            }
            return _value; // Return the short
        }
        else
        {
            throw new Exception("Could not read value of type 'short'!");
        }
    }

    /// <summary>
    /// Reads an int from the packet.
    /// packet data를 int data로 변환
    /// int는 4byte이기 때문에 readPos+=4
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public int ReadInt(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            int _value = BitConverter.ToInt32(readableBuffer, readPos); // Convert the bytes to an int
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 4; // Increase readPos by 4
            }
            return _value; // Return the int
        }
        else
        {
            throw new Exception("Could not read value of type 'int'!");
        }
    }

    /// <summary>
    /// Reads a long from the packet.
    /// packet data를 long data로 변환
    /// long는 8byte이기 때문에 readPos+=8
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public long ReadLong(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            long _value = BitConverter.ToInt64(readableBuffer, readPos); // Convert the bytes to a long
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 8; // Increase readPos by 8
            }
            return _value; // Return the long
        }
        else
        {
            throw new Exception("Could not read value of type 'long'!");
        }
    }

    /// <summary>
    /// Reads a float from the packet.
    /// packet data를 float data로 변환
    /// float은 4byte이기 때문에 readPos+=4
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public float ReadFloat(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            float _value = BitConverter.ToSingle(readableBuffer, readPos); // Convert the bytes to a float
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 4; // Increase readPos by 4
            }
            return _value; // Return the float
        }
        else
        {
            throw new Exception("Could not read value of type 'float'!");
        }
    }

    /// <summary>
    /// Reads a bool from the packet.
    /// packet data를 bool data로 변환
    /// bool은 1byte이기 때문에 readPos+=1
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public bool ReadBool(bool _moveReadPos = true)
    {
        if (buffer.Count > readPos)
        {
            // If there are unread bytes
            bool _value = BitConverter.ToBoolean(readableBuffer, readPos); // Convert the bytes to a bool
            if (_moveReadPos)
            {
                // If _moveReadPos is true
                readPos += 1; // Increase readPos by 1
            }
            return _value; // Return the bool
        }
        else
        {
            throw new Exception("Could not read value of type 'bool'!");
        }
    }

    /// <summary>
    /// Reads a string from the packet.
    /// packet data를 string data로 변환
    /// string의 길이를 _length에 저장하기 때문에 readPos+=_length
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public string ReadString(bool _moveReadPos = true)
    {
        try
        {
            int _length = ReadInt(); // Get the length of the string
            string _value = Encoding.ASCII.GetString(readableBuffer, readPos, _length); // Convert the bytes to a string
            if (_moveReadPos && _value.Length > 0)
            {
                // If _moveReadPos is true string is not empty
                readPos += _length; // Increase readPos by the length of the string
            }
            return _value; // Return the string
        }
        catch
        {
            throw new Exception("Could not read value of type 'string'!");
        }
    }

    /// <summary>
    /// Reads a Vector3 from the packet.
    /// ReadFloat을 통해 3개의 float 수신
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public Vector3 ReadVector3(bool _moveReadPos = true)
    {
        return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
    }

    /// <summary>
    /// Reads a Quaternion from the packet.
    /// ReadFloat을 통해 4개의 float 수신
    /// </summary>
    /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
    public Quaternion ReadQuaternion(bool _moveReadPos = true)
    {
        return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
    }
    #endregion

    private bool disposed = false;

    protected virtual void Dispose(bool _disposing)
    {
        if (!disposed)
        {
            if (_disposing)
            {
                buffer = null;
                readableBuffer = null;
                readPos = 0;
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
