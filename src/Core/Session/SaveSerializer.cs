using System.IO;
using MirrorChronicles.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// Saves as indented JSON with enums written by name, so renumbering an enum never corrupts a save.
    /// Reading and writing files is the Godot layer's job (user://).
    /// </summary>
    public static class SaveSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace, // saved lists replace the defaults
            Converters = { new StringEnumConverter() }
        };

        public static string Serialize(GameData data) => JsonConvert.SerializeObject(data, Settings);

        /// <exception cref="InvalidDataException">The text is not a readable save.</exception>
        public static GameData Deserialize(string json)
        {
            GameData data;
            try
            {
                data = JsonConvert.DeserializeObject<GameData>(json, Settings);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"The save is unreadable: {e.Message}", e);
            }

            return data ?? throw new InvalidDataException("The save is empty.");
        }
    }
}
