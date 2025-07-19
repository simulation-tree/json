using System;

namespace JSON
{
    /// <summary>
    /// A JSON token.
    /// </summary>
    public readonly struct Token
    {
        /// <summary>
        /// Token character for starting a JSON object.
        /// </summary>
        public const char StartObject = '{';

        /// <summary>
        /// Token character for ending a JSON object.
        /// </summary>
        public const char EndObject = '}';

        /// <summary>
        /// Token character for starting a JSON array.
        /// </summary>
        public const char StartArray = '[';

        /// <summary>
        /// Token character for ending a JSON array.
        /// </summary>
        public const char EndArray = ']';

        /// <summary>
        /// Token character for separating JSON values in an array or object.
        /// </summary>
        public const char Aggregator = ',';

        /// <summary>
        /// Token character for separating a JSON property name from its value.
        /// </summary>
        public const char Separator = ':';

        /// <summary>
        /// Double quotes used for JSON strings.
        /// </summary>
        public const char DoubleQuote = '"';

        /// <summary>
        /// Single quotes used for JSON5 strings.
        /// </summary>
        public const char SingleQuote = '\'';

        /// <summary>
        /// Text constant for a JSON <see langword="true"/> boolean.
        /// </summary>
        public const string True = "true";

        /// <summary>
        /// Text constant for a JSON <see langword="true"/> boolean.
        /// </summary>
        public const string False = "false";

        /// <summary>
        /// Text constant for a JSON <see langword="null"/>.
        /// </summary>
        public const string Null = "null";

        /// <summary>
        /// The start position of this token.
        /// </summary>
        public readonly int position;

        /// <summary>
        /// The length of this token.
        /// </summary>
        public readonly int length;

        /// <summary>
        /// The type of the token.
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="Token"/> struct.
        /// </summary>
        public Token(int position, int length, Type type)
        {
            this.position = position;
            this.length = length;
            this.type = type;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"JSONToken (type: {type}, position: {position}, length: {length})";
        }

        /// <summary>
        /// Retrieves the text that this token represents from the given <paramref name="reader"/>.
        /// </summary>
        public readonly string GetText(JSONReader reader)
        {
            Span<char> buffer = stackalloc char[length];
            int read = reader.GetText(this, buffer);
            return buffer.Slice(0, read).ToString();
        }

        /// <summary>
        /// Copies the text that this token represents from the given <paramref name="reader"/>,
        /// into the <paramref name="destination"/> span.
        /// </summary>
        /// <returns>Amount of <see cref="char"/>s copied.</returns>
        public readonly int GetText(JSONReader reader, Span<char> destination)
        {
            return reader.GetText(this, destination);
        }

        /// <summary>
        /// JSON token type.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// Uninitialized or not recognized.
            /// </summary>
            Unknown,

            /// <summary>
            /// Start of a JSON object.
            /// </summary>
            StartObject,

            /// <summary>
            /// End of a JSON object.
            /// </summary>
            EndObject,

            /// <summary>
            /// Start of a JSON array.
            /// </summary>
            StartArray,

            /// <summary>
            /// End of a JSON array.
            /// </summary>
            EndArray,

            /// <summary>
            /// JSON value token.
            /// </summary>
            Value,
        }
    }
}
