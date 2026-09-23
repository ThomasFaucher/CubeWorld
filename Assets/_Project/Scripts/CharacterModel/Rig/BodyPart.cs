using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.CharacterModel.Rig
{
    /// <summary>
    /// Résultat d'un générateur de pièce : un mesh dont l'ORIGINE est le point d'attache
    /// (= pivot de rotation pour l'animation), plus les sockets offerts aux pièces enfants.
    /// Exemple : le torse expose Neck/ShoulderL/ShoulderR ; le bassin expose Torso/HipL/HipR.
    /// </summary>
    public sealed class BodyPart
    {
        public string Name { get; }
        public Mesh Mesh { get; }

        private readonly Dictionary<string, PartSocket> sockets = new();

        // internal (pas public) : PartSocket est internal à cette assemblée, donc un
        // constructeur/une méthode publics ne peuvent pas l'exposer en paramètre/retour
        // (CS0050/CS0051). Seuls les générateurs de pièces et CharacterModelBuilder (même
        // assemblée) construisent des BodyPart ou lisent leurs sockets ; les consommateurs
        // cross-assemblée (PlayerGearVisual...) ne font que recevoir/transmettre des instances
        // déjà construites (via GearGenerator.BuildSword par ex.), jamais via ce constructeur.
        internal BodyPart(string name, Mesh mesh, params PartSocket[] childSockets)
        {
            Name = name;
            Mesh = mesh;

            foreach (PartSocket socket in childSockets)
            {
                sockets[socket.Name] = socket;
            }
        }

        internal PartSocket GetSocket(string socketName) =>
            sockets.TryGetValue(socketName, out PartSocket socket)
                ? socket
                : throw new ArgumentException(
                    $"La pièce '{Name}' n'expose pas de socket '{socketName}'."
                );
    }
}
