using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace GnosiaCustomizer.utils
{
    public class CharacterText
    {
        public string Name { get; set; } = default;
        public string Origin { get; set; } = default;
        public string Bio1 { get; set; } = default;
        public string Bio2 { get; set; } = default;
        public string Honorific { get; set; } = default;
        public byte? Sex { get; set; } = 2; // 0 = Male, 1 = Female, 2 = Non-Binary
        public uint? Age { get; set; } = 25;
        public Dictionary<string, float> Attributes { get; set; } = default;
        public Dictionary<string, float> AbilityStart { get; set; } = default;
        public Dictionary<string, float> AbilityMax { get; set; } = default;
        public HashSet<string> KnownSkills { get; set; } = default;
        public HashSet<string> PreferredPlaces { get; set; } = default;
        public HashSet<string> DislikedPlaces { get; set; } = default;
        public int? DefenseMin { get; set; } = 100;
        public int? DefenseWithGnos { get; set; } = 150;
        public Dictionary<string, LocalizedText> SingleLines { get; private set; } = [];
        public Dictionary<string, MultilineLocalizedText> MultiLines { get; private set; } = [];


        public void LoadFromFile(string path)
        {
            var yaml = File.ReadAllText(path);
            var input = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(input);

            var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (var entry in root.Children)
            {
                var key = entry.Key.ToString();
                var value = entry.Value;

                switch (key)
                {
                    case "name": 
                        Name = value.ToString(); 
                        break;
                    case "origin": 
                        Origin = value.ToString(); 
                        break;
                    case "bio1": 
                        Bio1 = value.ToString();
                        break;
                    case "bio2": 
                        Bio2 = value.ToString(); 
                        break;
                    case "honorific": Honorific = value.ToString();
                        break;
                    case "sex":
                        if (byte.TryParse(value.ToString(), out var sex)) 
                        { 
                            Sex = sex;
                        }
                        break;
                    case "age":
                        if (uint.TryParse(value.ToString(), out var age))
                        {
                            Age = age;
                        }
                        break;
                    case "attributes":
                        Attributes = ParseFloatMap(value);
                        break;
                    case "ability_start":
                        AbilityStart = ParseFloatMap(value);
                        break;
                    case "ability_max":
                        AbilityMax = ParseFloatMap(value);
                        break;
                    case "known_skills":
                        KnownSkills = ParseBoolMap(value);
                        break;
                    case "preferred_places":
                        PreferredPlaces = ParseBoolMap(value);
                        break;
                    case "disliked_places":
                        DislikedPlaces = ParseBoolMap(value);
                        break;
                    case "defense_min":
                        if (int.TryParse(value.ToString(), out var defMin))
                        {
                            DefenseMin = defMin;
                        }
                        break;
                    case "defense_with_gnos":
                        if (int.TryParse(value.ToString(), out var defWith)) {
                            DefenseWithGnos = defWith;
                        }
                        break;

                    default:
                        if (value is YamlMappingNode mapNode)
                        {
                            if (mapNode.Children.ContainsKey("line"))
                            {
                                var singleText = new LocalizedText();

                                if (mapNode.Children.TryGetValue("line", out var lineNode))
                                    singleText.Line = lineNode.ToString();

                                if (mapNode.Children.TryGetValue("sprite", out var spriteNode) && int.TryParse(spriteNode.ToString(), out var sprite))
                                    singleText.Sprite = sprite;

                                SingleLines[key] = singleText;
                            }
                            else if (mapNode.Children.ContainsKey("lines"))
                            {
                                var multiText = new MultilineLocalizedText();

                                if (mapNode.Children.TryGetValue("lines", out var linesNode) && linesNode is YamlSequenceNode sequence)
                                {
                                    foreach (var item in sequence)
                                    {
                                        if (item is YamlMappingNode lineMap)
                                        {
                                            var line = new LocalizedText();
                                            if (lineMap.Children.TryGetValue("line", out var lineStr))
                                            {
                                                line.Line = lineStr.ToString();
                                            }

                                            if (lineMap.Children.TryGetValue("sprite", out var spriteVal) && int.TryParse(spriteVal.ToString(), out var spr))
                                            {
                                                line.Sprite = spr;
                                            }

                                            multiText.Lines.Add(line);
                                        }
                                    }
                                }

                                MultiLines[key] = multiText;

                            }
                        }
                        break;
                }
            }
        }

        private static Dictionary<string, float> ParseFloatMap(YamlNode node)
        {
            var result = new Dictionary<string, float>();
            if (node is YamlMappingNode map)
            {
                foreach (var kv in map.Children)
                {
                    if (float.TryParse(kv.Value.ToString(), out var val))
                    {
                        result[kv.Key.ToString()] = val;
                    }
                }
            }
            return result;
        }

        private static HashSet<string> ParseBoolMap(YamlNode node)
        {
            var result = new HashSet<string>();
            if (node is YamlMappingNode map)
            {
                foreach (var kv in map.Children)
                {
                    if (bool.TryParse(kv.Value.ToString(), out var val) && val)
                    {
                        result.Add(kv.Key.ToString());
                    }
                }
            }
            return result;
        }
    }

    public class LocalizedText
    {
        public string Line { get; set; } = default;
        public int Sprite { get; set; } = 0;
        public List<OverrideData> Overrides { get; set; } = default;
    }

    public class MultilineLocalizedText
    {
        public List<LocalizedText> Lines { get; set; } = new List<LocalizedText>();
    }

    public class OverrideData
    {
        [YamlMember(Alias = "0")]
        public string P0 { get; set; } = default;
        [YamlMember(Alias = "1")]
        public string P1 { get; set; } = default;
        [YamlMember(Alias = "2")]
        public string P2 { get; set; } = default;
        [YamlMember(Alias = "3")]
        public string P3 { get; set; } = default;
        [YamlMember(Alias = "4")]
        public string P4 { get; set; } = default;
        [YamlMember(Alias = "5")]
        public string P5 { get; set; } = default;
        public string Line { get; set; } = default;
    }
}
