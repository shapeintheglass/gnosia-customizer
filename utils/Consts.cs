using UnityEngine;

namespace GnosiaCustomizer.utils
{
    internal class Consts
    {
        public const string AssetsFolder = "gnosia_customizer";
        public const string TextureAssetsFolder = "other_textures";
        public const string AudioAssetsFolder = "audio";

        public const int NumCharacters = 14;
        public const int NumHeads = 8;

        public static readonly string[] CharaFolderNames = ["p01", "p02", "p03", "p04", "p05", "p06", "p07", "p08", "p09", "p10", "p11", "p12", "p13", "p14"];
        public static readonly int[] CharaFolderIds = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14];
        public static readonly uint[] CharSpriteIndeces = [100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 1300, 1400];

        public static readonly string[] HeadFileNamesWithExt = ["h00.png", "h01.png", "h02.png", "h03.png", "h04.png", "h05.png", "h06.png", "h07.png"];
        public static readonly string[] HeadNames = ["body", "h01", "h02", "h03", "h04", "h05", "h06", "h07"];

        public static readonly Vector2 ZeroOne = new Vector2(0.0f, 1.0f);
        public const uint Order = 10U;
    }
}
