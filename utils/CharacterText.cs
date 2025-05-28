using System.Collections.Generic;

namespace GnosiaCustomizer.utils
{
    public class CharacterText
    {
        public string Name { get; set; }
        public byte? Sex { get; set; }
        public uint? Age { get; set; }
        public string Place { get; set; }
        public byte? NumJournalEntries { get; set; }
        public List<Dictionary<string, string>> JournalEntries { get; set; }
        public Dictionary<string, float> Attributes { get; set; }
        public Dictionary<string, float> AbilityStart { get; set; }
        public Dictionary<string, float> AbilityMax { get; set; }
        public Dictionary<string, bool> KnownSkills { get; set; }
        public Dictionary<string, bool> PreferredPlaces { get; set; }
        public Dictionary<string, bool> DislikedPlaces { get; set; }
        public int? HpMin { get; set; }
        public int? HpWithGnos { get; set; }
        public Dictionary<string, Dictionary<string, string>> Dialogue { get; set; }
    }
}
