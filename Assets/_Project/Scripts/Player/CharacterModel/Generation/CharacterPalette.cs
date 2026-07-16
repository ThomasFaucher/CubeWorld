using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Generation
{
    /// <summary>
    /// Toutes les couleurs d'un personnage, tirées d'un <see cref="CharacterRng"/> : palette
    /// "héros" inspirée de Link, construite sur un CONTRASTE DE VALEUR assumé — trois
    /// familles clairement séparées + un accent :
    ///  - SOMBRE : cuir quasi noir (épaulières, ceinture, baudrier, bottes) ;
    ///  - CLAIR : blanc cassé (manches, pantalon, col) ;
    ///  - MOYEN identitaire : vert tunique/bonnet ;
    ///  - ACCENT isolé : or saturé (boucle, garde/pommeau d'épée) — nulle part ailleurs.
    /// C'est cette alternance sombre/clair/vif qui donne le relief "CubeWorld" ; une palette
    /// dérivée d'une seule teinte lit comme un aplat, quel que soit l'éclairage.
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : c'est ICI qu'on élargit la diversité visuelle
    /// (autres tenues à thème, palettes par archétype...) sans toucher aux générateurs de
    /// pièces, qui ne connaissent que les rôles de couleur. Garder la règle des trois
    /// familles de valeur, sinon on retombe dans le rendu plat.
    /// </summary>
    internal readonly struct CharacterPalette
    {
        public readonly Color32 Skin;
        public readonly Color32 SkinShadow;
        public readonly Color32 Hair;
        public readonly Color32 HairShadow;
        public readonly Color32 Eye;
        public readonly Color32 EyeWhite;
        public readonly Color32 Mouth;
        public readonly Color32 Blush;

        /// <summary>Vert principal : tunique, jupe de tunique et bonnet.</summary>
        public readonly Color32 OutfitPrimary;

        /// <summary>Vert sombre : taille, ombres de la tenue.</summary>
        public readonly Color32 OutfitSecondary;

        /// <summary>Blanc cassé : col, liserés.</summary>
        public readonly Color32 OutfitTrim;

        /// <summary>Doré saturé : boucle, garde/pommeau — l'UNIQUE accent vif.</summary>
        public readonly Color32 Emblem;

        /// <summary>Cuir quasi noir : ceinture, baudrier, épaulières, poignée d'épée.</summary>
        public readonly Color32 Belt;

        /// <summary>Cuir éclairci : reliefs des pièces de cuir (liseré d'épaulière...).</summary>
        public readonly Color32 LeatherLight;

        /// <summary>Blanc cassé : pantalon et manches d'avant-bras.</summary>
        public readonly Color32 Pants;

        public readonly Color32 Boot;
        public readonly Color32 BootSole;

        /// <summary>Acier clair : arête/plat de la lame.</summary>
        public readonly Color32 Blade;

        /// <summary>Acier ombré : flancs de la lame (fausse ciselure).</summary>
        public readonly Color32 BladeDark;

        private CharacterPalette(
            Color32 skin,
            Color32 skinShadow,
            Color32 hair,
            Color32 hairShadow,
            Color32 eye,
            Color32 eyeWhite,
            Color32 mouth,
            Color32 blush,
            Color32 outfitPrimary,
            Color32 outfitSecondary,
            Color32 outfitTrim,
            Color32 emblem,
            Color32 belt,
            Color32 leatherLight,
            Color32 pants,
            Color32 boot,
            Color32 bootSole,
            Color32 blade,
            Color32 bladeDark
        )
        {
            Skin = skin;
            SkinShadow = skinShadow;
            Hair = hair;
            HairShadow = hairShadow;
            Eye = eye;
            EyeWhite = eyeWhite;
            Mouth = mouth;
            Blush = blush;
            OutfitPrimary = outfitPrimary;
            OutfitSecondary = outfitSecondary;
            OutfitTrim = outfitTrim;
            Emblem = emblem;
            Belt = belt;
            LeatherLight = leatherLight;
            Pants = pants;
            Boot = boot;
            BootSole = bootSole;
            Blade = blade;
            BladeDark = bladeDark;
        }

        public static CharacterPalette Generate(CharacterRng rng)
        {
            // Peau : petit pool de teintes plausibles, avec une légère variance individuelle.
            float[] skinHues = { 0.06f, 0.07f, 0.08f, 0.09f, 0.10f };
            float skinHue = rng.Pick(skinHues);
            Color32 skin = FromHsv(
                skinHue,
                rng.NextFloat(0.26f, 0.36f),
                rng.NextFloat(0.86f, 0.96f)
            );
            Color32 skinShadow = Darken(skin, 0.82f);

            // Blond doré, du miel au blond clair selon le seed ; ombre bien plus foncée
            // (sourcils, mèches en creux) pour que le visage reste dessiné.
            Color32 hair = FromHsv(
                rng.NextFloat(0.11f, 0.14f),
                rng.NextFloat(0.5f, 0.68f),
                rng.NextFloat(0.78f, 0.92f)
            );
            Color32 hairShadow = Darken(hair, 0.55f);

            // Iris bleu FONCÉ très saturé : contraste maximal avec la sclère blanc pur.
            Color32 eye = FromHsv(
                rng.NextFloat(0.55f, 0.62f),
                rng.NextFloat(0.85f, 0.95f),
                rng.NextFloat(0.4f, 0.55f)
            );

            // Famille MOYENNE : vert tunique franc, un cran plus sombre qu'avant pour
            // trancher avec le blanc des manches.
            float greenHue = rng.NextFloat(0.28f, 0.34f);
            float greenSat = rng.NextFloat(0.6f, 0.8f);
            Color32 outfitPrimary = FromHsv(greenHue, greenSat, rng.NextFloat(0.42f, 0.52f));
            Color32 outfitSecondary = FromHsv(
                greenHue,
                greenSat + 0.05f,
                rng.NextFloat(0.2f, 0.28f)
            );

            // Famille CLAIRE : blanc cassé quasi pur, saturation minimale.
            Color32 outfitTrim = FromHsv(
                rng.NextFloat(0.10f, 0.14f),
                rng.NextFloat(0.03f, 0.08f),
                rng.NextFloat(0.9f, 0.96f)
            );
            Color32 pants = FromHsv(
                rng.NextFloat(0.09f, 0.13f),
                rng.NextFloat(0.04f, 0.08f),
                rng.NextFloat(0.9f, 0.96f)
            );

            // Famille SOMBRE : cuir brun presque noir + version éclaircie pour les reliefs.
            float leatherHue = rng.NextFloat(0.05f, 0.08f);
            Color32 belt = FromHsv(
                leatherHue,
                rng.NextFloat(0.45f, 0.6f),
                rng.NextFloat(0.1f, 0.16f)
            );
            Color32 leatherLight = FromHsv(
                leatherHue,
                rng.NextFloat(0.4f, 0.55f),
                rng.NextFloat(0.24f, 0.3f)
            );
            Color32 boot = FromHsv(
                leatherHue,
                rng.NextFloat(0.45f, 0.6f),
                rng.NextFloat(0.16f, 0.22f)
            );
            Color32 bootSole = Darken(boot, 0.55f);

            // ACCENT : or saturé, réservé aux petits éléments (boucle, garde, pommeau).
            Color32 emblem = FromHsv(
                rng.NextFloat(0.12f, 0.14f),
                rng.NextFloat(0.8f, 0.9f),
                rng.NextFloat(0.85f, 0.95f)
            );

            // Acier de lame : clair froid + flanc ombré.
            Color32 blade = FromHsv(
                0.58f,
                rng.NextFloat(0.03f, 0.08f),
                rng.NextFloat(0.86f, 0.94f)
            );
            Color32 bladeDark = Darken(blade, 0.68f);

            return new CharacterPalette(
                skin,
                skinShadow,
                hair,
                hairShadow,
                eye,
                eyeWhite: new Color32(252, 252, 254, 255),
                mouth: new Color32(165, 75, 82, 255),
                blush: new Color32(240, 152, 158, 255),
                outfitPrimary: outfitPrimary,
                outfitSecondary: outfitSecondary,
                outfitTrim: outfitTrim,
                emblem: emblem,
                belt: belt,
                leatherLight: leatherLight,
                pants: pants,
                boot: boot,
                bootSole: bootSole,
                blade: blade,
                bladeDark: bladeDark
            );
        }

        public static Color32 Darken(Color32 color, float factor) =>
            new(
                (byte)(color.r * factor),
                (byte)(color.g * factor),
                (byte)(color.b * factor),
                color.a
            );

        private static Color32 FromHsv(float h, float s, float v) => Color.HSVToRGB(h, s, v);
    }
}
