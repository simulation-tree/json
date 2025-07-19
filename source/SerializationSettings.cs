using System;
using Unmanaged;

namespace JSON
{
    /// <summary>
    /// Settings to customize JSON serialization behaviour.
    /// </summary>
    public struct SerializationSettings
    {
        /// <summary>
        /// The default amount of spaces.
        /// </summary>
        public const int DefaultIndentation = 4;

        /// <summary>
        /// Settings for tightly packed JSON serialization.
        /// </summary>
        public static readonly SerializationSettings Default = new(Flags.None, 0);

        /// <summary>
        /// Settings for tighly packed JSON5 serialization.
        /// </summary>
        public static readonly SerializationSettings JSON5 = new(Flags.QuotelessNames | Flags.SingleQuotedText, 0);

        /// <summary>
        /// Settings for pretty printed JSON serialization.
        /// </summary>
        public static readonly SerializationSettings PrettyPrinted = new(Flags.CarrierReturn | Flags.LineFeed | Flags.SpaceAfterColon, DefaultIndentation);

        /// <summary>
        /// Settings for pretty printed JSON5 serialization.
        /// </summary>
        public static readonly SerializationSettings JSON5PrettyPrinted = new(Flags.CarrierReturn | Flags.LineFeed | Flags.QuotelessNames | Flags.SingleQuotedText | Flags.SpaceAfterColon, DefaultIndentation);

        /// <summary>
        /// Contains flags that modify the serialization behaviour.
        /// </summary>
        public Flags flags;

        /// <summary>
        /// Indentation level.
        /// </summary>
        public int indent;

        /// <summary>
        /// Initializes a new instance with the given <paramref name="flags"/> and <paramref name="indent"/>.
        /// </summary>
        public SerializationSettings(Flags flags, int indent)
        {
            this.flags = flags;
            this.indent = indent;
        }

        /// <summary>
        /// Appends indentation to the given <paramref name="text"/>.
        /// </summary>
        public readonly void Indent(Text text)
        {
            text.Append(' ', indent);
        }

        /// <summary>
        /// Appends indentation to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void Indent(ByteWriter writer)
        {
            writer.WriteUTF8(' ', indent);
        }

        /// <summary>
        /// Appends a new line to the given <paramref name="text"/>.
        /// </summary>
        public readonly void NewLine(Text text)
        {
            if ((flags & Flags.CarrierReturn) != 0)
            {
                text.Append('\r');
            }

            if ((flags & Flags.LineFeed) != 0)
            {
                text.Append('\n');
            }
        }

        /// <summary>
        /// Appends a new line to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void NewLine(ByteWriter writer)
        {
            if ((flags & Flags.CarrierReturn) != 0)
            {
                writer.WriteUTF8('\r');
            }

            if ((flags & Flags.LineFeed) != 0)
            {
                writer.WriteUTF8('\n');
            }
        }

        /// <summary>
        /// Writes a quote character for text to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void WriteQuoteCharacter(ByteWriter writer)
        {
            if ((flags & Flags.SingleQuotedText) != 0)
            {
                writer.WriteUTF8(Token.SingleQuote);
            }
            else
            {
                writer.WriteUTF8(Token.DoubleQuote);
            }
        }

        /// <summary>
        /// Writes a quote character for text to the given <paramref name="text"/>.
        /// </summary>
        public readonly void WriteQuoteCharacter(Text text)
        {
            if ((flags & Flags.SingleQuotedText) != 0)
            {
                text.Append(Token.SingleQuote);
            }
            else
            {
                text.Append(Token.DoubleQuote);
            }
        }

        /// <summary>
        /// Writes a quote character for names to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void WriteQuoteCharacterForName(ByteWriter writer)
        {
            if ((flags & Flags.QuotelessNames) == 0)
            {
                WriteQuoteCharacter(writer);
            }
        }

        /// <summary>
        /// Writes a quote character for names to the given <paramref name="text"/>.
        /// </summary>
        public readonly void WriteQuoteCharacterForName(Text text)
        {
            if ((flags & Flags.QuotelessNames) == 0)
            {
                WriteQuoteCharacter(text);
            }
        }

        /// <summary>
        /// Appends a space after a colon character to the given <paramref name="writer"/>.
        /// </summary>
        public readonly void SpaceAfterColon(ByteWriter writer)
        {
            if ((flags & Flags.SpaceAfterColon) != 0)
            {
                writer.WriteUTF8(' ');
            }
        }

        /// <summary>
        /// Flags describing options.
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>
            /// No flags set.
            /// </summary>
            None = 0,

            /// <summary>
            /// The <c>\r</c> should be appended to the end of a line.
            /// </summary>
            CarrierReturn = 1,

            /// <summary>
            /// The <c>\n</c> should be appended to the end of a line.
            /// </summary>
            LineFeed = 2,

            /// <summary>
            /// The name of a <see cref="JSONProperty"/> should not be encapsulated with quotes.
            /// </summary>
            QuotelessNames = 4,

            /// <summary>
            /// Quotes are appended as <c>'</c> instead of <c>"</c>.
            /// </summary>
            SingleQuotedText = 8,

            /// <summary>
            /// A space should be appended after a colon character.
            /// </summary>
            SpaceAfterColon = 16,
        }
    }
}