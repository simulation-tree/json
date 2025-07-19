# JSON

[![Test](https://github.com/simulation-tree/json/actions/workflows/test.yml/badge.svg)](https://github.com/simulation-tree/json/actions/workflows/test.yml)

JSON serialization library, supporting JSON5.

### Reader and writer

The reader and writers are low-level concepts used to traverse and write data:
```cs
using JSONWriter writer = new();
writer.WriteStartObject();
writer.WriteName("name");
writer.WriteText("John Doe");
writer.WriteEndObject();

using JSONReader reader = new(writer.GetBytes());
reader.ReadStartObject();
reader.ReadToken();
Span<char> nameBuffer = stackalloc char[32];
int nameLength = reader.ReadText(nameBuffer);
reader.ReadEndObject();
Assert.That(nameBuffer[..nameLength].ToString(), Is.EqualTo("John Doe"));
```

### Generic object

This is the high-level type that represents a JSON object without the need
for interacting with the reader/writer types:
```cs
JSONObject fruit = new();
fruit.Add("name", "cherry");
fruit.Add("color", "red");

JSONArray inventory = new();
inventory.Add("apples");
inventory.Add("oranges");
inventory.Add(fruit);

using JSONObject jsonObject = new();
jsonObject.Add("name", "John Doe");
jsonObject.Add("age", 42);
jsonObject.Add("alive", true);
jsonObject.Add("inventory", inventory);

jsonObject["age"].Number++;

using Text jsonText = new();
SerializationSettings settings = SerializationSettings.PrettyPrint;
jsonObject.ToString(jsonText, settings);
Console.WriteLine(jsonText);
```

Output:
```json
{
    "name": "John Doe",
    "age": 43,
    "alive": true,
    "inventory": [
        "apples",
        "oranges",
        {
            "name": "cherry",
            "color": "red"
        }
    ]
}
```

### JSON5 support

The reading mechanism supports both old and new JSON formats. But for the writer,
some settings need to be adjusted:
```cs
SerializationSettings settings = new();
settings.flags |= SerializationFlags.QuotelessNames;
settings.flags |= SerializationFlags.SingleQuotedText;
```

The shorthand for these settings is `SerializationSettings.JSON5` and `SerializationSettings.JSON5PrettyPrint`.

### Serializating C# objects

The readers and writers have API for serializing/deserializing C# objects that
implement the `IJSONObject` interface. They require the `Read` and `Write` methods
to be implemented:
```cs
public struct Player : IJSONObject, IDisposable
{
    public int hp;
    public bool alive;

    private Text name;

    public readonly Span<char> Name
    {
        get => name.AsSpan();
        set => name.CopyFrom(value);
    }

    public Player(int hp, bool alive, ReadOnlySpan<char> name)
    {
        this.hp = hp;
        this.alive = alive;
        this.name = new(name);
    }

    public void Dispose()
    {
        name.Dispose();
    }

    void IJSONObject.Read(ref JSONReader reader)
    {
        //read hp
        reader.ReadToken();
        hp = (int)reader.ReadNumber(out _);

        //read alive
        reader.ReadToken();
        alive = reader.ReadBoolean(out _);

        //read name
        reader.ReadToken();
        Span<char> nameBuffer = stackalloc char[32];
        int nameLength = reader.ReadText(nameBuffer);
        name = new(nameBuffer.Slice(0, nameLength));
    }

    void IJSONObject.Write(JSONWriter writer)
    {
        writer.WriteProperty(nameof(hp), hp);
        writer.WriteProperty(nameof(alive), alive);
        writer.WriteProperty(nameof(name), name.AsSpan());
    }
}

byte[] jsonBytes = File.ReadAllBytes("player.json");
using ByteReader reader = new(jsonBytes);
JSONReader jsonReader = new(reader);
using Player player = jsonReader.ReadObject<Player>();
ReadOnlySpan<char> name = player.Name;
```
