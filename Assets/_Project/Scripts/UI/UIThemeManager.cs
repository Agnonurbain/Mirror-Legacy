using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.Data;

namespace MirrorChronicles.UI
{
    /// <summary>
    /// Applies a UIThemeData to all tagged UI elements in the scene.
    /// Call ApplyTheme() after scene load or theme change.
    /// </summary>
    public class UIThemeManager : MonoBehaviour
    {
        public static UIThemeManager Instance { get; private set; }

        public UIThemeData currentTheme;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (currentTheme != null)
                ApplyTheme();
        }

        public void ApplyTheme()
        {
            if (currentTheme == null) return;

            foreach (var tag in FindObjectsByType<UIThemeTag>(FindObjectsSortMode.None))
            {
                ApplyToElement(tag);
            }
        }

        private void ApplyToElement(UIThemeTag tag)
        {
            var image = tag.GetComponent<Image>();
            var text = tag.GetComponent<TMP_Text>();

            switch (tag.themeRole)
            {
                case UIThemeRole.PanelBackground:
                    if (image != null) image.color = currentTheme.panelBackground;
                    if (currentTheme.panelSprite != null && image != null)
                        image.sprite = currentTheme.panelSprite;
                    break;

                case UIThemeRole.HeaderBackground:
                    if (image != null) image.color = currentTheme.headerBackground;
                    break;

                case UIThemeRole.RowBackground:
                    if (image != null) image.color = currentTheme.rowBackground;
                    break;

                case UIThemeRole.PrimaryButton:
                    if (image != null) image.color = currentTheme.primaryButton;
                    if (currentTheme.buttonSprite != null && image != null)
                        image.sprite = currentTheme.buttonSprite;
                    break;

                case UIThemeRole.SecondaryButton:
                    if (image != null) image.color = currentTheme.secondaryButton;
                    break;

                case UIThemeRole.DangerButton:
                    if (image != null) image.color = currentTheme.dangerButton;
                    break;

                case UIThemeRole.TextPrimary:
                    if (text != null) text.color = currentTheme.textPrimary;
                    break;

                case UIThemeRole.TextGold:
                    if (text != null) text.color = currentTheme.textGold;
                    break;

                case UIThemeRole.TextDanger:
                    if (text != null) text.color = currentTheme.textDanger;
                    break;

                case UIThemeRole.ModalOverlay:
                    if (image != null) image.color = currentTheme.modalOverlay;
                    break;
            }
        }
    }

    public enum UIThemeRole
    {
        PanelBackground,
        HeaderBackground,
        RowBackground,
        PrimaryButton,
        SecondaryButton,
        DangerButton,
        TextPrimary,
        TextGold,
        TextDanger,
        ModalOverlay
    }

    /// <summary>
    /// Tag component to identify a UI element's theme role.
    /// </summary>
    public class UIThemeTag : MonoBehaviour
    {
        public UIThemeRole themeRole;
    }
}
