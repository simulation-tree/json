using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace JSON
{
    /// <summary>
    /// Abstract object able to contain any JSON structure.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct JSONObject : IDisposable, ISerializable, IEquatable<JSONObject>
    {
        private Implementation* jsonObject;

        /// <summary>
        /// All properties of this JSON object.
        /// </summary>
        public readonly ReadOnlySpan<JSONProperty> Properties
        {
            get
            {
                ThrowIfDisposed();

                return jsonObject->properties.AsSpan();
            }
        }

        /// <summary>
        /// Amount of properties in this JSON object.
        /// </summary>
        public readonly int Count
        {
            get
            {
                ThrowIfDisposed();

                return jsonObject->properties.Count;
            }
        }

        /// <summary>
        /// Checks if this JSON object is disposed.
        /// </summary>
        public readonly bool IsDisposed => jsonObject is null;

        /// <summary>
        /// Indexer to access properties by the given <paramref name="index"/>.
        /// </summary>
        public readonly ref JSONProperty this[int index]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIndexIsOutOfRange(index);

                return ref jsonObject->properties[index];
            }
        }

        /// <summary>
        /// Indexer to access properties by the given <paramref name="name"/>.
        /// </summary>
        public readonly ref JSONProperty this[ReadOnlySpan<char> name]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIsMissing(name);

                Span<JSONProperty> properties = jsonObject->properties.AsSpan();
                for (int i = 0; i < properties.Length; i++)
                {
                    ref JSONProperty property = ref properties[i];
                    if (property.Name.SequenceEqual(name))
                    {
                        return ref property;
                    }
                }

                return ref Unsafe.AsRef<JSONProperty>(default);
            }
        }

        /// <summary>
        /// Indexer to access properties by the given <paramref name="name"/>.
        /// </summary>
        public readonly ref JSONProperty this[string name]
        {
            get
            {
                ThrowIfDisposed();
                ThrowIfPropertyIsMissing(name);

                Span<JSONProperty> properties = jsonObject->properties.AsSpan();
                for (int i = 0; i < properties.Length; i++)
                {
                    ref JSONProperty property = ref properties[i];
                    if (property.Name.SequenceEqual(name))
                    {
                        return ref property;
                    }
                }

                return ref Unsafe.AsRef<JSONProperty>(default);
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty JSON object.
        /// </summary>
        public JSONObject()
        {
            jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
        }
#endif

        /// <summary>
        /// Initializes an existing JSON object using the given <paramref name="pointer"/>.
        /// </summary>
        public JSONObject(void* pointer)
        {
            jsonObject = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ThrowIfDisposed();

            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            for (int i = 0; i < properties.Length; i++)
            {
                properties[i].Dispose();
            }

            jsonObject->properties.Dispose();
            MemoryAddress.Free(ref jsonObject);
        }

        /// <summary>
        /// Removes all properties of this JSON object.
        /// </summary>
        public readonly void Clear()
        {
            ThrowIfDisposed();

            jsonObject->properties.Clear();
        }

        /// <summary>
        /// Removes the property at the given <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveAt(int index)
        {
            ThrowIfDisposed();

            jsonObject->properties.RemoveAt(index);
        }

        /// <summary>
        /// Removes the property at the given <paramref name="index"/>,
        /// by swapping it with the last property in the list.
        /// </summary>
        public readonly void RemoveAtBySwapping(int index)
        {
            ThrowIfDisposed();

            jsonObject->properties.RemoveAtBySwapping(index);
        }

        /// <summary>
        /// Interprets this JSON object as a specific type <typeparamref name="T"/>,
        /// </summary>
        public readonly T As<T>() where T : unmanaged, IJSONSerializable
        {
            ThrowIfDisposed();

            T value = default;
            using Text result = new(0);
            ToString(result);
            using ByteReader reader = ByteReader.CreateFromUTF8(result.AsSpan());
            JSONReader jsonReader = new(reader);
            jsonReader.TryReadToken(out _);
            value.Read(jsonReader);
            return value;
        }

        /// <summary>
        /// Appends the JSON representation of this object to the given <paramref name="result"/> text buffer.
        /// </summary>
        public readonly void ToString(Text result, SerializationSettings settings = default)
        {
            ToString(result, settings, 0);
        }

        internal readonly void ToString(Text result, SerializationSettings settings, int depth)
        {
            ThrowIfDisposed();

            result.Append(Token.StartObject);
            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            if (properties.Length > 0)
            {
                settings.NewLine(result);
                for (int i = 0; i <= depth; i++)
                {
                    settings.Indent(result);
                }

                int position = 0;
                while (true)
                {
                    ref JSONProperty property = ref properties[position];
                    int childDepth = depth;
                    childDepth++;
                    result.Append('\"');
                    result.Append(property.Name);
                    result.Append('\"');
                    result.Append(':');
                    property.ToString(result, settings, childDepth);
                    position++;

                    if (position == Count)
                    {
                        break;
                    }

                    result.Append(',');
                    settings.NewLine(result);
                    for (int i = 0; i <= depth; i++)
                    {
                        settings.Indent(result);
                    }
                }

                settings.NewLine(result);
                for (int i = 0; i < depth; i++)
                {
                    settings.Indent(result);
                }
            }

            result.Append(Token.EndObject);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            ThrowIfDisposed();

            Text buffer = new(0);
            ToString(buffer);
            string result = buffer.ToString();
            buffer.Dispose();
            return result;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(JSONObject));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPropertyIsMissing(ReadOnlySpan<char> name)
        {
            if (!Contains(name))
            {
                throw new NullReferenceException($"Property `{name.ToString()}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPropertyIndexIsOutOfRange(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException($"Property index `{index}` is out of range");
            }
        }

        /// <summary>
        /// Appends a new <paramref name="text"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, text);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="text"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(string name, string text)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, text);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="number"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> name, double number)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, number);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="number"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(string name, double number)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, number);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="boolean"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> name, bool boolean)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, boolean);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="boolean"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(string name, bool boolean)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, boolean);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="jsonObject"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> name, JSONObject jsonObject)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonObject);
            this.jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="jsonObject"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(string name, JSONObject jsonObject)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonObject);
            this.jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="jsonArray"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(ReadOnlySpan<char> name, JSONArray jsonArray)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonArray);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <paramref name="jsonArray"/> JSON property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Add(string name, JSONArray jsonArray)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name, jsonArray);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <see langword="null"/> JSON property with the given <paramref name="name"/>,
        /// </summary>
        public readonly void AddNull(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Appends a new <see langword="null"/> JSON property with the given <paramref name="name"/>,
        /// </summary>
        public readonly void AddNull(string name)
        {
            ThrowIfDisposed();

            JSONProperty property = new(name);
            jsonObject->properties.Add(property);
        }

        /// <summary>
        /// Checks if this JSON object contains a property with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool Contains(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name.SequenceEqual(name))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this JSON object contains a property with the given <paramref name="name"/>.
        /// </summary>
        public readonly bool Contains(string name)
        {
            return Contains(name.AsSpan());
        }

        /// <summary>
        /// Assigns the <paramref name="text"/> to the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Set(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            JSONProperty property = this[name];
            property.Text = text;
        }

        /// <summary>
        /// Assigns the <paramref name="text"/> to the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly void Set(string name, string text)
        {
            Set(name.AsSpan(), text.AsSpan());
        }

        /// <summary>
        /// Retrieves the text contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> GetText(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Text;
        }

        /// <summary>
        /// Retrieves the text contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ReadOnlySpan<char> GetText(string name)
        {
            return GetText(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the number contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ref double GetNumber(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Number;
        }

        /// <summary>
        /// Retrieves the number contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ref double GetNumber(string name)
        {
            return ref GetNumber(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the boolean contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ref bool GetBoolean(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return ref this[name].Boolean;
        }

        /// <summary>
        /// Retrieves the boolean contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly ref bool GetBoolean(string name)
        {
            return ref GetBoolean(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the JSON object contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly JSONObject GetObject(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Object;
        }

        /// <summary>
        /// Retrieves the JSON object contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly JSONObject GetObject(string name)
        {
            return GetObject(name.AsSpan());
        }

        /// <summary>
        /// Retrieves the JSON array contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly JSONArray GetArray(ReadOnlySpan<char> name)
        {
            ThrowIfDisposed();

            return this[name].Array;
        }

        /// <summary>
        /// Retrieves the JSON array contained in the property with the given <paramref name="name"/>.
        /// </summary>
        public readonly JSONArray GetArray(string name)
        {
            return GetArray(name.AsSpan());
        }

        /// <summary>
        /// Tries to retrieve the text contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a text proeprty with the name was found.</returns>
        public readonly bool TryGetText(ReadOnlySpan<char> name, out ReadOnlySpan<char> text)
        {
            ThrowIfDisposed();

            if (!Contains(name))
            {
                text = default;
                return false;
            }

            return this[name].TryGetText(out text);
        }

        /// <summary>
        /// Tries to retrieve the text contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a text proeprty with the name was found.</returns>
        public readonly bool TryGetText(string name, out ReadOnlySpan<char> text)
        {
            return TryGetText(name.AsSpan(), out text);
        }

        /// <summary>
        /// Tries to retrieve the number value contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a number proeprty with the name was found.</returns>
        public readonly bool TryGetNumber(ReadOnlySpan<char> name, out double number)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                number = default;
                return false;
            }

            return this[name].TryGetNumber(out number);
        }

        /// <summary>
        /// Tries to retrieve the number value contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a number proeprty with the name was found.</returns>
        public readonly bool TryGetNumber(string name, out double number)
        {
            return TryGetNumber(name.AsSpan(), out number);
        }

        /// <summary>
        /// Tries to retrieve the boolean value contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a boolean proeprty with the name was found.</returns>
        public readonly bool TryGetBoolean(ReadOnlySpan<char> name, out bool boolean)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                boolean = default;
                return false;
            }

            return this[name].TryGetBoolean(out boolean);
        }

        /// <summary>
        /// Tries to retrieve the boolean value contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a boolean proeprty with the name was found.</returns>
        public readonly bool TryGetBoolean(string name, out bool boolean)
        {
            return TryGetBoolean(name.AsSpan(), out boolean);
        }

        /// <summary>
        /// Tries to retrieve the JSON object contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a JSON object proeprty with the name was found.</returns>
        public readonly bool TryGetObject(ReadOnlySpan<char> name, out JSONObject obj)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                obj = default;
                return false;
            }

            return this[name].TryGetObject(out obj);
        }

        /// <summary>
        /// Tries to retrieve the JSON object contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a JSON object proeprty with the name was found.</returns>
        public readonly bool TryGetObject(string name, out JSONObject obj)
        {
            return TryGetObject(name.AsSpan(), out obj);
        }

        /// <summary>
        /// Tries to retrieve the JSON array contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a JSON array proeprty with the name was found.</returns>
        public readonly bool TryGetArray(ReadOnlySpan<char> name, out JSONArray array)
        {
            ThrowIfDisposed();
            if (!Contains(name))
            {
                array = default;
                return false;
            }

            return this[name].TryGetArray(out array);
        }

        /// <summary>
        /// Tries to retrieve the JSON array contained in the property with the given <paramref name="name"/>.
        /// </summary>
        /// <returns><see langword="true"/> if a JSON array proeprty with the name was found.</returns>
        public readonly bool TryGetArray(string name, out JSONArray array)
        {
            return TryGetArray(name.AsSpan(), out array);
        }

        /// <summary>
        /// Loads this JSON object from the given <paramref name="sourceText"/>.
        /// </summary>
        public readonly void Overwrite(ReadOnlySpan<char> sourceText)
        {
            ThrowIfDisposed();

            //todo: this is cheating, because its allocating a new buffer just to reuse the same parse function, even though the source text is already here
            using ByteReader byteReader = ByteReader.CreateFromUTF8(sourceText);
            Overwrite(byteReader);
        }

        /// <summary>
        /// Loads this JSON object from the given <paramref name="byteReader"/>.
        /// </summary>
        public readonly void Overwrite(ByteReader byteReader)
        {
            ThrowIfDisposed();

            JSONReader jsonReader = new(byteReader);
            if (jsonReader.TryPeekToken(out Token nextToken, out int readBytes))
            {
                if (nextToken.type == Token.Type.StartObject)
                {
                    //start of object
                    byteReader.Advance(readBytes);
                }
            }

            //todo: share these temp buffers?
            using Text nameTextBuffer = new(256);
            using Text nextTextBuffer = new(256);
            while (jsonReader.TryReadToken(out Token token))
            {
                if (token.type == Token.Type.Value)
                {
                    int capacity = token.length * 4;
                    if (nameTextBuffer.Length < capacity)
                    {
                        nameTextBuffer.SetLength(capacity);
                    }

                    int nameTextLength = jsonReader.GetText(token, nameTextBuffer.AsSpan());
                    Span<char> name = nameTextBuffer.Slice(0, nameTextLength);
                    if (jsonReader.TryReadToken(out nextToken))
                    {
                        int nextCapacity = nextToken.length * 4;
                        if (nextTextBuffer.Length < nextCapacity)
                        {
                            nextTextBuffer.SetLength(nextCapacity);
                        }

                        if (nextToken.type == Token.Type.Value)
                        {
                            int nextTextLength = jsonReader.GetText(nextToken, nextTextBuffer.AsSpan());
                            ReadOnlySpan<char> nextText = nextTextBuffer.Slice(0, nextTextLength);
                            if (double.TryParse(nextText, out double number))
                            {
                                Add(name, number);
                            }
                            else if (nextText.SequenceEqual(Token.True))
                            {
                                Add(name, true);
                            }
                            else if (nextText.SequenceEqual(Token.False))
                            {
                                Add(name, false);
                            }
                            else if (nextText.SequenceEqual(Token.Null))
                            {
                                AddNull(name);
                            }
                            else
                            {
                                Add(name, nextText);
                            }
                        }
                        else if (nextToken.type == Token.Type.StartObject)
                        {
                            JSONObject newObject = byteReader.ReadObject<JSONObject>();
                            Add(name, newObject);
                        }
                        else if (nextToken.type == Token.Type.StartArray)
                        {
                            JSONArray newArray = byteReader.ReadObject<JSONArray>();
                            Add(name, newArray);
                        }
                        else if (nextToken.type == Token.Type.EndObject)
                        {
                            break;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Invalid JSON token at position {nextToken.position}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"No succeeding token available after {name.ToString()}");
                    }
                }
                else if (token.type == Token.Type.EndObject)
                {
                    break;
                }
                else
                {
                    throw new InvalidOperationException($"Unexpected token `{token.type}`, expected }} or another text token");
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Text list = new(0);
            ToString(list);
            writer.WriteUTF8(list.AsSpan());
            list.Dispose();
        }

        void ISerializable.Read(ByteReader reader)
        {
            jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
            Overwrite(reader);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is JSONObject jsonObject && Equals(jsonObject);
        }

        /// <inheritdoc/>
        public readonly bool Equals(JSONObject other)
        {
            if (IsDisposed != other.IsDisposed)
            {
                return false;
            }
            else if (IsDisposed && other.IsDisposed)
            {
                return true;
            }

            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            Span<JSONProperty> otherProperties = other.jsonObject->properties.AsSpan();
            if (properties.Length != otherProperties.Length)
            {
                return false;
            }

            for (int i = 0; i < properties.Length; i++)
            {
                ref JSONProperty property = ref properties[i];
                ref JSONProperty otherProperty = ref otherProperties[i];
                if (!property.Equals(otherProperty))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            ThrowIfDisposed();

            int hash = 17;
            Span<JSONProperty> properties = jsonObject->properties.AsSpan();
            for (int i = 0; i < properties.Length; i++)
            {
                ref JSONProperty property = ref properties[i];
                hash = hash * 31 + property.GetHashCode();
            }

            return hash;
        }

        /// <summary>
        /// Creates a new empty JSON object.
        /// </summary>
        public static JSONObject Create()
        {
            Implementation* jsonObject = MemoryAddress.AllocatePointer<Implementation>();
            jsonObject->properties = new(4);
            return new JSONObject(jsonObject);
        }

        /// <summary>
        /// Parses the given <paramref name="jsonText"/> as a new <see cref="JSONObject"/>.
        /// </summary>
        public static JSONObject Parse(ReadOnlySpan<char> jsonText)
        {
            JSONObject jsonObject = Create();
            jsonObject.Overwrite(jsonText);
            return jsonObject;
        }

        /// <summary>
        /// Tries to parse the given <paramref name="jsonText"/> as a new <see cref="JSONObject"/>.
        /// </summary>
        /// <returns><see langword="true"/> if successful.</returns>
        public static bool TryParse(ReadOnlySpan<char> jsonText, out JSONObject jsonObject)
        {
            Implementation* jsonObjectPointer = MemoryAddress.AllocatePointer<Implementation>();
            jsonObjectPointer->properties = new(4);
            jsonObject = new(jsonObjectPointer);
            try
            {
                jsonObject.Overwrite(jsonText);
                return true;
            }
            catch
            {
                jsonObject.Dispose();
                jsonObject = default;
                return false;
            }
        }

        /// <summary>
        /// Parses the given <paramref name="jsonBytes"/> as a new <see cref="JSONObject"/>.
        /// </summary>
        public static JSONObject Parse(ReadOnlySpan<byte> jsonBytes)
        {
            using ByteReader byteReader = new(jsonBytes);
            return byteReader.ReadObject<JSONObject>();
        }

        /// <summary>
        /// Tries to parse the given <paramref name="jsonBytes"/> as a new <see cref="JSONObject"/>.
        /// </summary>
        /// <returns><see langword="true"/> if successful.</returns>
        public static bool TryParse(ReadOnlySpan<byte> jsonBytes, out JSONObject jsonObject)
        {
            using ByteReader byteReader = new(jsonBytes);
            jsonObject = default;
            try
            {
                jsonObject = byteReader.ReadObject<JSONObject>();
            }
            catch
            {
                if (jsonObject != default)
                {
                    jsonObject.Dispose();
                    jsonObject = default;
                }
            }

            return jsonObject != default;
        }

        /// <inheritdoc/>
        public static bool operator ==(JSONObject left, JSONObject right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(JSONObject left, JSONObject right)
        {
            return !(left == right);
        }

        private struct Implementation
        {
            public List<JSONProperty> properties;
        }
    }
}