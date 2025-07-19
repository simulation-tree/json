using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace JSON
{
    /// <summary>
    /// Represents a generic JSON property.
    /// </summary>
    [SkipLocalsInit]
    public struct JSONProperty : IDisposable, IEquatable<JSONProperty>
    {
        private readonly Text name;
        private MemoryAddress data;
        private int length;

        /// <summary>
        /// The type of this property.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Checks if this property is a text property.
        /// </summary>
        public readonly bool IsText
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Text;
            }
        }

        /// <summary>
        /// Checks if this property is a number property.
        /// </summary>
        public readonly bool IsNumber
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Number;
            }
        }

        /// <summary>
        /// Checks if this property is a boolean property.
        /// </summary>
        public readonly bool IsBoolean
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Boolean;
            }
        }

        /// <summary>
        /// Checks if this property is an object property.
        /// </summary>
        public readonly bool IsObject
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Object;
            }
        }

        /// <summary>
        /// Checks if this property is an array property.
        /// </summary>
        public readonly bool IsArray
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Array;
            }
        }

        /// <summary>
        /// Checks if this property contains a <see langword="null"/> property.
        /// </summary>
        public readonly bool IsNull
        {
            get
            {
                ThrowIfDisposed();

                return type == Type.Null;
            }
        }

        /// <summary>
        /// The name of this property.
        /// </summary>
        public readonly ReadOnlySpan<char> Name
        {
            get
            {
                ThrowIfDisposed();

                return name.AsSpan();
            }
        }

        /// <summary>
        /// Checks if this property has been disposed.
        /// </summary>
        public readonly bool IsDisposed => name.IsDisposed;

        /// <summary>
        /// The text value of this property.
        /// </summary>
        public ReadOnlySpan<char> Text
        {
            readonly get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Text);

                return data.GetSpan<char>(length / sizeof(char));
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Text);

                int newLength = value.Length * sizeof(char);
                if (length < newLength)
                {
                    MemoryAddress.Resize(ref data, newLength);
                }

                length = newLength;
                data.Write(0, value);
            }
        }

        /// <summary>
        /// The number value of this property.
        /// </summary>
        public readonly ref double Number
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Number);

                return ref data.Read<double>();
            }
        }

        /// <summary>
        /// The boolean value of this property.
        /// </summary>
        public readonly ref bool Boolean
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Boolean);

                return ref data.Read<bool>();
            }
        }

        /// <summary>
        /// The JSON object value of this property.
        /// </summary>
        public readonly JSONObject Object
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Object);

                return data.Read<JSONObject>();
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Object);

                data.Read<JSONObject>().Dispose();
                data.Write(0, value);
            }
        }

        /// <summary>
        /// The JSON array value of this property.
        /// </summary>
        public readonly JSONArray Array
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Array);

                return data.Read<JSONArray>();
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfTypeMismatch(Type.Array);

                data.Read<JSONArray>().Dispose();
                data.Write(0, value);
            }
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public JSONProperty()
        {
        }
#endif

        /// <summary>
        /// Creates a new <paramref name="text"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            this.name = new(name);
            length = text.Length * sizeof(char);
            data = MemoryAddress.Allocate(length);
            data.Write(0, text);
            type = Type.Text;
        }

        /// <summary>
        /// Creates a new <paramref name="number"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name, double number)
        {
            this.name = new(name);
            data = MemoryAddress.AllocateValue(number, out length);
            type = Type.Number;
        }

        /// <summary>
        /// Creates a new <paramref name="boolean"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name, bool boolean)
        {
            this.name = new(name);
            data = MemoryAddress.AllocateValue(boolean, out length);
            type = Type.Boolean;
        }

        /// <summary>
        /// Creates a new JSON property with the given <paramref name="name"/> and <paramref name="jsonObject"/>.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name, JSONObject jsonObject)
        {
            this.name = new(name);
            data = MemoryAddress.AllocateValue(jsonObject, out length);
            type = Type.Object;
        }

        /// <summary>
        /// Creates a new JSON property with the given <paramref name="name"/> and <paramref name="jsonArray"/>.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name, JSONArray jsonArray)
        {
            this.name = new(name);
            data = MemoryAddress.AllocateValue(jsonArray, out length);
            type = Type.Array;
        }

        /// <summary>
        /// Creates a null property.
        /// </summary>
        public JSONProperty(ReadOnlySpan<char> name)
        {
            this.name = new(name);
            length = 0;
            data = default;
            type = Type.Null;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(JSONProperty), "The JSON property has been disposed");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTypeMismatch(Type desiredType)
        {
            if (type != desiredType)
            {
                throw new InvalidOperationException($"Property is not of type {desiredType}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTypeIsUninitialized()
        {
            if (type == default)
            {
                throw new InvalidOperationException("Property type is uninitialized or unknown");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ThrowIfDisposed();

            if (type == Type.Object)
            {
                JSONObject jsonObject = data.Read<JSONObject>();
                jsonObject.Dispose();
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = data.Read<JSONArray>();
                jsonArray.Dispose();
            }

            name.Dispose();
            data.Dispose();
        }

        /// <summary>
        /// Appends the string representation of this property to the given <paramref name="result"/>.
        /// </summary>
        public readonly void ToString(Text result, SerializationSettings settings = default)
        {
            ToString(result, settings, 0);
        }

        internal readonly void ToString(Text result, SerializationSettings settings, int depth)
        {
            ThrowIfDisposed();
            ThrowIfTypeIsUninitialized();

            if (type == Type.Text)
            {
                settings.WriteQuoteCharacter(result);
                result.Append(Text);
                settings.WriteQuoteCharacter(result);
            }
            else if (type == Type.Number)
            {
                double number = data.Read<double>();
                Span<char> buffer = stackalloc char[64];
                int length = number.ToString(buffer);
                result.Append(buffer.Slice(0, length));
            }
            else if (type == Type.Boolean)
            {
                result.Append(data.Read<bool>() ? Token.True : Token.False);
            }
            else if (type == Type.Object)
            {
                JSONObject jsonObject = data.Read<JSONObject>();
                jsonObject.ToString(result, settings, depth);
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = data.Read<JSONArray>();
                jsonArray.ToString(result, settings, depth);
            }
            else if (type == Type.Null)
            {
                result.Append(Token.Null);
            }
            else
            {
                throw new InvalidOperationException($"Property is of an unknown type: {type}");
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            ThrowIfDisposed();

            if (type == Type.Text)
            {
                return Text.ToString();
            }
            else if (type == Type.Number)
            {
                double number = Number;
                Span<char> buffer = stackalloc char[64];
                int length = number.ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }
            else if (type == Type.Boolean)
            {
                return Boolean ? Token.True : Token.False;
            }
            else if (type == Type.Object)
            {
                JSONObject jsonObject = data.Read<JSONObject>();
                return jsonObject.ToString();
            }
            else if (type == Type.Array)
            {
                JSONArray jsonArray = data.Read<JSONArray>();
                return jsonArray.ToString();
            }
            else if (type == Type.Null)
            {
                return Token.Null;
            }
            else
            {
                throw new InvalidOperationException($"Property is of an unknown type: {type}");
            }
        }

        /// <summary>
        /// Tries to retrieve the <paramref name="text"/> of this property.
        /// </summary>
        /// <returns><see langword="true"/> if this property is a text property.</returns>
        public readonly bool TryGetText(out ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            if (type == Type.Text)
            {
                text = Text;
                return true;
            }

            text = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the <paramref name="number"/> of this property.
        /// </summary>
        /// <returns><see langword="true"/> if this property is a number property.</returns>
        public readonly bool TryGetNumber(out double number)
        {
            ThrowIfDisposed();

            if (type == Type.Number)
            {
                number = data.Read<double>();
                return true;
            }

            number = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the <paramref name="boolean"/> of this property.
        /// </summary>
        /// <returns><see langword="true"/> if this property is a boolean property.</returns>
        public readonly bool TryGetBoolean(out bool boolean)
        {
            ThrowIfDisposed();

            if (type == Type.Boolean)
            {
                boolean = data.Read<bool>();
                return true;
            }

            boolean = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the <paramref name="jsonObject"/> of this property.
        /// </summary>
        /// <returns><see langword="true"/> if this property is an object property.</returns>
        public readonly bool TryGetObject(out JSONObject jsonObject)
        {
            ThrowIfDisposed();

            if (type == Type.Object)
            {
                jsonObject = data.Read<JSONObject>();
                return true;
            }

            jsonObject = default;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the <paramref name="jsonArray"/> of this property.
        /// </summary>
        /// <returns><see langword="true"/> if this property is an array property.</returns>
        public readonly bool TryGetArray(out JSONArray jsonArray)
        {
            ThrowIfDisposed();

            if (type == Type.Array)
            {
                jsonArray = data.Read<JSONArray>();
                return true;
            }

            jsonArray = default;
            return false;
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is JSONProperty property && Equals(property);
        }

        /// <inheritdoc/>
        public readonly bool Equals(JSONProperty other)
        {
            if (IsDisposed != other.IsDisposed)
            {
                return false;
            }
            else if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            return type == other.type && length == other.length && name.Equals(other.name) && data.GetSpan(length).SequenceEqual(other.data.GetSpan(other.length));
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            ThrowIfDisposed();

            int hash = 17;
            hash = hash * 31 + type.GetHashCode();
            hash = hash * 31 + length;
            hash = hash * 31 + name.GetHashCode();
            Span<byte> bytes = data.GetSpan(length);
            for (int i = 0; i < length; i++)
            {
                hash = hash * 31 + bytes[i];
            }

            return hash;
        }

        /// <inheritdoc/>
        public static bool operator ==(JSONProperty left, JSONProperty right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(JSONProperty left, JSONProperty right)
        {
            return !(left == right);
        }

        /// <summary>
        /// JSON property types.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// Uninitialized or unknown type.
            /// </summary>
            Unknown,

            /// <summary>
            /// Text property type.
            /// </summary>
            Text,

            /// <summary>
            /// Number property type.
            /// </summary>
            Number,

            /// <summary>
            /// Boolean property type.
            /// </summary>
            Boolean,

            /// <summary>
            /// Object property type.
            /// </summary>
            Object,

            /// <summary>
            /// Array property type.
            /// </summary>
            Array,

            /// <summary>
            /// Null property type.
            /// </summary>
            Null,
        }
    }
}