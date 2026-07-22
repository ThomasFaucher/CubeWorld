using System.Collections.Generic;
using CubeWorld.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CubeWorld.UI
{
    /// <summary>
    /// Toasts flottants : ramassage (+item) et inventaire plein.
    /// </summary>
    public sealed class PickupFeedbackUI : MonoBehaviour
    {
        private const float ToastLifetime = 1.2f;
        private const float RiseSpeed = 40f;

        private static PickupFeedbackUI instance;

        private RectTransform root;
        private readonly List<Toast> toasts = new();

        private struct Toast
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public float Age;
            public float Lifetime;
        }

        public static void Ensure(Canvas canvas)
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("PickupFeedbackUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            instance = go.AddComponent<PickupFeedbackUI>();
            instance.Build();
        }

        private void Build()
        {
            root = GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.35f);
            root.anchorMax = new Vector2(0.5f, 0.35f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero;
            root.anchoredPosition = Vector2.zero;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ItemPickedUpEvent>(OnPickedUp);
            EventBus.Subscribe<InventoryFullEvent>(OnInventoryFull);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnPickedUp);
            EventBus.Unsubscribe<InventoryFullEvent>(OnInventoryFull);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                Toast t = toasts[i];
                t.Age += Time.unscaledDeltaTime;
                float u = t.Age / t.Lifetime;
                t.Rt.anchoredPosition += Vector2.up * (RiseSpeed * Time.unscaledDeltaTime);
                t.Group.alpha = 1f - Mathf.Clamp01(u);

                if (t.Age >= t.Lifetime)
                {
                    Destroy(t.Rt.gameObject);
                    toasts.RemoveAt(i);
                }
                else
                {
                    toasts[i] = t;
                }
            }
        }

        private void OnPickedUp(ItemPickedUpEvent evt)
        {
            string qty = evt.Quantity > 1 ? $" x{evt.Quantity}" : string.Empty;
            SpawnToast($"+{evt.DisplayName}{qty}", new Color(0.85f, 1f, 0.75f, 1f));
        }

        private void OnInventoryFull(InventoryFullEvent evt)
        {
            SpawnToast("Inventaire plein", new Color(1f, 0.45f, 0.4f, 1f));
        }

        private void SpawnToast(string message, Color color)
        {
            var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(root, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(320f, 36f);
            rt.anchoredPosition = new Vector2(0f, toasts.Count * 28f);

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            Image bg = go.AddComponent<Image>();
            Texture2D tex = Texture2D.whiteTexture;
            bg.sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            bg.color = new Color(0.05f, 0.06f, 0.09f, 0.75f);
            bg.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            TMP_Text text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = message;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            RectTransform textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            toasts.Add(
                new Toast
                {
                    Rt = rt,
                    Group = group,
                    Age = 0f,
                    Lifetime = ToastLifetime,
                }
            );
        }
    }
}
