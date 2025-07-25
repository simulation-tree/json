using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace JSON
{
    /// <summary>
    /// A <see cref="ByteWriter"/> wrapper for reading JSON.
    /// </summary>
    [SkipLocalsInit]
    public ref struct JSONReader
    {
        private ByteReader reader;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not available", true)]
        public JSONReader()
        {
        }
#endif

        /// <summary>
        /// Creates a new wrapper around the given binary reader.
        /// </summary>
        public JSONReader(ByteReader reader)
        {
            this.reader = reader;
        }

        /// <summary>
        /// Tries to peek the next token in the stream without advancing the reader.
        /// </summary>
        /// <returns><see langword="true"/> if there was a token to read.</returns>
        public readonly bool TryPeekToken(out Token token)
        {
            return TryPeekToken(out token, out _);
        }

        /// <summary>
        /// Tries to peek the next token in the stream without advancing the reader.
        /// And populates the <paramref name="readBytes"/> with how many <see cref="byte"/>s
        /// were read.
        /// </summary>
        /// <returns><see langword="true"/> if there was a token to read.</returns>
        public readonly bool TryPeekToken(out Token token, out int readBytes)
        {
            token = default;
            int position = reader.Position;
            int length = reader.Length;
            while (position < length)
            {
                byte bytesRead = reader.PeekUTF8(position, out char c, out _);
                if (c == Token.StartObject)
                {
                    token = new Token(position, bytesRead, Token.Type.StartObject);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == Token.EndObject)
                {
                    token = new Token(position, bytesRead, Token.Type.EndObject);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == Token.StartArray)
                {
                    token = new Token(position, bytesRead, Token.Type.StartArray);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == Token.EndArray)
                {
                    token = new Token(position, bytesRead, Token.Type.EndArray);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == Token.Aggregator || c == Token.Separator || SharedFunctions.IsWhiteSpace(c))
                {
                    position += bytesRead;
                }
                else if (c == Token.DoubleQuote)
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        if (c == Token.DoubleQuote)
                        {
                            token = new Token(start, position - start, Token.Type.Value);
                            readBytes = position - reader.Position + 2;
                            return true;
                        }

                        position += bytesRead;
                    }
                }
                else if (c == Token.SingleQuote)
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        if (c == Token.SingleQuote)
                        {
                            token = new Token(start, position - start, Token.Type.Value);
                            readBytes = position - reader.Position + 2;
                            return true;
                        }

                        position += bytesRead;
                    }
                }
                else
                {
                    int start = position;
                    position += bytesRead;
                    while (position < length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        if (c == Token.StartObject || c == Token.EndObject || c == Token.StartArray || c == Token.EndArray || c == Token.Aggregator || c == Token.Separator || SharedFunctions.IsWhiteSpace(c))
                        {
                            token = new Token(start, position - start, Token.Type.Value);
                            readBytes = position - reader.Position;
                            return true;
                        }

                        position += bytesRead;
                    }

                    throw new InvalidOperationException($"Unexpected end of stream while reading token, expected a JSON token to finish the text");
                }
            }

            readBytes = default;
            return false;
        }

        /// <summary>
        /// Reads the next token and advances the reader by the amount of <see cref="byte"/>s read.
        /// </summary>
        public readonly Token ReadToken()
        {
            TryPeekToken(out Token token, out int readBytes);
            reader.Advance(readBytes);
            return token;
        }

        /// <summary>
        /// Tries to read the next token and advances the reader by the amount of <see cref="byte"/>s read.
        /// </summary>
        /// <returns><see langword="true"/> if a token was read.</returns>
        public readonly bool TryReadToken(out Token token)
        {
            bool read = TryPeekToken(out token, out int readBytes);
            reader.Advance(readBytes);
            return read;
        }

        /// <summary>
        /// Copies the text of the next token in this reader into the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied into the <paramref name="destination"/>.</returns>
        public readonly int ReadText(Span<char> destination)
        {
            while (TryReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Value)
                {
                    return GetText(token, destination);
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for text but none found");
        }

        /// <summary>
        /// Reads a <see cref="double"/> value from the next token.
        /// </summary>
        public readonly double ReadNumber()
        {
            while (TryReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Value)
                {
                    return GetNumber(token);
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for number but none found");
        }

        /// <summary>
        /// Reads a <see cref="bool"/> value from the next token.
        /// </summary>
        public readonly bool ReadBoolean()
        {
            Span<char> buffer = stackalloc char[32];
            while (TryReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Value)
                {
                    int length = GetText(token, buffer);
                    if (length == Token.True.Length && buffer.Slice(0, length).SequenceEqual(Token.True))
                    {
                        return true;
                    }
                    else if (length == Token.False.Length && buffer.Slice(0, length).SequenceEqual(Token.False))
                    {
                        return false;
                    }

                    throw new InvalidOperationException($"Could not parse {buffer.Slice(0, length).ToString()} as a boolean");
                }
                else
                {
                    throw new InvalidOperationException($"Expected token for property name but found {token.type}");
                }
            }

            throw new InvalidOperationException("Expected token for boolean but none more found");
        }

        /// <summary>
        /// Reads the succeeding tokens into a new <typeparamref name="T"/> instance.
        /// </summary>
        public readonly T ReadObject<T>() where T : unmanaged, IJSONSerializable
        {
            while (TryReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray || token.type == Token.Type.Value)
                {
                    //skip
                }
                else if (token.type == Token.Type.StartObject)
                {
                    T obj = default;
                    obj.Read(this);
                    if (TryPeekToken(out Token peek, out int readBytes) && peek.type == Token.Type.EndObject)
                    {
                        reader.Advance(readBytes);
                        //reached end of object
                    }

                    return obj;
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected start object token");
        }

        /// <summary>
        /// Copies the text of the given <paramref name="token"/> into the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int GetText(Token token, Span<char> destination)
        {
            return reader.PeekUTF8(token.position, token.length, destination);
        }

        /// <summary>
        /// Reads a <see cref="double"/> value from the given <paramref name="token"/>.
        /// </summary>
        public readonly double GetNumber(Token token)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            return double.Parse(buffer.Slice(0, length));
        }

        /// <summary>
        /// Reads a <see cref="bool"/> value from the given <paramref name="token"/>.
        /// </summary>
        public readonly bool GetBoolean(Token token)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            return buffer.Slice(0, length).SequenceEqual(Token.True);
        }
    }
}