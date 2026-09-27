// fake Span/ReadOnlySpan over arrays, enough for UltimaSDK. no ref structs
// on 2.0 so it's just (array, start, length).
// stackalloc into a Span ends up in the (void*, n) ctor, which ignores the
// pointer and allocates an array. fine for Razor's scratch buffers.
// MemoryMarshal.Cast/AsBytes copy, so read-only only (BwtDecompress has a
// NET20 path for the one place that writes through).

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System
{
    public readonly ref struct Span<T>
    {
        internal readonly T[] _array;
        internal readonly int _start;
        readonly int _length;

        public Span(T[] array)
        {
            _array = array ?? new T[0];
            _start = 0;
            _length = _array.Length;
        }

        public Span(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException("start");
                array = new T[0];
            }
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                throw new ArgumentOutOfRangeException("start");
            _array = array;
            _start = start;
            _length = length;
        }

        public unsafe Span(void* pointer, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            _array = new T[length];
            _start = 0;
            _length = length;
        }

        public static Span<T> Empty
        {
            get { return new Span<T>(new T[0]); }
        }

        public int Length
        {
            get { return _length; }
        }

        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        public ref T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_length)
                    throw new IndexOutOfRangeException();
                return ref _array[_start + index];
            }
        }

        public ref T GetPinnableReference()
        {
            if (_length == 0)
                return ref EmptyRef<T>.Value;
            return ref _array[_start];
        }

        public Span<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                throw new ArgumentOutOfRangeException("start");
            return new Span<T>(_array, _start + start, _length - start);
        }

        public Span<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                throw new ArgumentOutOfRangeException("start");
            return new Span<T>(_array, _start + start, length);
        }

        public void Clear()
        {
            Array.Clear(_array, _start, _length);
        }

        public void Fill(T value)
        {
            for (int i = 0; i < _length; i++)
                _array[_start + i] = value;
        }

        public void CopyTo(Span<T> destination)
        {
            if (!TryCopyTo(destination))
                throw new ArgumentException("Destination is too short.", "destination");
        }

        public bool TryCopyTo(Span<T> destination)
        {
            if (_length > destination.Length)
                return false;
            Array.Copy(_array, _start, destination._array, destination._start, _length);
            return true;
        }

        public T[] ToArray()
        {
            T[] result = new T[_length];
            Array.Copy(_array, _start, result, 0, _length);
            return result;
        }

        public override string ToString()
        {
            char[] chars = _array as char[];
            if (chars != null)
                return new string(chars, _start, _length);
            return string.Format("System.Span<{0}>[{1}]", typeof(T).Name, _length);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public static implicit operator Span<T>(T[] array)
        {
            return new Span<T>(array);
        }

        public static implicit operator Span<T>(ArraySegment<T> segment)
        {
            return new Span<T>(segment.Array, segment.Offset, segment.Count);
        }

        public static implicit operator ReadOnlySpan<T>(Span<T> span)
        {
            return new ReadOnlySpan<T>(span._array, span._start, span._length);
        }

        public ref struct Enumerator
        {
            readonly Span<T> _span;
            int _index;

            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            public bool MoveNext()
            {
                return ++_index < _span.Length;
            }

            public ref T Current
            {
                get { return ref _span[_index]; }
            }
        }
    }

    public readonly ref struct ReadOnlySpan<T>
    {
        internal readonly T[] _array;
        internal readonly int _start;
        readonly int _length;

        public ReadOnlySpan(T[] array)
        {
            _array = array ?? new T[0];
            _start = 0;
            _length = _array.Length;
        }

        public ReadOnlySpan(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    throw new ArgumentOutOfRangeException("start");
                array = new T[0];
            }
            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                throw new ArgumentOutOfRangeException("start");
            _array = array;
            _start = start;
            _length = length;
        }

        public unsafe ReadOnlySpan(void* pointer, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            _array = new T[length];
            _start = 0;
            _length = length;
        }

        public static ReadOnlySpan<T> Empty
        {
            get { return new ReadOnlySpan<T>(new T[0]); }
        }

        public int Length
        {
            get { return _length; }
        }

        public bool IsEmpty
        {
            get { return _length == 0; }
        }

        public ref readonly T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_length)
                    throw new IndexOutOfRangeException();
                return ref _array[_start + index];
            }
        }

        public ref readonly T GetPinnableReference()
        {
            if (_length == 0)
                return ref EmptyRef<T>.Value;
            return ref _array[_start];
        }

        public ReadOnlySpan<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                throw new ArgumentOutOfRangeException("start");
            return new ReadOnlySpan<T>(_array, _start + start, _length - start);
        }

        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                throw new ArgumentOutOfRangeException("start");
            return new ReadOnlySpan<T>(_array, _start + start, length);
        }

        public void CopyTo(Span<T> destination)
        {
            if (!TryCopyTo(destination))
                throw new ArgumentException("Destination is too short.", "destination");
        }

        public bool TryCopyTo(Span<T> destination)
        {
            if (_length > destination.Length)
                return false;
            Array.Copy(_array, _start, destination._array, destination._start, _length);
            return true;
        }

        public T[] ToArray()
        {
            T[] result = new T[_length];
            Array.Copy(_array, _start, result, 0, _length);
            return result;
        }

        public override string ToString()
        {
            char[] chars = _array as char[];
            if (chars != null)
                return new string(chars, _start, _length);
            return string.Format("System.ReadOnlySpan<{0}>[{1}]", typeof(T).Name, _length);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public static implicit operator ReadOnlySpan<T>(T[] array)
        {
            return new ReadOnlySpan<T>(array);
        }

        public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment)
        {
            return new ReadOnlySpan<T>(segment.Array, segment.Offset, segment.Count);
        }

        public ref struct Enumerator
        {
            readonly ReadOnlySpan<T> _span;
            int _index;

            internal Enumerator(ReadOnlySpan<T> span)
            {
                _span = span;
                _index = -1;
            }

            public bool MoveNext()
            {
                return ++_index < _span.Length;
            }

            public ref readonly T Current
            {
                get { return ref _span[_index]; }
            }
        }
    }

    internal static class EmptyRef<T>
    {
        // for GetPinnableReference on an empty span, never read
        internal static T Value;
    }

    public static class MemoryExtensions
    {
        public static ReadOnlySpan<char> AsSpan(this string text)
        {
            return text == null ? default(ReadOnlySpan<char>) : new ReadOnlySpan<char>(text.ToCharArray());
        }

        public static ReadOnlySpan<char> AsSpan(this string text, int start)
        {
            return AsSpan(text).Slice(start);
        }

        public static ReadOnlySpan<char> AsSpan(this string text, int start, int length)
        {
            return AsSpan(text).Slice(start, length);
        }

        public static Span<T> AsSpan<T>(this T[] array)
        {
            return new Span<T>(array);
        }

        public static Span<T> AsSpan<T>(this T[] array, int start)
        {
            return new Span<T>(array, start, (array == null ? 0 : array.Length) - start);
        }

        public static Span<T> AsSpan<T>(this T[] array, int start, int length)
        {
            return new Span<T>(array, start, length);
        }

        public static int IndexOf<T>(this Span<T> span, T value) where T : IEquatable<T>
        {
            return IndexOf((ReadOnlySpan<T>)span, value);
        }

        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T>
        {
            for (int i = 0; i < span.Length; i++)
                if (span[i].Equals(value))
                    return i;
            return -1;
        }

        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            if (value.Length == 0)
                return 0;
            for (int i = 0; i + value.Length <= span.Length; i++)
            {
                int j = 0;
                while (j < value.Length && span[i + j].Equals(value[j]))
                    j++;
                if (j == value.Length)
                    return i;
            }
            return -1;
        }

        public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            return IndexOf((ReadOnlySpan<T>)span, value);
        }

        public static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            if (span.Length != other.Length)
                return false;
            for (int i = 0; i < span.Length; i++)
                if (!span[i].Equals(other[i]))
                    return false;
            return true;
        }

        public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other) where T : IEquatable<T>
        {
            return SequenceEqual((ReadOnlySpan<T>)span, other);
        }

        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            return value.Length <= span.Length && SequenceEqual(span.Slice(0, value.Length), value);
        }

        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T>
        {
            return StartsWith((ReadOnlySpan<T>)span, value);
        }

        public static void Reverse<T>(this Span<T> span)
        {
            Array.Reverse(span._array, span._start, span.Length);
        }
    }
}

namespace System.Runtime.InteropServices
{
    public static class MemoryMarshal
    {
        public static ref T GetReference<T>(Span<T> span)
        {
            return ref span.GetPinnableReference();
        }

        public static ref T GetReference<T>(ReadOnlySpan<T> span)
        {
            if (span.Length == 0)
                return ref EmptyRef<T>.Value;
            return ref span._array[span._start];
        }

        // copies, see top of file
        public static ReadOnlySpan<TTo> Cast<TFrom, TTo>(ReadOnlySpan<TFrom> span)
            where TFrom : struct
            where TTo : struct
        {
            int fromSize = Marshal.SizeOf(typeof(TFrom));
            int toSize = Marshal.SizeOf(typeof(TTo));
            int bytes = span.Length * fromSize;
            TTo[] result = new TTo[bytes / toSize];
            Buffer.BlockCopy(span._array, span._start * fromSize, result, 0, result.Length * toSize);
            return new ReadOnlySpan<TTo>(result);
        }

        public static Span<TTo> Cast<TFrom, TTo>(Span<TFrom> span)
            where TFrom : struct
            where TTo : struct
        {
            ReadOnlySpan<TTo> copy = Cast<TFrom, TTo>((ReadOnlySpan<TFrom>)span);
            return new Span<TTo>(copy._array, copy._start, copy.Length);
        }

        public static Span<byte> AsBytes<T>(Span<T> span) where T : struct
        {
            return Cast<T, byte>(span);
        }

        public static ReadOnlySpan<byte> AsBytes<T>(ReadOnlySpan<T> span) where T : struct
        {
            return Cast<T, byte>(span);
        }
    }
}

namespace System.Buffers
{
    // no pooling, not worth it here
    public abstract class ArrayPool<T>
    {
        static readonly ArrayPool<T> s_shared = new NewArrayPool();

        public static ArrayPool<T> Shared
        {
            get { return s_shared; }
        }

        public abstract T[] Rent(int minimumLength);

        public abstract void Return(T[] array, bool clearArray = false);

        sealed class NewArrayPool : ArrayPool<T>
        {
            public override T[] Rent(int minimumLength)
            {
                return new T[minimumLength];
            }

            public override void Return(T[] array, bool clearArray = false)
            {
            }
        }
    }
}

namespace System.Buffers.Binary
{
    public static class BinaryPrimitives
    {
        static bool Fits(ReadOnlySpan<byte> source, int size)
        {
            return source.Length >= size;
        }

        static ulong Read(ReadOnlySpan<byte> source, int size, bool bigEndian)
        {
            ulong value = 0;
            for (int i = 0; i < size; i++)
            {
                int b = bigEndian ? i : size - 1 - i;
                value = (value << 8) | source[b];
            }
            return value;
        }

        static ulong ReadChecked(ReadOnlySpan<byte> source, int size, bool bigEndian)
        {
            if (!Fits(source, size))
                throw new ArgumentOutOfRangeException("source");
            return Read(source, size, bigEndian);
        }

        public static short ReadInt16LittleEndian(ReadOnlySpan<byte> source) { return (short)ReadChecked(source, 2, false); }
        public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source) { return (ushort)ReadChecked(source, 2, false); }
        public static int ReadInt32LittleEndian(ReadOnlySpan<byte> source) { return (int)ReadChecked(source, 4, false); }
        public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source) { return (uint)ReadChecked(source, 4, false); }
        public static long ReadInt64LittleEndian(ReadOnlySpan<byte> source) { return (long)ReadChecked(source, 8, false); }
        public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source) { return ReadChecked(source, 8, false); }
        public static short ReadInt16BigEndian(ReadOnlySpan<byte> source) { return (short)ReadChecked(source, 2, true); }
        public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source) { return (ushort)ReadChecked(source, 2, true); }
        public static int ReadInt32BigEndian(ReadOnlySpan<byte> source) { return (int)ReadChecked(source, 4, true); }
        public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source) { return (uint)ReadChecked(source, 4, true); }
        public static long ReadInt64BigEndian(ReadOnlySpan<byte> source) { return (long)ReadChecked(source, 8, true); }
        public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source) { return ReadChecked(source, 8, true); }

        public static bool TryReadInt16LittleEndian(ReadOnlySpan<byte> source, out short value) { bool ok = Fits(source, 2); value = ok ? (short)Read(source, 2, false) : (short)0; return ok; }
        public static bool TryReadUInt16LittleEndian(ReadOnlySpan<byte> source, out ushort value) { bool ok = Fits(source, 2); value = ok ? (ushort)Read(source, 2, false) : (ushort)0; return ok; }
        public static bool TryReadInt32LittleEndian(ReadOnlySpan<byte> source, out int value) { bool ok = Fits(source, 4); value = ok ? (int)Read(source, 4, false) : 0; return ok; }
        public static bool TryReadUInt32LittleEndian(ReadOnlySpan<byte> source, out uint value) { bool ok = Fits(source, 4); value = ok ? (uint)Read(source, 4, false) : 0; return ok; }
        public static bool TryReadInt64LittleEndian(ReadOnlySpan<byte> source, out long value) { bool ok = Fits(source, 8); value = ok ? (long)Read(source, 8, false) : 0; return ok; }
        public static bool TryReadUInt64LittleEndian(ReadOnlySpan<byte> source, out ulong value) { bool ok = Fits(source, 8); value = ok ? Read(source, 8, false) : 0; return ok; }
        public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> source, out short value) { bool ok = Fits(source, 2); value = ok ? (short)Read(source, 2, true) : (short)0; return ok; }
        public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> source, out ushort value) { bool ok = Fits(source, 2); value = ok ? (ushort)Read(source, 2, true) : (ushort)0; return ok; }
        public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> source, out int value) { bool ok = Fits(source, 4); value = ok ? (int)Read(source, 4, true) : 0; return ok; }
        public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> source, out uint value) { bool ok = Fits(source, 4); value = ok ? (uint)Read(source, 4, true) : 0; return ok; }
        public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> source, out long value) { bool ok = Fits(source, 8); value = ok ? (long)Read(source, 8, true) : 0; return ok; }
        public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> source, out ulong value) { bool ok = Fits(source, 8); value = ok ? Read(source, 8, true) : 0; return ok; }
    }
}
