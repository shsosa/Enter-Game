using UnityEngine;

namespace FeelinSlimy
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomPlatform : MonoBehaviour
    {
        [Header("Platform Visuals")]
        [Tooltip("Tint color of this ground, platform, or obstacle block.")]
        public Color platformColor = new Color(0.28f, 0.36f, 0.48f, 1f);
        [Tooltip("Optional custom sprite for this block.")]
        public Sprite customSprite;

        private SpriteRenderer _spriteRenderer;

        private void OnEnable()
        {
            ApplyVisuals();
        }

        private void OnValidate()
        {
            ApplyVisuals();
        }

        public void ApplyVisuals()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null) return;

            if (customSprite != null)
            {
                _spriteRenderer.sprite = customSprite;
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = ProceduralVisuals.GetPlatformSprite();
            }

            _spriteRenderer.color = platformColor;
            _spriteRenderer.sortingOrder = 0;
        }
    }
}
