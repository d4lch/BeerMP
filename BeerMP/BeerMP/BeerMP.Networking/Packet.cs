using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BeerMP.Networking;

public class Packet : IDisposable
{
	private List<byte> buffer;

	private byte[] readableBuffer;

	private int readPos;

	internal int id;

	internal int scene;

	private bool disposed;

	public Packet()
	{
		buffer = new List<byte>();
		readPos = 0;
		id = 0;
		scene = 1;
	}

	public Packet(int _id)
	{
		buffer = new List<byte>();
		readPos = 0;
		id = _id;
		scene = 1;
	}

	internal Packet(int _id, GameScene _scene = GameScene.GAME)
	{
		buffer = new List<byte>();
		readPos = 0;
		id = _id;
		scene = (int)_scene;
	}

	internal Packet(byte[] _data)
	{
		buffer = new List<byte>();
		readPos = 0;
		SetBytes(_data);
	}

	public void SetBytes(byte[] _data)
	{
		Write(_data);
		readableBuffer = buffer.ToArray();
	}

	internal void WriteLength()
	{
		buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
	}

	internal void InsertString(string _value)
	{
		buffer.InsertRange(0, Encoding.ASCII.GetBytes(_value));
		buffer.InsertRange(0, BitConverter.GetBytes(_value.Length));
	}

	public byte[] ToArray()
	{
		readableBuffer = buffer.ToArray();
		return readableBuffer;
	}

	public int Length()
	{
		return buffer.Count;
	}

	public int UnreadLength()
	{
		return Length() - readPos;
	}

	public void Reset(bool _shouldReset = true)
	{
		if (_shouldReset)
		{
			buffer.Clear();
			readableBuffer = null;
			readPos = 0;
		}
		else
		{
			readPos -= 4;
		}
	}

	public void Write(byte _value, int index = -1)
	{
		if (index >= 0 && index <= buffer.Count)
		{
			buffer.Insert(index, _value);
		}
		else
		{
			buffer.Add(_value);
		}
	}

	public void Write(byte[] _value, int index = -1)
	{
		if (index >= 0 && index <= buffer.Count)
		{
			buffer.InsertRange(index, _value);
		}
		else
		{
			buffer.AddRange(_value);
		}
	}

	public void Write(short _value, int index = -1)
	{
		Write(BitConverter.GetBytes(_value), index);
	}

	public void Write(int _value, int index = -1)
	{
		Write(BitConverter.GetBytes(_value), index);
	}

	public void Write(long _value, int index = -1)
	{
		Write(BitConverter.GetBytes(_value), index);
	}

	public void Write(float _value, int index = -1)
	{
		Write(BitConverter.GetBytes(_value), index);
	}

	public void Write(bool _value, int index = -1)
	{
		Write(BitConverter.GetBytes(_value), index);
	}

	public void Write(string _value, int index = -1)
	{
		Write(_value.Length);
		Write(Encoding.ASCII.GetBytes(_value), index);
	}

	public void Write(Vector2 _value, int index = -1)
	{
		Write(_value.x, index);
		Write(_value.y, index);
	}

	public void Write(Vector3 _value, int index = -1)
	{
		Write(_value.x, index);
		Write(_value.y, index);
		Write(_value.z, index);
	}

	public void Write(Vector4 _value, int index = -1)
	{
		Write(_value.x, index);
		Write(_value.y, index);
		Write(_value.z, index);
		Write(_value.w, index);
	}

	public void Write(Quaternion _value, int index = -1)
	{
		Write(_value.x, index);
		Write(_value.y, index);
		Write(_value.z, index);
		Write(_value.w, index);
	}

	public byte ReadByte(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			byte result = readableBuffer[readPos];
			if (_moveReadPos)
			{
				readPos++;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'byte'!");
	}

	public byte[] ReadBytes(int _length, bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			byte[] result = buffer.GetRange(readPos, _length).ToArray();
			if (_moveReadPos)
			{
				readPos += _length;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'byte[]'!");
	}

	public short ReadShort(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			short result = BitConverter.ToInt16(readableBuffer, readPos);
			if (_moveReadPos)
			{
				readPos += 2;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'short'!");
	}

	public int ReadInt(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			int result = BitConverter.ToInt32(readableBuffer, readPos);
			if (_moveReadPos)
			{
				readPos += 4;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'int'!");
	}

	public long ReadLong(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			long result = BitConverter.ToInt64(readableBuffer, readPos);
			if (_moveReadPos)
			{
				readPos += 8;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'long'!");
	}

	public float ReadFloat(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			float result = BitConverter.ToSingle(readableBuffer, readPos);
			if (_moveReadPos)
			{
				readPos += 4;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'float'!");
	}

	public bool ReadBool(bool _moveReadPos = true)
	{
		if (buffer.Count > readPos)
		{
			bool result = BitConverter.ToBoolean(readableBuffer, readPos);
			if (_moveReadPos)
			{
				readPos++;
			}
			return result;
		}
		throw new Exception("Could not read value of type 'bool'!");
	}

	public string ReadString(bool _moveReadPos = true)
	{
		try
		{
			int num = ReadInt();
			string @string = Encoding.ASCII.GetString(readableBuffer, readPos, num);
			if (_moveReadPos && @string.Length > 0)
			{
				readPos += num;
			}
			return @string;
		}
		catch
		{
			throw new Exception("Could not read value of type 'string'!");
		}
	}

	public Vector2 ReadVector2(bool _moveReadPos = true)
	{
		int num = readPos;
		Vector2 result = new Vector2(ReadFloat(), ReadFloat());
		if (!_moveReadPos)
		{
			readPos = num;
		}
		return result;
	}

	public Vector3 ReadVector3(bool _moveReadPos = true)
	{
		int num = readPos;
		Vector3 result = new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
		if (!_moveReadPos)
		{
			readPos = num;
		}
		return result;
	}

	public Vector4 ReadVector4(bool _moveReadPos = true)
	{
		int num = readPos;
		Vector4 result = new Vector4(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
		if (!_moveReadPos)
		{
			readPos = num;
		}
		return result;
	}

	public Quaternion ReadQuaternion(bool _moveReadPos = true)
	{
		int num = readPos;
		Quaternion result = new Quaternion(ReadFloat(), ReadFloat(), ReadFloat(), ReadFloat());
		if (!_moveReadPos)
		{
			readPos = num;
		}
		return result;
	}

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
		Dispose(_disposing: true);
		GC.SuppressFinalize(this);
	}
}
