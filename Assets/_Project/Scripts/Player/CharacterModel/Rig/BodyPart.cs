using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeWorld.Player.CharacterModel.Rig
{
    /// <summary>
    /// Résultat d'un générateur de pièce : un mesh dont l'ORIGINE est le point d'attache
    /// (= pivot de rotation pour l'animation), plus les sockets offerts aux pièces enfants.
    /// Exemple : le torse expose Neck/ShoulderL/ShoulderR ; le bassin expose Torso/HipL/HipR.
    /// </summary>
    internal sealed class BodyPart
    {
        public string Name { get; }
        public Mesh Mesh { get; }

        private readonly Dictionary<string, PartSocket> sockets = new();

        public BodyPart(string name, Mesh mesh, params PartSocket[] childSockets)
        {
            Name = name;
            Mesh = mesh;

            foreach (PartSocket socket in childSockets)
            {
                sockets[socket.Name] = socket;
            }
        }

        public PartSocket GetSocket(string socketName) =>
            sockets.TryGetValue(socketName, out PartSocket socket)
                ? socket
                : throw new ArgumentException(
                    $"La pièce '{Name}' n'expose pas de socket '{socketName}'."
                );
    }
}
