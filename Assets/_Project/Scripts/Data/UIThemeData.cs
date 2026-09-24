using UnityEngine;

namespace MirrorChronicles.Data
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "MirrorChronicles/UI Theme")]
    public class UIThemeData : ScriptableObject
    {
        [Header("Background Colors")]
        public Color panelBackground = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        public Color headerBackground = new Color(0.06f, 0.06f, 0.08f, 0.9f);
        public Color rowBackground = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        public Color modalOverlay = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        [Header("Button Colors")]
        public Color primaryButton = new Color(0.75f, 0.45f, 0.15f, 1f);
        public Color secondaryButton = new Color(0.3f, 0.5f, 0.6f, 1f);
        public Color dangerButton = new Color(0.8f, 0.2f, 0.2f, 1f);
        public Color disabledButton = new Color(0.3f, 0.3f, 0.3f, 0.6f);

        [Header("Text Colors")]
        public Color textPrimary = Color.white;
        public Color textSecondary = new Color(0.7f, 0.7f, 0.7f);
        public Color textGold = new Color(0.9f, 0.8f, 0.5f);
        public Color textDanger = new Color(1f, 0.3f, 0.3f);
        public Color textSuccess = new Color(0.3f, 1f, 0.3f);

        [Header("Borders & Accents")]
        public Color borderColor = new Color(0.6f, 0.5f, 0.3f, 0.8f);
        public Color accentColor = new Color(0.8f, 0.6f, 0.2f, 1f);

        [Header("Shuimo Style")]
        public Color inkColor = new Color(0.1f, 0.1f, 0.12f, 1f);
        public Color parchmentColor = new Color(0.92f, 0.87f, 0.76f, 1f);
        public Color bronzeColor = new Color(0.72f, 0.53f, 0.26f, 1f);

        [Header("Sprites (assign when available)")]
        public Sprite panelSprite;
        public Sprite buttonSprite;
        public Sprite borderSprite;
        public Sprite parchmentBackground;
    }
}
