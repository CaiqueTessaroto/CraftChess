#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReplaceTextWithTMP
{
    [MenuItem("Tools/UI/Replace Text with TMP (Scene)")]
    public static void ReplaceAllText()
    {
        Text[] texts = Object.FindObjectsOfType<Text>(true);

        int count = 0;

        foreach (Text oldText in texts)
        {
            GameObject go = oldText.gameObject;

            Undo.RegisterFullObjectHierarchyUndo(go, "Replace Text with TMP");

            // Salva propriedades importantes
            string content = oldText.text;
            Color color = oldText.color;
            int fontSize = oldText.fontSize;
            TextAnchor alignment = oldText.alignment;
            bool raycast = oldText.raycastTarget;

            RectTransform rect = oldText.GetComponent<RectTransform>();

            // Remove Text
            Object.DestroyImmediate(oldText, true);

            // Adiciona TMP
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();

            // Restaura propriedades
            tmp.text = content;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = raycast;

            tmp.alignment = ConvertAlignment(alignment);

            tmp.enableWordWrapping = true;
            tmp.richText = true;

            count++;
        }

        Debug.Log($"✔ {count} Text components converted to TextMeshProUGUI");
    }

    static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;

            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;

            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;

            default: return TextAlignmentOptions.Center;
        }
    }
}
#endif
