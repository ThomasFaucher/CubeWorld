using UnityEngine;

namespace CubeWorld.CharacterModel.Rig
{
    /// <summary>
    /// Point d'ancrage offert par une pièce à ses pièces enfants : position + orientation
    /// exprimées dans le repère LOCAL de la pièce (unités monde, pivot de la pièce à
    /// l'origine). Convention d'assemblage : le mesh de chaque pièce est généré avec son
    /// propre point d'attache à l'origine, donc brancher un enfant = poser son Transform
    /// exactement sur le socket du parent (voir <see cref="CharacterAssembler"/>).
    /// N'importe quelle tête/bras/jambe générée peut ainsi s'attacher à n'importe quel
    /// torse qui expose le socket correspondant.
    /// </summary>
    internal readonly struct PartSocket
    {
        public readonly string Name;
        public readonly Vector3 LocalPosition;
        public readonly Quaternion LocalRotation;

        public PartSocket(string name, Vector3 localPosition, Quaternion localRotation)
        {
            Name = name;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
        }

        public PartSocket(string name, Vector3 localPosition)
            : this(name, localPosition, Quaternion.identity) { }
    }
}
