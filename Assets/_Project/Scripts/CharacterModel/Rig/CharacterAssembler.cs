using UnityEngine;

namespace CubeWorld.CharacterModel.Rig
{
    /// <summary>
    /// Assemble des <see cref="BodyPart"/> en hiérarchie de GameObjects : un GameObject par
    /// pièce (MeshFilter + MeshRenderer, matériau partagé), posé sur le socket de son parent.
    /// Comme chaque mesh est généré avec son point d'attache à l'origine, l'assemblage est un
    /// simple positionnement local — et la rotation du Transform anime la pièce autour de son
    /// articulation.
    /// </summary>
    public static class CharacterAssembler
    {
        /// <param name="boneName">
        /// Nom du GameObject créé ; laisser null pour reprendre le nom de la pièce. Sert à
        /// renommer en L/R une même pièce générée pour les deux côtés.
        /// </param>
        // internal (pas public) : PartSocket est internal à cette assemblée, donc une méthode
        // publique ne peut pas l'exposer en paramètre (CS0051). Seul CharacterModelBuilder
        // (même assemblée) assemble via les sockets ; les consommateurs cross-assemblée
        // (PlayerGearVisual...) montent leurs pièces via AttachToAnchor, qui reste public.
        internal static Transform Attach(
            BodyPart part,
            Transform parent,
            PartSocket socket,
            Material material,
            string boneName = null
        )
        {
            var partObject = new GameObject(boneName ?? part.Name);
            partObject.transform.SetParent(parent, false);
            partObject.transform.localPosition = socket.LocalPosition;
            partObject.transform.localRotation = socket.LocalRotation;

            partObject.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            partObject.AddComponent<MeshRenderer>().sharedMaterial = material;

            return partObject.transform;
        }

        /// <summary>
        /// Transform vide (pas de mesh) posé exactement sur un socket : une ancre stable
        /// à laquelle une pièce peut être montée/démontée dynamiquement après coup (voir
        /// <see cref="AttachToAnchor"/>), sans dépendre de la pièce qui a exposé le socket
        /// (celle-ci n'existe plus une fois l'assemblage terminé — voir CharacterModelBuilder).
        /// </summary>
        // internal (pas public) : même raison que Attach ci-dessus (PartSocket est internal).
        internal static Transform CreateAnchor(string name, Transform parent, PartSocket socket)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = socket.LocalPosition;
            anchor.transform.localRotation = socket.LocalRotation;

            return anchor.transform;
        }

        /// <summary>
        /// Monte une pièce directement sur une ancre déjà positionnée (voir
        /// <see cref="CreateAnchor"/>) : contrairement à <see cref="Attach"/>, la pièce
        /// prend une transformation locale identité — c'est l'ancre qui porte déjà
        /// l'offset/rotation du socket d'origine.
        /// </summary>
        public static Transform AttachToAnchor(BodyPart part, Transform anchor, Material material)
        {
            var partObject = new GameObject(part.Name);
            partObject.transform.SetParent(anchor, false);
            partObject.transform.localPosition = Vector3.zero;
            partObject.transform.localRotation = Quaternion.identity;

            partObject.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            partObject.AddComponent<MeshRenderer>().sharedMaterial = material;

            return partObject.transform;
        }

        /// <summary>
        /// Reparente une pièce déjà montée (voir <see cref="AttachToAnchor"/>) sur une AUTRE
        /// ancre — ex. faire passer une arme/un outil du dos à la main pendant une attaque/le
        /// minage. Même convention de pivot que <see cref="AttachToAnchor"/> : position/
        /// rotation locales remises à zéro, c'est l'ancre cible qui porte l'offset.
        /// </summary>
        public static void MoveToAnchor(Transform partTransform, Transform anchor)
        {
            partTransform.SetParent(anchor, false);
            partTransform.localPosition = Vector3.zero;
            partTransform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Matériau unique partagé par toutes les pièces (vertex colors, flat shading) :
        /// une seule instance pour tout le personnage, le batching regroupe les draw calls.
        /// </summary>
        public static Material CreateSharedMaterial()
        {
            // Shader dédié personnage (bandes cel creuses + AO vertex en alpha) ; repli sur
            // le shader terrain puis l'URP Lit si les assets ne sont pas encore importés.
            Shader shader = Shader.Find("CubeWorld/VoxelCharacter");
            if (shader == null)
            {
                shader = Shader.Find("CubeWorld/VoxelTerrain");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            return shader != null ? new Material(shader) : null;
        }
    }
}
