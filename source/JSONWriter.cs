using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace JSON
{
    /// <summary>
    /// Writer for JSON data.
    /// </summary>
    [SkipLocalsInit]
    public struct JSONWriter : IDisposable
    {
        private readonly SerializationSettings settings;
        private readonly ByteWriter writer;
        private Token last;
        private int depth;

        /// <summary>
        /// Checks if this writer has been disposed.
        /// </summary>
        public readonly bool IsDisposed => writer.IsDisposed;

        /// <summary>
        /// The current position in the writer in <see cref="byte"/>s.
        /// </summary>
        public readonly int Position => writer.Position;

#if NET
        /// <summary>
        /// Initializes a new writer.
        /// </summary>
        public JSONWriter()
        {
            settings = default;
            writer = new ByteWriter(4);
            last = default;
        }
#endif

        /// <summary>
        /// Initializes a new writer with the given <paramref name="settings"/>.
        /// </summary>
        public JSONWriter(SerializationSettings settings)
        {
            this.settings = settings;
            writer = new ByteWriter(4);
            last = default;
        }

        /// <summary>
        /// Retrieves JSON formatted text from this writer.
        /// </summary>
        public override readonly string ToString()
        {
            ByteReader reader = new(AsSpan());
            Text tempBuffer = new(Position * 3);
            Span<char> buffer = tempBuffer.AsSpan();
            int read = reader.ReadUTF8(buffer);
            reader.Dispose();
            string result = buffer.Slice(0, read).ToString();
            tempBuffer.Dispose();
            return result;
        }

        /// <summary>
        /// All <see cref="byte"/>s written.
        /// </summary>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return writer.AsSpan();
        }

        /// <inheritdoc/>
        public readonly void Dispose()
        {
            writer.Dispose();
        }

        /// <summary>
        /// Appends a <see cref="Token.Type.StartObject"/> token.
        /// </summary>
        public void WriteStartObject()
        {
            if (last.type == Token.Type.EndObject)
            {
                writer.WriteUTF8(Token.Aggregator);
                settings.NewLine(writer);
            }

            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.StartObject);
            writer.WriteUTF8(Token.StartObject);
            settings.NewLine(writer);
            depth++;
        }

        /// <summary>
        /// Appends a <see cref="Token.Type.EndObject"/> token.
        /// </summary>
        public void WriteEndObject()
        {
            depth--;
            settings.NewLine(writer);
            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.EndObject);
            writer.WriteUTF8(Token.EndObject);
        }

        /// <summary>
        /// Appends a <see cref="Token.Type.StartArray"/> token.
        /// </summary>
        public void WriteStartArray()
        {
            last = new(writer.Position, sizeof(char), Token.Type.StartArray);
            writer.WriteUTF8(Token.StartArray);
            settings.NewLine(writer);
            depth++;
        }

        /// <summary>
        /// Appends a <see cref="Token.Type.EndArray"/> token.
        /// </summary>
        public void WriteEndArray()
        {
            depth--;
            settings.NewLine(writer);
            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char), Token.Type.EndArray);
            writer.WriteUTF8(Token.EndArray);
        }

        private void WriteText(ReadOnlySpan<char> value, SerializationSettings settings)
        {
            last = new(writer.Position, sizeof(char) * (2 + value.Length), Token.Type.Value);
            settings.WriteQuoteCharacter(writer);
            writer.WriteUTF8(value);
            settings.WriteQuoteCharacter(writer);
        }

        /// <summary>
        /// Writes the given text value assuming its an element inside an array.
        /// </summary>
        public void WriteTextElement(ReadOnlySpan<char> value)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8(Token.Aggregator);
                settings.NewLine(writer);
            }

            WriteText(value, settings);
        }

        /// <summary>
        /// Appends a new token for the given <paramref name="number"/>.
        /// </summary>
        public void WriteNumber(double number)
        {
            Span<char> buffer = stackalloc char[32];
            int length = number.ToString(buffer);

            last = new(writer.Position, sizeof(char) * length, Token.Type.Value);
            writer.WriteUTF8(buffer.Slice(0, length));
        }

        /// <summary>
        /// Appends a new token for the given <paramref name="boolean"/>.
        /// </summary>
        public void WriteBoolean(bool boolean)
        {
            if (boolean)
            {
                last = new(writer.Position, sizeof(char) * 4, Token.Type.Value);
                writer.WriteUTF8(Token.True);
            }
            else
            {
                last = new(writer.Position, sizeof(char) * 5, Token.Type.Value);
                writer.WriteUTF8(Token.False);
            }
        }

        /// <summary>
        /// Appends a new <see langword="null"/> token.
        /// </summary>
        public void WriteNull()
        {
            last = new(writer.Position, sizeof(char) * 4, Token.Type.Value);
            writer.WriteUTF8(Token.Null);
        }

        /// <summary>
        /// Appends a new JSON object for the given <paramref name="obj"/>.
        /// </summary>
        public void WriteObject<T>(T obj) where T : unmanaged, IJSONSerializable
        {
            WriteStartObject();
            obj.Write(ref this);
            WriteEndObject();
        }

        /// <summary>
        /// Writes only the name of the property.
        /// </summary>
        public void WriteName(ReadOnlySpan<char> name)
        {
            if (last.type != Token.Type.StartObject && last.type != Token.Type.StartArray && last.type != Token.Type.Unknown)
            {
                writer.WriteUTF8(Token.Aggregator);
                settings.NewLine(writer);
            }

            for (int i = 0; i < depth; i++)
            {
                settings.Indent(writer);
            }

            last = new(writer.Position, sizeof(char) * (2 + name.Length), Token.Type.Value);
            settings.WriteQuoteCharacterForName(writer);
            writer.WriteUTF8(name);
            settings.WriteQuoteCharacterForName(writer);
            writer.WriteUTF8(':');
            settings.SpaceAfterColon(writer);
        }

        /// <summary>
        /// Writes only the name of the property.
        /// </summary>
        /// <param name="name"></param>
        public void WriteName(string name)
        {
            WriteName(name.AsSpan());
        }

        /// <summary>
        /// Writes the given <paramref name="items"/> as a new JSON array with the given <paramref name="name"/>.
        /// </summary>
        public void WriteArray<T>(ReadOnlySpan<char> name, ReadOnlySpan<T> items) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteStartArray();
            for (int i = 0; i < items.Length; i++)
            {
                WriteObject(items[i]);
            }

            WriteEndArray();
        }

        /// <summary>
        /// Writes the given <paramref name="items"/> as a new JSON array with the given <paramref name="name"/>.
        /// </summary>
        public void WriteArray<T>(string name, ReadOnlySpan<T> items) where T : unmanaged, IJSONSerializable
        {
            WriteArray(name.AsSpan(), items);
        }

        /// <summary>
        /// Writes a <paramref name="text"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(ReadOnlySpan<char> name, ReadOnlySpan<char> text)
        {
            WriteName(name);
            WriteText(text, settings);
        }

        /// <summary>
        /// Writes a <paramref name="text"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(string name, ReadOnlySpan<char> text)
        {
            WriteProperty(name.AsSpan(), text);
        }

        /// <summary>
        /// Writes a <paramref name="number"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(ReadOnlySpan<char> name, double number)
        {
            WriteName(name);
            WriteNumber(number);
        }

        /// <summary>
        /// Writes a <paramref name="number"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(string name, double number)
        {
            WriteProperty(name.AsSpan(), number);
        }

        /// <summary>
        /// Writes a <paramref name="boolean"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(ReadOnlySpan<char> name, bool boolean)
        {
            WriteName(name);
            WriteBoolean(boolean);
        }

        /// <summary>
        /// Writes a <paramref name="boolean"/> property with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty(string name, bool boolean)
        {
            WriteProperty(name.AsSpan(), boolean);
        }

        /// <summary>
        /// Writes a JSON object property containing <paramref name="obj"/> with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty<T>(ReadOnlySpan<char> name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteName(name);
            WriteObject(obj);
        }

        /// <summary>
        /// Writes a JSON object property containing <paramref name="obj"/> with the given <paramref name="name"/>.
        /// </summary>
        public void WriteProperty<T>(string name, T obj) where T : unmanaged, IJSONSerializable
        {
            WriteProperty(name.AsSpan(), obj);
        }

        /// <summary>
        /// Creates a new empty JSON writer.
        /// </summary>
        public static JSONWriter Create(SerializationSettings settings = default)
        {
            return new(settings);
        }
    }
}