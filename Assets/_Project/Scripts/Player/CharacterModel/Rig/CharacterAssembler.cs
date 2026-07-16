using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Rig
{
    /// <summary>
    /// Assemble des <see cref="BodyPart"/> en hiérarchie de GameObjects : un GameObject par
    /// pièce (MeshFilter + MeshRenderer, matériau partagé), posé sur le socket de son parent.
    /// Comme chaque mesh est généré avec son point d'attache à l'origine, l'assemblage est un
    /// simple positionnement local — et la rotation du Transform anime la pièce autour de son
    /// articulation.
    /// </summary>
    internal static class CharacterAssembler
    {
        /// <param name="boneName">
        /// Nom du GameObject créé ; laisser null pour reprendre le nom de la pièce. Sert à
        /// renommer en L/R une même pièce générée pour les deux côtés.
        /// </param>
        public static Transform Attach(
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
