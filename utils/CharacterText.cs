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
        public List<JournalEntry> JournalEntries { get; set; }
        public Dictionary<string, float> Attributes { get; set; }
        public Dictionary<string, float> AbilityStart { get; set; }
        public Dictionary<string, float> AbilityMax { get; set; }
        public Dictionary<string, bool> KnownSkills { get; set; }
        public Dictionary<string, bool> PreferredPlaces { get; set; }
        public Dictionary<string, bool> DislikedPlaces { get; set; }
        public int? HpMin { get; set; }
        public int? HpWithGnos { get; set; }
        public Dictionary<string, List<Line>> Dialogue { get; set; }
    }

    public class JournalEntry
    {
        public string Text { get; set; }
        public int Type { get; set; }
    }

    public class Line
    {
        public string Text { get; set; }
        public int? Face { get; set; } // Nullable int to handle cases where face is not present
    }
}
