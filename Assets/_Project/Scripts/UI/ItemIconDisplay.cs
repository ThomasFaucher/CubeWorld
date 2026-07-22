using CubeWorld.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Applique l'icône d'un <see cref="ItemDefinition"/> sur un <see cref="Image"/> UI :
    /// Sprite si présent, sinon pastille teintée (fallback).
    /// </summary>
    public static class ItemIconDisplay
    {
        private static Sprite fallbackSprite;

        public static void ApplyTo(Image image, ItemDefinition item)
        {
            if (image == null)
            {
                return;
            }

            if (item == null)
            {
                image.enabled = false;
                image.sprite = null;
                return;
            }

            image.enabled = true;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (item.Icon != null)
            {
                image.sprite = item.Icon;
                image.color = Color.white;
                image.material = null;
                return;
            }

            // Pas d'icône : pastille colorée (évite le carré blanc du sprite UI défaut).
            image.sprite = FallbackSprite();
            image.color = item.Color;
        }

        private static Sprite FallbackSprite()
        {
            if (fallbackSprite != null)
            {
                return fallbackSprite;
            }

            Texture2D tex = Texture2D.whiteTexture;
            fallbackSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            return fallbackSprite;
        }
    }
}
