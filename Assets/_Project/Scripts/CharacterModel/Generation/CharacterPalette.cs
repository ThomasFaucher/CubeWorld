using UnityEngine;

namespace CubeWorld.CharacterModel.Generation
{
    /// <summary>
    /// Toutes les couleurs d'un personnage, tirées d'un <see cref="CharacterRng"/>. Une
    /// famille de teintes par <see cref="CharacterArchetype"/> (voir <see cref="Generate"/>),
    /// mais TOUJOURS construite sur un CONTRASTE DE VALEUR assumé en trois familles
    /// clairement séparées + un accent — c'est cette alternance sombre/clair/vif qui donne le
    /// relief "CubeWorld" à un rendu plat ; une palette dérivée d'une seule teinte lit comme
    /// un aplat, quel que soit l'éclairage. Détail par archétype (voir <see cref="OutfitScheme"/>
    /// pour les plages de teinte, partagées par <see cref="GenerateHumanoid"/>) :
    ///  - Swordsman : cuir quasi noir (sombre) / blanc cassé (clair) / vert tunique (moyen
    ///    identitaire) / or saturé (accent).
    ///  - Archer : même trame que le Swordsman, teinte moyenne olive/brune (tenue de rôdeur) /
    ///    accent bronze terni (au lieu de l'or vif).
    ///  - Mage : teinte moyenne bleu-violet (robe d'arcaniste) / accent violet-magenta vif
    ///    ("lueur" arcanique, au lieu de l'or/bronze des combattants).
    ///  - Elf : teinte moyenne émeraude/sarcelle (au lieu du vert du Swordsman) / accent
    ///    argenté pâle (au lieu de l'or).
    ///  - Skeleton : cuir décomposé quasi noir (sombre) / os pâle (clair) / pagne-bandage
    ///    grisâtre (moyen identitaire) / rouille-bronze terni (accent).
    ///
    /// >>> POINT DE VARIATION PROCÉDURALE : c'est ICI qu'on élargit la diversité visuelle par
    /// archétype (nouvelles familles de teintes, voir <see cref="OutfitScheme"/>) sans toucher
    /// aux générateurs de pièces, qui ne connaissent que les rôles de couleur. La FORME
    /// (oreilles, cage thoracique, couvre-tête...) est gérée séparément par
    /// <see cref="CharacterSilhouette"/>/<see cref="CharacterSilhouetteCatalog"/>. Garder la
    /// règle des trois familles de valeur, sinon on retombe dans le rendu plat.
    /// </summary>
    public readonly struct CharacterPalette
    {
        public readonly Color32 Skin;
        public readonly Color32 SkinShadow;
        public readonly Color32 Hair;
        public readonly Color32 HairShadow;
        public readonly Color32 Eye;
        public readonly Color32 EyeWhite;

        /// <summary>Orbite creuse (squelette) : utilisée à la place de Eye/EyeWhite quand la silhouette n'a pas d'yeux.</summary>
        public readonly Color32 EyeSocket;

        public readonly Color32 Mouth;
        public readonly Color32 Blush;

        /// <summary>Teinte identitaire (tunique/robe/pagne selon l'archétype) : tunique/jupe/bonnet ou pagne.</summary>
        public readonly Color32 OutfitPrimary;

        /// <summary>Variante sombre de la teinte identitaire : taille, ombres de la tenue.</summary>
        public readonly Color32 OutfitSecondary;

        /// <summary>Blanc cassé (héros) / os clair (squelette) : col, liserés.</summary>
        public readonly Color32 OutfitTrim;

        /// <summary>Unique accent vif de l'archétype (or, bronze, argent, violet arcanique, rouille...).</summary>
        public readonly Color32 Emblem;

        /// <summary>Cuir quasi noir (héros) / cuir décomposé (squelette) : ceinture, baudrier, épaulières, poignée d'épée.</summary>
        public readonly Color32 Belt;

        /// <summary>Cuir éclairci (héros) / cuir décomposé clair (squelette) : reliefs des pièces de cuir.</summary>
        public readonly Color32 LeatherLight;

        /// <summary>Blanc cassé (héros) / os pâle (squelette) : pantalon et manches d'avant-bras.</summary>
        public readonly Color32 Pants;

        public readonly Color32 Boot;
        public readonly Color32 BootSole;

        /// <summary>Acier clair (héros) / acier rouillé (squelette) : arête/plat de la lame.</summary>
        public readonly Color32 Blade;

        /// <summary>Acier ombré (héros) / rouille sombre (squelette) : flancs de la lame (fausse ciselure).</summary>
        public readonly Color32 BladeDark;

        private CharacterPalette(
            Color32 skin,
            Color32 skinShadow,
            Color32 hair,
            Color32 hairShadow,
            Color32 eye,
            Color32 eyeWhite,
            Color32 eyeSocket,
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
            EyeSocket = eyeSocket;
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

        /// <summary>
        /// Plages de teinte propres à un archétype humanoïde, consommées par
        /// <see cref="GenerateHumanoid"/> — tout le reste de la palette (peau, cheveux, yeux,
        /// cuir, lame) est partagé entre humanoïdes, seule la teinte identitaire (tunique/
        /// robe) et l'accent changent d'un archétype à l'autre.
        /// </summary>
        private readonly struct OutfitScheme
        {
            public readonly float OutfitHueMin;
            public readonly float OutfitHueMax;
            public readonly float OutfitSatMin;
            public readonly float OutfitSatMax;
            public readonly float AccentHueMin;
            public readonly float AccentHueMax;
            public readonly float AccentSatMin;
            public readonly float AccentSatMax;
            public readonly float AccentValMin;
            public readonly float AccentValMax;

            public OutfitScheme(
                float outfitHueMin,
                float outfitHueMax,
                float outfitSatMin,
                float outfitSatMax,
                float accentHueMin,
                float accentHueMax,
                float accentSatMin,
                float accentSatMax,
                float accentValMin,
                float accentValMax
            )
            {
                OutfitHueMin = outfitHueMin;
                OutfitHueMax = outfitHueMax;
                OutfitSatMin = outfitSatMin;
                OutfitSatMax = outfitSatMax;
                AccentHueMin = accentHueMin;
                AccentHueMax = accentHueMax;
                AccentSatMin = accentSatMin;
                AccentSatMax = accentSatMax;
                AccentValMin = accentValMin;
                AccentValMax = accentValMax;
            }
        }

        /// <summary>Vert tunique + or saturé — combattant de référence.</summary>
        private static readonly OutfitScheme SwordsmanScheme = new(
            outfitHueMin: 0.28f,
            outfitHueMax: 0.34f,
            outfitSatMin: 0.6f,
            outfitSatMax: 0.8f,
            accentHueMin: 0.12f,
            accentHueMax: 0.14f,
            accentSatMin: 0.8f,
            accentSatMax: 0.9f,
            accentValMin: 0.85f,
            accentValMax: 0.95f
        );

        /// <summary>Tenue olive/brune de rôdeur + accent bronze terni (au lieu de l'or vif).</summary>
        private static readonly OutfitScheme ArcherScheme = new(
            outfitHueMin: 0.20f,
            outfitHueMax: 0.25f,
            outfitSatMin: 0.45f,
            outfitSatMax: 0.62f,
            accentHueMin: 0.07f,
            accentHueMax: 0.09f,
            accentSatMin: 0.55f,
            accentSatMax: 0.68f,
            accentValMin: 0.55f,
            accentValMax: 0.65f
        );

        /// <summary>Robe bleu-violet d'arcaniste + accent violet-magenta vif ("lueur" arcanique).</summary>
        private static readonly OutfitScheme MageScheme = new(
            outfitHueMin: 0.63f,
            outfitHueMax: 0.70f,
            outfitSatMin: 0.5f,
            outfitSatMax: 0.7f,
            accentHueMin: 0.78f,
            accentHueMax: 0.84f,
            accentSatMin: 0.7f,
            accentSatMax: 0.85f,
            accentValMin: 0.75f,
            accentValMax: 0.9f
        );

        /// <summary>Émeraude/sarcelle (au lieu du vert Swordsman) + accent argenté pâle (au lieu de l'or).</summary>
        private static readonly OutfitScheme ElfScheme = new(
            outfitHueMin: 0.40f,
            outfitHueMax: 0.46f,
            outfitSatMin: 0.55f,
            outfitSatMax: 0.75f,
            accentHueMin: 0.56f,
            accentHueMax: 0.60f,
            accentSatMin: 0.06f,
            accentSatMax: 0.14f,
            accentValMin: 0.85f,
            accentValMax: 0.95f
        );

        // internal (pas public) : CharacterRng est internal à cette assemblée (voir son
        // commentaire), donc une méthode publique ne peut pas l'exposer en paramètre (CS0051).
        // Seul CharacterModelBuilder (même assemblée) appelle cette factory ; les consommateurs
        // cross-assemblée (PlayerGearVisual...) reçoivent une CharacterPalette déjà construite
        // via CharacterModelRoot.Palette, jamais en l'appelant eux-mêmes.
        internal static CharacterPalette Generate(CharacterRng rng, CharacterArchetype archetype) =>
            archetype switch
            {
                CharacterArchetype.Skeleton => GenerateSkeleton(rng),
                CharacterArchetype.Archer => GenerateHumanoid(rng, ArcherScheme),
                CharacterArchetype.Mage => GenerateHumanoid(rng, MageScheme),
                CharacterArchetype.Elf => GenerateHumanoid(rng, ElfScheme),
                _ => GenerateHumanoid(rng, SwordsmanScheme),
            };

        /// <summary>
        /// Palette humanoïde partagée par Swordsman/Archer/Mage/Elf : peau, cheveux, yeux,
        /// cuir et lame identiques (le personnage reste lisible comme "de la même famille"),
        /// seule la teinte identitaire (tunique/robe) et l'accent varient selon
        /// <paramref name="scheme"/> — voir le commentaire de tête de fichier pour le détail
        /// par archétype.
        /// </summary>
        private static CharacterPalette GenerateHumanoid(CharacterRng rng, OutfitScheme scheme)
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

            // Famille MOYENNE (identitaire) : teinte propre à l'archétype (voir OutfitScheme),
            // toujours un cran plus sombre pour la variante secondaire afin de trancher avec
            // le blanc des manches.
            float outfitHue = rng.NextFloat(scheme.OutfitHueMin, scheme.OutfitHueMax);
            float outfitSat = rng.NextFloat(scheme.OutfitSatMin, scheme.OutfitSatMax);
            Color32 outfitPrimary = FromHsv(outfitHue, outfitSat, rng.NextFloat(0.42f, 0.52f));
            Color32 outfitSecondary = FromHsv(
                outfitHue,
                outfitSat + 0.05f,
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

            // ACCENT : teinte propre à l'archétype (or, bronze, argent, violet arcanique...),
            // réservée aux petits éléments (boucle, garde, pommeau) — voir OutfitScheme.
            Color32 emblem = FromHsv(
                rng.NextFloat(scheme.AccentHueMin, scheme.AccentHueMax),
                rng.NextFloat(scheme.AccentSatMin, scheme.AccentSatMax),
                rng.NextFloat(scheme.AccentValMin, scheme.AccentValMax)
            );

            // Acier de lame : clair froid + flanc ombré, identique pour tous les humanoïdes.
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
                eyeSocket: new Color32(20, 18, 18, 255), // inutilisé (HasEyes = true)
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

        /// <summary>
        /// Palette squelette : même règle 3 familles + 1 accent que les humanoïdes, réinterprétée en
        /// monochrome os/cuir décomposé — sombre = cuir décomposé, clair = os pâle, moyen
        /// identitaire = pagne/bandage grisâtre, accent = rouille/bronze terni (sword incluse).
        /// </summary>
        private static CharacterPalette GenerateSkeleton(CharacterRng rng)
        {
            // Famille CLAIRE : os pâle, légèrement ivoire/jauni, avec une variance individuelle
            // comme la peau du héros. Réutilisé pour Skin (crâne/mains), Pants (membres),
            // Boot (pieds) et OutfitTrim (bandages clairs).
            float boneHue = rng.NextFloat(0.09f, 0.13f);
            Color32 bone = FromHsv(
                boneHue,
                rng.NextFloat(0.10f, 0.16f),
                rng.NextFloat(0.82f, 0.92f)
            );
            Color32 boneShadow = Darken(bone, 0.72f); // crevasses/articulations

            // Champs Hair/Blush inutilisés (HasHair = HasBlush = false côté silhouette) mais
            // on leur donne quand même une valeur cohérente plutôt qu'un défaut arbitraire.
            Color32 hair = boneShadow;
            Color32 hairShadow = Darken(boneShadow, 0.7f);
            Color32 blush = bone;

            // Orbites creuses : quasi noir, à peine teinté pour éviter un noir pur qui
            // "troue" visuellement le mesh sous l'éclairage ambiant.
            Color32 eyeSocket = new(18, 16, 15, 255);
            Color32 eye = new(8, 8, 8, 255); // inutilisé (HasEyes = false)
            Color32 eyeWhite = new(230, 226, 214, 255); // inutilisé (HasEyes = false)

            // Bouche : fente sombre entre les mâchoires.
            Color32 mouth = Darken(bone, 0.35f);

            // Famille MOYENNE identitaire : pagne/bandage grisâtre plutôt qu'une teinte vive —
            // le squelette n'a pas d'"uniforme" coloré, juste des hardes.
            float ragHue = rng.NextFloat(0.06f, 0.11f);
            float ragSat = rng.NextFloat(0.08f, 0.16f);
            Color32 outfitPrimary = FromHsv(ragHue, ragSat, rng.NextFloat(0.30f, 0.40f));
            Color32 outfitSecondary = FromHsv(ragHue, ragSat + 0.04f, rng.NextFloat(0.18f, 0.24f));

            // Famille SOMBRE : cuir décomposé quasi noir + version éclaircie pour les reliefs
            // (harnais, ceinture) — même rôle que le cuir du héros, en plus terne.
            float leatherHue = rng.NextFloat(0.06f, 0.09f);
            Color32 belt = FromHsv(
                leatherHue,
                rng.NextFloat(0.30f, 0.42f),
                rng.NextFloat(0.08f, 0.13f)
            );
            Color32 leatherLight = FromHsv(
                leatherHue,
                rng.NextFloat(0.28f, 0.38f),
                rng.NextFloat(0.20f, 0.26f)
            );
            Color32 bootSole = Darken(bone, 0.55f);

            // ACCENT : rouille/bronze terni — l'unique touche de couleur saturée (boucle,
            // garde/pommeau d'épée), en écho terni à l'or du héros.
            Color32 emblem = FromHsv(
                rng.NextFloat(0.05f, 0.07f),
                rng.NextFloat(0.55f, 0.68f),
                rng.NextFloat(0.45f, 0.55f)
            );

            // Lame rouillée : acier terni + flanc plus sombre encore (piqûres de rouille).
            Color32 blade = FromHsv(
                rng.NextFloat(0.07f, 0.09f),
                rng.NextFloat(0.18f, 0.28f),
                rng.NextFloat(0.55f, 0.65f)
            );
            Color32 bladeDark = Darken(blade, 0.6f);

            return new CharacterPalette(
                skin: bone,
                skinShadow: boneShadow,
                hair: hair,
                hairShadow: hairShadow,
                eye: eye,
                eyeWhite: eyeWhite,
                eyeSocket: eyeSocket,
                mouth: mouth,
                blush: blush,
                outfitPrimary: outfitPrimary,
                outfitSecondary: outfitSecondary,
                outfitTrim: bone,
                emblem: emblem,
                belt: belt,
                leatherLight: leatherLight,
                pants: bone,
                boot: bone,
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
