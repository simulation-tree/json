using Collections.Generic;
using JSON;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unmanaged;
using Unmanaged.Tests;

namespace Serialization.Tests
{
    public class JSONTests : UnmanagedTests
    {
        [Test]
        public void ParseTokens()
        {
            JsonObject json = new()
            {
                { "name", "John Programming" },
                { "age", 42 },
                { "isAlive", true },
                {
                    "address", new JsonObject
                    {
                        { "streetAddress", "21 2nd Street" },
                        { "city", "New York" },
                        { "state", "NY" },
                        { "postalCode", "10021-3100" }
                    }
                }
            };

            string jsonString = json.ToString();
            using ByteReader reader = ByteReader.CreateFromUTF8(jsonString);
            JSONReader jsonReader = new(reader);
            using List<Token> tokens = new();
            while (jsonReader.TryReadToken(out Token token))
            {
                tokens.Add(token);
                Console.WriteLine($"{token.type} = {token.GetText(jsonReader)}");
            }

            Assert.That(tokens.Count, Is.EqualTo(19));
            Assert.That(tokens[0].type == Token.Type.StartObject);
            Assert.That(tokens[1].type == Token.Type.Value);     //name
            Assert.That(tokens[2].type == Token.Type.Value);     //John Doe
            Assert.That(tokens[3].type == Token.Type.Value);     //age
            Assert.That(tokens[4].type == Token.Type.Value);     //42
            Assert.That(tokens[5].type == Token.Type.Value);     //isAlive
            Assert.That(tokens[6].type == Token.Type.Value);     //true
            Assert.That(tokens[7].type == Token.Type.Value);     //address
            Assert.That(tokens[8].type == Token.Type.StartObject);
            Assert.That(tokens[9].type == Token.Type.Value);     //streetAddress
            Assert.That(tokens[10].type == Token.Type.Value);    //21 2nd Street
            Assert.That(tokens[11].type == Token.Type.Value);    //city
            Assert.That(tokens[12].type == Token.Type.Value);    //New York
            Assert.That(tokens[13].type == Token.Type.Value);    //state
            Assert.That(tokens[14].type == Token.Type.Value);    //NY
            Assert.That(tokens[15].type == Token.Type.Value);    //postalCode
            Assert.That(tokens[16].type == Token.Type.Value);    //10021-3100
            Assert.That(tokens[17].type == Token.Type.EndObject);
            Assert.That(tokens[18].type == Token.Type.EndObject);
        }

        [Test]
        public void ParseJSON5Tokens()
        {
            string source = @"{
                name: 'John Programming',
                age: 42,
                isAlive: true,
                address: {
                    streetAddress: '21 2nd Street',
                    city: 'New York',
                    state: 'NY',
                    postalCode: '10021-3100'
                }
            }";

            using ByteReader reader = ByteReader.CreateFromUTF8(source);
            JSONReader jsonReader = new(reader);
            using List<Token> tokens = new();
            while (jsonReader.TryReadToken(out Token token))
            {
                tokens.Add(token);
                Console.WriteLine($"{token.type} = {token.GetText(jsonReader)}");
            }

            Assert.That(tokens.Count, Is.EqualTo(19));
            Assert.That(tokens[0].type == Token.Type.StartObject);
            Assert.That(tokens[1].type == Token.Type.Value);     //name
            Assert.That(tokens[2].type == Token.Type.Value);     //John Doe
            Assert.That(tokens[3].type == Token.Type.Value);     //age
            Assert.That(tokens[4].type == Token.Type.Value);     //42
            Assert.That(tokens[5].type == Token.Type.Value);     //isAlive
            Assert.That(tokens[6].type == Token.Type.Value);     //true
            Assert.That(tokens[7].type == Token.Type.Value);     //address
            Assert.That(tokens[8].type == Token.Type.StartObject);
            Assert.That(tokens[9].type == Token.Type.Value);     //streetAddress
            Assert.That(tokens[10].type == Token.Type.Value);    //21 2nd Street
            Assert.That(tokens[11].type == Token.Type.Value);    //city
            Assert.That(tokens[12].type == Token.Type.Value);    //New York
            Assert.That(tokens[13].type == Token.Type.Value);    //state
            Assert.That(tokens[14].type == Token.Type.Value);    //NY
            Assert.That(tokens[15].type == Token.Type.Value);    //postalCode
            Assert.That(tokens[16].type == Token.Type.Value);    //10021-3100
            Assert.That(tokens[17].type == Token.Type.EndObject);
            Assert.That(tokens[18].type == Token.Type.EndObject);
        }

        [Test]
        public void ReadSampleJSON()
        {
            JsonObject json = new()
            {
                { "name", "John Doe" },
                { "age", 42 },
                { "isAlive", true },
                {
                    "address", new JsonObject
                    {
                        { "streetAddress", "21 2nd Street" },
                        { "city", "New York" },
                        { "state", "NY" },
                        { "postalCode", "10021-3100" }
                    }
                }
            };

            string jsonString = json.ToString();
            using ByteReader reader = ByteReader.CreateFromUTF8(jsonString);
            JSONObject obj = reader.ReadObject<JSONObject>();
            Assert.That(obj.GetText("name").ToString(), Is.EqualTo("John Doe"));
            Assert.That(obj.GetNumber("age"), Is.EqualTo(42));
            Assert.That(obj.GetBoolean("isAlive"), Is.True);
            Assert.That(obj.Contains("address"), Is.True);

            JSONObject address = obj.GetObject("address");
            Assert.That(address.GetText("streetAddress").ToString(), Is.EqualTo("21 2nd Street"));
            Assert.That(address.GetText("city").ToString(), Is.EqualTo("New York"));
            Assert.That(address.GetText("state").ToString(), Is.EqualTo("NY"));
            Assert.That(address.GetText("postalCode").ToString(), Is.EqualTo("10021-3100"));
            obj.Dispose();
        }

        [Test]
        public void CreateJSON()
        {
            DateTime now = DateTime.Now;
            double seconds = (now - new DateTime(1970, 1, 1)).TotalSeconds;

            JSONObject obj = new();
            obj.Add("name", "John Doe");
            obj.Add("age", 42);
            obj.Add("isAlive", true);

            JSONObject address = new();
            address.Add("streetAddress", "21 2nd Street");
            address.Add("city", "New York");
            address.Add("state", "NY");
            address.Add("seconds", seconds);

            JSONArray inventory = new();
            inventory.Add("apples");
            inventory.Add("oranges");
            inventory.Add("pears");

            obj.Add("address", address);
            obj.Add("inventory", inventory);

            string jsonString = obj.ToString();
            JsonNode json = JsonNode.Parse(jsonString) ?? throw new Exception();
            Assert.That(json["name"]!.GetValue<string>(), Is.EqualTo("John Doe"));
            Assert.That(json["age"]!.GetValue<int>(), Is.EqualTo(42));
            Assert.That(json["isAlive"]!.GetValue<bool>(), Is.True);

            Assert.That(json["address"], Is.Not.Null);
            Assert.That(json["address"]!["streetAddress"]!.GetValue<string>(), Is.EqualTo("21 2nd Street"));
            Assert.That(json["address"]!["city"]!.GetValue<string>(), Is.EqualTo("New York"));
            Assert.That(json["address"]!["state"]!.GetValue<string>(), Is.EqualTo("NY"));
            Assert.That(json["address"]!["seconds"]!.GetValue<double>(), Is.EqualTo(seconds));

            Assert.That(json["inventory"], Is.Not.Null);
            Assert.That(json["inventory"]![0]!.GetValue<string>(), Is.EqualTo("apples"));
            Assert.That(json["inventory"]![1]!.GetValue<string>(), Is.EqualTo("oranges"));
            Assert.That(json["inventory"]![2]!.GetValue<string>(), Is.EqualTo("pears"));
            obj.Dispose();
        }

        [Test]
        public void ExampleUsage()
        {
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

            using Text buffer = new();
            jsonObject.ToString(buffer, SerializationSettings.PrettyPrinted);
            Console.WriteLine(buffer.ToString());
        }

        [Test]
        public void ListOfSettings()
        {
            JsonObject settings = new();
            settings.Add("name", "John Doe");
            settings.Add("age", 42);
            settings.Add("isAlive", true);
            settings.Add("another", "aA");

            System.Collections.Generic.List<(string name, object? value)> settingsList = new();
            using ByteReader reader = ByteReader.CreateFromUTF8(settings.ToString());
            JSONReader jsonReader = new(reader);
            jsonReader.ReadToken(); //{
            Span<char> buffer = stackalloc char[32];
            while (jsonReader.TryReadToken(out Token token))
            {
                if (token.type == Token.Type.Value)
                {
                    int length = jsonReader.GetText(token, buffer);
                    string name = buffer.Slice(0, length).ToString();
                    Token next = jsonReader.ReadToken();
                    if (next.type == Token.Type.Value)
                    {
                        length = jsonReader.GetText(next, buffer);
                        string value = buffer.Slice(0, length).ToString();
                        if (double.TryParse(value, out double number))
                        {
                            settingsList.Add((name, number));
                        }
                        else if (bool.TryParse(value, out bool boolean))
                        {
                            settingsList.Add((name, boolean));
                        }
                        else
                        {
                            settingsList.Add((name, value));
                        }
                    }
                    else
                    {
                        throw new Exception($"Expected text token, but got {next.type}");
                    }
                }
                else
                {
                    //}
                    break;
                }
            }

            Assert.That(settingsList.Count, Is.EqualTo(4));
            Assert.That(settingsList[0].name, Is.EqualTo("name"));
            Assert.That(settingsList[0].value, Is.EqualTo("John Doe"));
            Assert.That(settingsList[1].name, Is.EqualTo("age"));
            Assert.That(settingsList[1].value, Is.EqualTo(42));
            Assert.That(settingsList[2].name, Is.EqualTo("isAlive"));
            Assert.That(settingsList[2].value, Is.True);
            Assert.That(settingsList[3].name, Is.EqualTo("another"));
            Assert.That(settingsList[3].value, Is.EqualTo("aA"));
        }

        [Test]
        public void ReadJSONWithArray()
        {
            JsonObject json = new();
            JsonArray inventory = new();
            const int ItemCount = 32;
            for (uint i = 0; i < ItemCount; i++)
            {
                JsonObject item = new();
                item.Add("name", $"Item {i}");
                item.Add("value", Guid.NewGuid().ToString());
                item.Add("quantity", i * (Guid.NewGuid().GetHashCode() % 7));
                item.Add("isRare", i % 2 == 0);
                inventory.Add(item);
            }

            json.Add("inventory", inventory);
            string jsonString = json.ToJsonString(new JsonSerializerOptions() { WriteIndented = false });
            using ByteReader reader = ByteReader.CreateFromUTF8(jsonString);
            JSONObject obj = reader.ReadObject<JSONObject>();
            JSONArray array = obj.GetArray("inventory");
            Assert.That(array.Count, Is.EqualTo(ItemCount));
            string otherString = obj.ToString();
            Assert.That(jsonString, Is.EqualTo(otherString));
            obj.Dispose();
        }

        [Test]
        public void DeserializeIntoStruct()
        {
            Guid g = Guid.NewGuid();
            bool rare = g.GetHashCode() % 2 == 0;
            using JSONObject item = new();
            item.Add("name", "Item 25");
            item.Add("value", g.ToString());
            item.Add("quantity", g.GetHashCode() % 7);
            item.Add("isRare", rare);

            string str = item.ToString();
            using ByteReader reader = ByteReader.CreateFromUTF8(str);
            JSONReader jsonReader = new(reader);
            using DummyJSONObject dummy = jsonReader.ReadObject<DummyJSONObject>();
            Assert.That(dummy.Name.ToString(), Is.EqualTo("Item 25"));
            Assert.That(dummy.Value.ToString(), Is.EqualTo(g.ToString()));
            Assert.That(dummy.quantity, Is.EqualTo(g.GetHashCode() % 7));
            Assert.That(dummy.isRare, Is.EqualTo(rare));
        }

        [Test]
        public void SerializeFromStruct()
        {
            using DummyJSONObject dummy = new("abacus", "212-4", 32, false);
            using JSONWriter writer = new();
            writer.WriteObject(dummy);
            string jsonSource = writer.ToString();
            Assert.That(jsonSource, Is.EqualTo("{\"name\":\"abacus\",\"value\":\"212-4\",\"quantity\":32,\"isRare\":false}"));
        }

        [Test]
        public void WriteArrayToJSON5()
        {
            using Player player = new("playerName", 100, "red");
            player.AddItem("abacus", "212-4", 32, false);
            player.AddItem("itemId", "forgot what this is", 1, true);

            SerializationSettings settings = SerializationSettings.JSON5PrettyPrinted;
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                settings.flags &= ~SerializationSettings.Flags.CarrierReturn;
            }

            using JSONWriter jsonWriter = new(settings);
            jsonWriter.WriteObject(player);
            string jsonSource = jsonWriter.ToString();

            using ByteReader reader = ByteReader.CreateFromUTF8(jsonSource);
            JSONReader jsonReader = new(reader);
            using Player readPlayer = jsonReader.ReadObject<Player>();
            Assert.That(readPlayer.Name.SequenceEqual(player.Name), Is.True);
            Assert.That(readPlayer.HP, Is.EqualTo(player.HP));
            Assert.That(readPlayer.Items.Length, Is.EqualTo(player.Items.Length));
            Assert.That(readPlayer.Color, Is.EqualTo(player.Color));

            string expectedSource = GetResourceText("Sample1.json5").ToString();
            Assert.That(jsonSource, Is.EqualTo(expectedSource));
        }

        [Test]
        public void TryReadXMLAsJSON()
        {
            string source = "<Project Sdk=\"Microshaft.Sdk.blabla\"><Some>5</Some></Project>";
            if (JSONObject.TryParse(source, out JSONObject jsonObject))
            {
                Assert.Fail("Expected failure, but parsed successfully");
            }
        }

        [Test]
        public void DeserializeArray()
        {
            using JSONObject inventory = new();
            JSONArray array = new();
            for (uint i = 0; i < 32; i++)
            {
                array.Add(i);
            }

            inventory.Add("inventory", array);
            using ByteReader reader = ByteReader.CreateFromUTF8(inventory.ToString());
            using JSONObject obj = reader.ReadObject<JSONObject>();
            JSONArray items = obj.GetArray("inventory");
            Assert.That(items.Count, Is.EqualTo(32));
            for (int i = 0; i < items.Count; i++)
            {
                Assert.That(items[i].Number, Is.EqualTo(i));
            }
        }

        [Test]
        public void ReadMaterialJSON()
        {
            string source = GetResourceText("Sample2.json").ToString();
            using JSONObject jsonObject = JSONObject.Parse(source);
            Assert.That(jsonObject.Contains("vertex"), Is.True);
            Assert.That(jsonObject.Contains("fragment"), Is.True);

            using ByteReader byteReader = ByteReader.CreateFromUTF8(source);
            using JSONObject jsonObject2 = byteReader.ReadObject<JSONObject>();
            bool hasVertexProperty = jsonObject2.TryGetText("vertex", out ReadOnlySpan<char> vertexText);
            bool hasFragmentProperty = jsonObject2.TryGetText("fragment", out ReadOnlySpan<char> fragmentText);
            Assert.That(hasVertexProperty, Is.True);
            Assert.That(hasFragmentProperty, Is.True);
            Assert.That(vertexText.ToString(), Is.EqualTo("Assets/Shaders/Text.vertex.glsl"));
            Assert.That(fragmentText.ToString(), Is.EqualTo("Assets/Shaders/Text.fragment.glsl"));
        }

        [Test]
        public void ConvertJSONToStruct()
        {
            JsonObject json = new();
            JsonArray inventory = new();
            System.Collections.Generic.List<DummyJSONObject> originals = new();
            for (int i = 0; i < 32; i++)
            {
                Guid g = Guid.NewGuid();
                DummyJSONObject dummy = new($"Item {i}", g.ToString(), i * (g.GetHashCode() % 7), i % 2 == 0);

                JsonObject item = new();
                item.Add("name", dummy.Name.ToString());
                item.Add("value", dummy.Value.ToString());
                item.Add("quantity", dummy.quantity);
                item.Add("isRare", dummy.isRare);
                inventory.Add(item);
                originals.Add(dummy);
            }

            json.Add("inventory", inventory);

            using ByteReader reader = ByteReader.CreateFromUTF8(json.ToString());
            using JSONObject jsonObject = reader.ReadObject<JSONObject>();
            JSONArray array = jsonObject.GetArray("inventory");
            Assert.That(array.Count, Is.EqualTo(32));
            for (int i = 0; i < array.Count; i++)
            {
                JSONProperty item = array[i];
                JSONObject itemObj = item.Object;
                using DummyJSONObject dummy = itemObj.As<DummyJSONObject>();
                using DummyJSONObject original = originals[(int)i];
                Assert.That(dummy.Name.ToString(), Is.EqualTo(original.Name.ToString()));
                Assert.That(dummy.Value.ToString(), Is.EqualTo(original.Value.ToString()));
                Assert.That(dummy.quantity, Is.EqualTo(original.quantity));
                Assert.That(dummy.isRare, Is.EqualTo(original.isRare));
            }
        }

        public struct DummyJSONObject : IJSONSerializable, IDisposable
        {
            public int quantity;
            public bool isRare;

            private Text name;
            private Text value;

            public readonly ReadOnlySpan<char> Name => name.AsSpan();
            public readonly ReadOnlySpan<char> Value => value.AsSpan();

            public DummyJSONObject(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int quantity, bool isRare)
            {
                this.name = new(name);
                this.value = new(value);
                this.quantity = quantity;
                this.isRare = isRare;
            }

            public DummyJSONObject(string name, string value, int quantity, bool isRare)
            {
                this.name = new(name);
                this.value = new(value);
                this.quantity = quantity;
                this.isRare = isRare;
            }

            public void Dispose()
            {
                name.Dispose();
                value.Dispose();
            }

            void IJSONSerializable.Read(JSONReader reader)
            {
                //for all properties, skip reading the name, and read the value directly (assumes layout is perfect)
                Span<char> buffer = stackalloc char[64];
                reader.ReadToken();
                int length = reader.ReadText(buffer);
                name = new(buffer.Slice(0, length));
                reader.ReadToken();
                length = reader.ReadText(buffer);
                value = new(buffer.Slice(0, length));
                reader.ReadToken();
                quantity = (int)reader.ReadNumber();
                reader.ReadToken();
                isRare = reader.ReadBoolean();
            }

            readonly void IJSONSerializable.Write(ref JSONWriter writer)
            {
                writer.WriteProperty(nameof(name), name.AsSpan());
                writer.WriteProperty(nameof(value), value.AsSpan());
                writer.WriteProperty(nameof(quantity), quantity);
                writer.WriteProperty(nameof(isRare), isRare);
            }
        }

        public struct Player : IJSONSerializable, IDisposable
        {
            private Text name;
            private int hp;
            private List<DummyJSONObject> items;
            private ASCIIText16 htmlColor;

            public readonly ReadOnlySpan<char> Name => name.AsSpan();
            public readonly int HP => hp;
            public readonly ReadOnlySpan<DummyJSONObject> Items => items.AsSpan();
            public readonly ASCIIText16 Color => htmlColor;

            public Player(ReadOnlySpan<char> name, int hp, ReadOnlySpan<char> htmlColor)
            {
                this.name = new(name);
                this.hp = hp;
                this.items = new();
                this.htmlColor = new(htmlColor);
            }

            public void Dispose()
            {
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].Dispose();
                }

                items.Dispose();
                name.Dispose();
            }

            public readonly void AddItem(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int quantity, bool isRare)
            {
                items.Add(new(name, value, quantity, isRare));
            }

            void IJSONSerializable.Read(JSONReader reader)
            {
                reader.ReadToken(); //name
                Span<char> buffer = stackalloc char[64];
                int nameLength = reader.ReadText(buffer);
                name = new(buffer.Slice(0, nameLength));

                reader.ReadToken(); //name
                hp = (int)reader.ReadNumber();

                items = new();
                reader.ReadToken(); //name
                reader.ReadToken(); //[
                while (reader.TryPeekToken(out Token token))
                {
                    if (token.type == Token.Type.EndArray)
                    {
                        break;
                    }

                    DummyJSONObject item = reader.ReadObject<DummyJSONObject>();
                    items.Add(item);
                }

                reader.ReadToken(); //]

                reader.ReadToken(); //name
                int colorLength = reader.ReadText(buffer);
                htmlColor = new(buffer.Slice(0, colorLength));
            }

            readonly void IJSONSerializable.Write(ref JSONWriter writer)
            {
                writer.WriteProperty(nameof(name), name.AsSpan());
                writer.WriteProperty(nameof(hp), hp);
                writer.WriteArray<DummyJSONObject>(nameof(items), items.AsSpan());
                writer.WriteProperty(nameof(htmlColor), htmlColor.ToString());
            }
        }
    }
}