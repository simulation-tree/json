namespace JSON
{
    /// <summary>
    /// Describes a type that can serialize to and from JSON data.
    /// </summary>
    public interface IJSONSerializable
    {
        /// <summary>
        /// Reads the JSON data from the given <paramref name="reader"/> and populates the current instance.
        /// </summary>
        void Read(JSONReader reader);

        /// <summary>
        /// Writes the current instance to JSON data using the given <paramref name="writer"/>.
        /// </summary>
        void Write(ref JSONWriter writer);
    }
}