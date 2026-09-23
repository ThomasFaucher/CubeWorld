using System.Runtime.CompilerServices;

// Autorise les tests edit-mode (VoxelGrid/VoxelStamper sont internal à cette assemblée par
// conception, voir CharacterModel/README.md §1 — seule l'API publique documentée doit être
// consommée par du code de gameplay, mais les tests ont besoin d'accéder aux briques bas
// niveau directement).
[assembly: InternalsVisibleTo("CubeWorld.Tests.EditMode")]
