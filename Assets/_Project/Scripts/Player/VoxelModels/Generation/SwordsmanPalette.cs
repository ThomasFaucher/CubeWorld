using UnityEngine;

namespace CubeWorld.Player.VoxelModels.Generation
{
    /// <summary>
    /// Toutes les couleurs du Swordsman, tirées depuis un <see cref="CharacterRng"/> au lieu
    /// d'être figées. Tunique/cape/gemmes/liseré dérivent d'une même teinte d'accent pour que
    /// la tenue reste cohérente d'un seed à l'autre.
    /// </summary>
    internal readonly struct SwordsmanPalette
    {
        public readonly Color32 Skin;
        public readonly Color32 Hair;
        public readonly Color32 Eye;
        public readonly Color32 Armor;
        public readonly Color32 ArmorLight;
        public readonly Color32 Tunic;
        public readonly Color32 Pants;
        public readonly Color32 Belt;
        public readonly Color32 Boot;
        public readonly Color32 BootSole;
        public readonly Color32 Gem;
        public readonly Color32 Cape;
        public readonly Color32 CapeDark;
        public readonly Color32 CapeTrim;
        public readonly Color32 Pouch;
        public readonly Color32 EyeWhite;
        public readonly Color32 Mouth;
        public readonly Color32 Blush;

        private SwordsmanPalette(
            Color32 skin,
            Color32 hair,
            Color32 eye,
            Color32 armor,
            Color32 armorLight,
            Color32 tunic,
            Color32 pants,
            Color32 belt,
            Color32 boot,
            Color32 bootSole,
            Color32 gem,
            Color32 cape,
            Color32 capeDark,
            Color32 capeTrim,
            Color32 pouch,
            Color32 eyeWhite,
            Color32 mouth,
            Color32 blush
        )
        {
            Skin = skin;
            Hair = hair;
            Eye = eye;
            Armor = armor;
            ArmorLight = armorLight;
            Tunic = tunic;
            Pants = pants;
            Belt = belt;
            Boot = boot;
            BootSole = bootSole;
            Gem = gem;
            Cape = cape;
            CapeDark = capeDark;
            CapeTrim = capeTrim;
            Pouch = pouch;
            EyeWhite = eyeWhite;
            Mouth = mouth;
            Blush = blush;
        }

        public static SwordsmanPalette Generate(CharacterRng rng)
        {
            // Peau : petit pool de teintes plausibles, avec une légère variance individuelle.
            float[] skinHues = { 0.06f, 0.07f, 0.08f, 0.09f, 0.10f };
            float skinHue = rng.Pick(skinHues);
            Color32 skin = FromHsv(skinHue, rng.NextFloat(0.28f, 0.38f), rng.NextFloat(0.82f, 0.94f));

            float hairHue = rng.NextFloat(0f, 1f);
            Color32 hair = FromHsv(hairHue, rng.NextFloat(0.45f, 0.85f), rng.NextFloat(0.35f, 0.75f));

            float eyeHue = rng.NextFloat(0f, 1f);
            Color32 eye = FromHsv(eyeHue, rng.NextFloat(0.55f, 0.85f), rng.NextFloat(0.55f, 0.85f));

            float armorHue = rng.NextFloat(0f, 1f);
            float armorSat = rng.NextFloat(0.03f, 0.10f);
            Color32 armor = FromHsv(armorHue, armorSat, rng.NextFloat(0.38f, 0.46f));
            Color32 armorLight = FromHsv(armorHue, armorSat, rng.NextFloat(0.54f, 0.62f));

            // Teinte d'accent unique : pilote tunique, cape, gemme et liseré ensemble.
            float accentHue = rng.NextFloat(0f, 1f);
            float accentSat = rng.NextFloat(0.4f, 0.65f);
            Color32 tunic = FromHsv(accentHue, accentSat, rng.NextFloat(0.30f, 0.42f));
            Color32 cape = FromHsv(accentHue, accentSat, rng.NextFloat(0.24f, 0.34f));
            Color32 capeDark = FromHsv(accentHue, accentSat + 0.05f, rng.NextFloat(0.14f, 0.22f));
            Color32 gem = FromHsv((accentHue + 0.5f) % 1f, rng.NextFloat(0.55f, 0.75f), rng.NextFloat(0.55f, 0.7f));
            Color32 capeTrim = FromHsv(rng.NextFloat(0.10f, 0.16f), rng.NextFloat(0.45f, 0.6f), rng.NextFloat(0.65f, 0.8f));

            Color32 pants = FromHsv(armorHue, rng.NextFloat(0.05f, 0.12f), rng.NextFloat(0.18f, 0.26f));
            Color32 belt = FromHsv(0.10f, rng.NextFloat(0.25f, 0.35f), rng.NextFloat(0.72f, 0.84f));
            Color32 boot = FromHsv(0.08f, rng.NextFloat(0.2f, 0.3f), rng.NextFloat(0.12f, 0.18f));
            Color32 bootSole = FromHsv(0.08f, rng.NextFloat(0.1f, 0.2f), rng.NextFloat(0.06f, 0.1f));
            Color32 pouch = FromHsv(0.08f, rng.NextFloat(0.4f, 0.55f), rng.NextFloat(0.35f, 0.48f));

            return new SwordsmanPalette(
                skin,
                hair,
                eye,
                armor,
                armorLight,
                tunic,
                pants,
                belt,
                boot,
                bootSole,
                gem,
                cape,
                capeDark,
                capeTrim,
                pouch,
                eyeWhite: new Color32(250, 250, 252, 255),
                mouth: new Color32(180, 90, 95, 255),
                blush: new Color32(235, 150, 155, 255)
            );
        }

        private static Color32 FromHsv(float h, float s, float v) => Color.HSVToRGB(h, s, v);
    }
}
