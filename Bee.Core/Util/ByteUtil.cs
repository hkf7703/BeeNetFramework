using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Diagnostics;

namespace Bee.Util
{
    /// <summary>
    /// 表示字节存储次序枚举
    /// </summary>
    public enum Endians
    {
        /// <summary>
        /// 高位在前
        /// </summary>
        Big,
        /// <summary>
        /// 低位在前
        /// </summary>
        Little
    }
    /// <summary>
    /// 提供二进制数据生成支持      
    /// 非线程安全类型  
    /// </summary>
    [DebuggerDisplay("Length = {Length}, Endian = {Endian}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public class ByteBuilder
    {
        /// <summary>
        /// 容量
        /// </summary>
        private int _capacity;

        /// <summary>
        /// 当前数据
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// 获取数据长度
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// 获取字节存储次序
        /// </summary>
        public Endians Endian { get; private set; }


        /// <summary>
        /// 提供二进制数据读取和操作支持
        /// </summary>
        /// <param name="endian">字节存储次序</param>       
        public ByteBuilder(Endians endian)
        {
            this.Endian = endian;
        }

        /// <summary>
        /// 添加一个bool类型
        /// </summary>
        /// <param name="value">值</param>
        public void Add(bool value)
        {
            this.Add(BitConverter.GetBytes(value));
        }

        /// <summary>
        /// 添加一个字节
        /// </summary>
        /// <param name="value">字节</param>
        public void Add(byte value)
        {
            this.Add(new byte[] { value });
        }

        /// <summary>
        /// 将16位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(short value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将16位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(ushort value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将32位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(int value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将32位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(uint value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将64位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(long value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 将64位整数转换为byte数组再添加
        /// </summary>
        /// <param name="value">整数</param>        
        public void Add(ulong value)
        {
            var bytes = ByteConverter.ToBytes(value, this.Endian);
            this.Add(bytes);
        }

        /// <summary>
        /// 添加指定数据数组
        /// </summary>
        /// <param name="array">数组</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public void Add(byte[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }
            this.Add(array, 0, array.Length);
        }

        /// <summary>
        /// 添加指定数据数组
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="offset">数组的偏移量</param>
        /// <param name="count">字节数</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Add(byte[] array, int offset, int count)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            if (offset < 0 || offset > array.Length)
            {
                throw new ArgumentOutOfRangeException("offset", "offset值无效");
            }

            if (count < 0 || (offset + count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("count", "count值无效");
            }
            int newLength = this.Length + count;
            this.ExpandCapacity(newLength);

            Buffer.BlockCopy(array, offset, this._buffer, this.Length, count);
            this.Length = newLength;
        }


        /// <summary>
        /// 扩容
        /// </summary>
        /// <param name="newLength">满足的新大小</param>
        private void ExpandCapacity(int newLength)
        {
            if (newLength <= this._capacity)
            {
                return;
            }

            if (this._capacity == 0)
            {
                this._capacity = 64;
            }

            while (newLength > this._capacity)
            {
                this._capacity = this._capacity * 2;
            }

            var newBuffer = new byte[this._capacity];
            if (this.Length > 0)
            {
                Buffer.BlockCopy(this._buffer, 0, newBuffer, 0, this.Length);
            }
            this._buffer = newBuffer;
        }


        /// <summary>
        /// 获取或设置指定位置的字节
        /// </summary>
        /// <param name="index">索引</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return this._buffer[index];
            }
            set
            {
                if (index < 0 || index >= this.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }
                this._buffer[index] = value;
            }
        }

        /// <summary>
        /// 将指定长度的数据复制到目标数组
        /// </summary>
        /// <param name="dstArray">目标数组</param>     
        /// <param name="count">要复制的字节数</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void CopyTo(byte[] dstArray, int count)
        {
            this.CopyTo(dstArray, 0, count);
        }

        /// <summary>
        /// 将指定长度的数据复制到目标数组
        /// </summary>
        /// <param name="dstArray">目标数组</param>
        /// <param name="dstOffset">目标数组偏移量</param>
        /// <param name="count">要复制的字节数</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void CopyTo(byte[] dstArray, int dstOffset, int count)
        {
            this.CopyTo(0, dstArray, dstOffset, count);
        }

        /// <summary>
        /// 从指定偏移位置将数据复制到目标数组
        /// </summary>
        /// <param name="srcOffset">偏移量</param>
        /// <param name="dstArray">目标数组</param>
        /// <param name="dstOffset">目标数组偏移量</param>
        /// <param name="count">要复制的字节数</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void CopyTo(int srcOffset, byte[] dstArray, int dstOffset, int count)
        {
            Buffer.BlockCopy(this._buffer, srcOffset, dstArray, dstOffset, count);
        }

        /// <summary>
        /// 转换为byte数组
        /// </summary>
        /// <returns></returns>
        public byte[] ToArray()
        {
            var array = new byte[this.Length];
            this.CopyTo(array, array.Length);
            return array;
        }

        /// <summary>
        /// 转换为ByteRange类型
        /// </summary>      
        /// <returns></returns>        
        public ByteRange ToByteRange()
        {
            return new ByteRange(this._buffer, 0, this.Length);
        }

        /// <summary>
        /// 调试视图
        /// </summary>
        private class DebugView
        {
            /// <summary>
            /// 查看的对象
            /// </summary>
            private ByteBuilder buidler;

            /// <summary>
            /// 调试视图
            /// </summary>
            /// <param name="buidler">查看的对象</param>
            public DebugView(ByteBuilder buidler)
            {
                this.buidler = buidler;
            }

            /// <summary>
            /// 查看的内容
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public byte[] Values
            {
                get
                {
                    var array = new byte[buidler.Length];
                    buidler.CopyTo(array, buidler.Length);
                    return array;
                }
            }
        }
    }

    /// <summary>
    /// 定义二进制数据范围
    /// </summary>
    public interface IByteRange
    {
        /// <summary>
        /// 获取偏移量
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// 获取字节数
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取字节数组
        /// </summary>
        byte[] Buffer { get; }
    }

    /// <summary>
    /// 表示字节数组范围   
    /// </summary>
    [DebuggerDisplay("Offset = {Offset}, Count = {Count}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed class ByteRange : IByteRange
    {
        /// <summary>
        /// 获取偏移量
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// 获取字节数
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// 获取字节数组
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// 表示字节数组范围
        /// </summary>
        /// <param name="buffer">字节数组</param>   
        /// <exception cref="ArgumentNullException"></exception>
        public ByteRange(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            this.Buffer = buffer;
            this.Count = buffer.Length;
            this.Offset = 0;
        }

        /// <summary>
        /// 表示字节数组范围
        /// </summary>
        /// <param name="buffer">字节数组</param>
        /// <param name="offset">偏移量</param>
        /// <param name="count">字节数</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ByteRange(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException();
            }

            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset", "offset值无效");
            }

            if (count < 0 || (offset + count) > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count", "count值无效");
            }
            this.Buffer = buffer;
            this.Offset = offset;
            this.Count = count;
        }

        /// <summary>
        /// 分割为大小相等的ByteRange集合
        /// </summary>
        /// <param name="size">新的ByteRange大小</param>
        /// <returns></returns>
        public IEnumerable<ByteRange> SplitBySize(int size)
        {
            if (size >= this.Count)
            {
                yield return this;
                yield break;
            }

            var remain = this.Count % size;
            var count = this.Count - remain;

            var offset = 0;
            while (offset < count)
            {
                yield return new ByteRange(this.Buffer, this.Offset + offset, size);
                offset = offset + size;
            }

            if (remain > 0)
            {
                yield return new ByteRange(this.Buffer, offset, remain);
            }
        }

        /// <summary>
        /// 从byte[]隐式转换
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static implicit operator ByteRange(byte[] buffer)
        {
            return new ByteRange(buffer);
        }

        /// <summary>
        /// 从ArraySegment隐式转换
        /// </summary>
        /// <param name="arraySegment"></param>
        /// <returns></returns>
        public static implicit operator ByteRange(ArraySegment<byte> arraySegment)
        {
            return new ByteRange(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
        }

        /// <summary>
        /// 调试视图
        /// </summary>
        private class DebugView
        {
            /// <summary>
            /// 查看的对象
            /// </summary>
            private ByteRange view;

            /// <summary>
            /// 调试视图
            /// </summary>
            /// <param name="view">查看的对象</param>
            public DebugView(ByteRange view)
            {
                this.view = view;
            }

            /// <summary>
            /// 查看的内容
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public byte[] Values
            {
                get
                {
                    var byteArray = new byte[this.view.Count];
                    System.Buffer.BlockCopy(this.view.Buffer, this.view.Offset, byteArray, 0, this.view.Count);
                    return byteArray;
                }
            }
        }
    }

    /// <summary>
    /// 表示byte类型转换工具类
    /// 提供byte和整型之间的转换
    /// </summary>
    public static class ByteConverter
    {
        /// <summary>
        /// 返回由字节数组中指定位置的8个字节转换来的64位有符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>        
        public static long ToInt64(byte[] bytes, int startIndex, Endians endian)
        {
            long result = 0L;


            if (endian == Endians.Little)
            {
                int i1 = (int)(bytes[startIndex] | bytes[startIndex + 1] << 8
                    | bytes[startIndex + 2] << 16 | bytes[startIndex + 3] << 24);
                int i2 = (int)(bytes[startIndex + 4] | bytes[startIndex + 5] << 8
                    | bytes[startIndex + 6] << 16 | bytes[startIndex + 7] << 24);
                result = (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                int i1 = (int)(bytes[startIndex] << 24 | bytes[startIndex + 1] << 16
                    | bytes[startIndex + 2] << 8 | bytes[startIndex + 3]);
                int i2 = (int)(bytes[startIndex + 4] << 24 | bytes[startIndex + 5] << 16
                    | bytes[startIndex + 6] << 8 | bytes[startIndex + 7]);
                result = (uint)i2 | ((long)i1 << 32);
            }

            return result;
        }

        /// <summary>
        /// 返回由字节数组中指定位置的8个字节转换来的64位无符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>        
        public static ulong ToUInt64(byte[] bytes, int startIndex, Endians endian)
        {
            return (ulong)ToInt64(bytes, startIndex, endian);
        }


        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的32位有符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>        
        public static int ToInt32(byte[] bytes, int startIndex, Endians endian)
        {
            int result = 0;

            if (endian == Endians.Little)
            {
                result = (int)(bytes[startIndex] | bytes[startIndex + 1] << 8
                    | bytes[startIndex + 2] << 16 | bytes[startIndex + 3] << 24);
            }
            else
            {
                result = (int)(bytes[startIndex] << 24 | bytes[startIndex + 1] << 16
                    | bytes[startIndex + 2] << 8 | bytes[startIndex + 3]);
            }

            return result;
        }


        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的32位无符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>        
        public static uint ToUInt32(byte[] bytes, int startIndex, Endians endian)
        {
            return (uint)ToInt32(bytes, startIndex, endian);
        }

        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的16位有符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        public static short ToInt16(byte[] bytes, int startIndex, Endians endian)
        {
            short result = 0;

            if (endian == Endians.Little)
            {
                result = (short)(bytes[startIndex] | bytes[startIndex + 1] << 8);
            }
            else
            {
                result = (short)(bytes[startIndex] << 24 | bytes[startIndex + 1] << 16);
            }

            return result;
        }

        /// <summary>
        /// 返回由字节数组中指定位置的四个字节转换来的16位无符号整数
        /// </summary>
        /// <param name="bytes">字节数组</param>
        /// <param name="startIndex">位置</param>    
        /// <param name="endian">高低位</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <returns></returns>
        public static ushort ToUInt16(byte[] bytes, int startIndex, Endians endian)
        {
            return (ushort)ToInt16(bytes, startIndex, endian);
        }


        /// <summary>
        /// 返回由64位有符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(long value, Endians endian)
        {
            byte[] bytes = new byte[8];

            if (endian == Endians.Little)
            {
                bytes[0] = (byte)(value);
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
                bytes[4] = (byte)(value >> 32);
                bytes[5] = (byte)(value >> 40);
                bytes[6] = (byte)(value >> 48);
                bytes[7] = (byte)(value >> 56);
            }
            else
            {
                bytes[7] = (byte)(value);
                bytes[6] = (byte)(value >> 8);
                bytes[5] = (byte)(value >> 16);
                bytes[4] = (byte)(value >> 24);
                bytes[3] = (byte)(value >> 32);
                bytes[2] = (byte)(value >> 40);
                bytes[1] = (byte)(value >> 48);
                bytes[0] = (byte)(value >> 56);
            }
            return bytes;
        }


        /// <summary>
        /// 返回由64位无符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(ulong value, Endians endian)
        {
            return ToBytes((long)value, endian);
        }

        /// <summary>
        /// 返回由32位有符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(int value, Endians endian)
        {
            byte[] bytes = new byte[4];

            if (endian == Endians.Little)
            {
                bytes[0] = (byte)(value);
                bytes[1] = (byte)(value >> 8);
                bytes[2] = (byte)(value >> 16);
                bytes[3] = (byte)(value >> 24);
            }
            else
            {
                bytes[3] = (byte)(value);
                bytes[2] = (byte)(value >> 8);
                bytes[1] = (byte)(value >> 16);
                bytes[0] = (byte)(value >> 24);
            }

            return bytes;
        }

        /// <summary>
        /// 返回由32位无符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(uint value, Endians endian)
        {
            return ToBytes((int)value, endian);
        }

        /// <summary>
        /// 返回由16位有符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(short value, Endians endian)
        {
            byte[] bytes = new byte[2];

            if (endian == Endians.Little)
            {
                bytes[0] = (byte)(value);
                bytes[1] = (byte)(value >> 8);
            }
            else
            {
                bytes[1] = (byte)(value);
                bytes[0] = (byte)(value >> 8);
            }

            return bytes;
        }

        /// <summary>
        /// 返回由16位无符号整数转换为的字节数组
        /// </summary>
        /// <param name="value">整数</param>    
        /// <param name="endian">高低位</param>
        /// <returns></returns>
        public static byte[] ToBytes(ushort value, Endians endian)
        {
            return ToBytes((short)value, endian);
        }
    }

}
