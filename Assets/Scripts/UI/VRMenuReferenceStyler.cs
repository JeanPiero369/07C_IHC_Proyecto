using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class VRMenuReferenceStyler : MonoBehaviour
{
    public string title = "Laboratorio Virtual VR";
    public string subtitle = "Prácticas de química en realidad virtual";
    public string[] labels = { "Iniciar experimento", "Modo libre", "Acerca de", "Salir" };
    public string[] icons = { "⚗", "▤", "i", "⏻" };

    readonly Color navy = new Color(0.025f, 0.075f, 0.145f, 0.96f);
    readonly Color navy2 = new Color(0.0f, 0.13f, 0.32f, 1f);
    readonly Color brightBlue = new Color(0.0f, 0.48f, 1f, 1f);
    readonly Color glowBlue = new Color(0.0f, 0.62f, 1f, 0.32f);
    readonly Color buttonFace = new Color(0.93f, 0.96f, 0.99f, 1f);
    readonly Color buttonSide = new Color(0.0f, 0.22f, 0.55f, 1f);
    readonly Color textColor = new Color(0.035f, 0.105f, 0.22f, 1f);

    const string RootName = "VR_Reference_Menu_Visuals";

    void OnEnable()
    {
        if (!Application.isPlaying) ApplyStyle();
    }

    [ContextMenu("Apply Reference VR Menu")]
    public void ApplyStyle()
    {
        CleanupOldVisuals();
        ConfigureCanvas();
        EnsureXRInput();
        BuildBackdrop();
        StyleTexts();
        StyleButtons();
#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void ConfigureCanvas()
    {
        var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main ? Camera.main : FindObjectOfType<Camera>();
        canvas.sortingOrder = 20;

        var rt = transform as RectTransform;
        if (rt) rt.sizeDelta = new Vector2(980f, 650f);
        transform.position = new Vector3(0.104f, 1.2364f, 2.0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.0022f;

        var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 24f;
        scaler.referencePixelsPerUnit = 100f;

        if (!GetComponent<TrackedDeviceGraphicRaycaster>()) gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    void EnsureXRInput()
    {
        var eventSystem = EventSystem.current ? EventSystem.current : FindObjectOfType<EventSystem>(true);
        if (!eventSystem)
            eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        if (!eventSystem.GetComponent<XRUIInputModule>()) eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    void CleanupOldVisuals()
    {
        DeleteChild("VR_3D_Visuals");
        DeleteChild(RootName);
        foreach (var feedback in GetComponentsInChildren<VRButton3DFeedback>(true))
            DestroyImmediateSafe(feedback);
    }

    void BuildBackdrop()
    {
        var root = NewRect(transform, RootName, Vector2.zero, new Vector2(980f, 650f));
        root.SetAsFirstSibling();

        // Main solid futuristic panel, no magenta/fuchsia.
        var panel = NewImage(root, "Main_Navy_Technology_Panel", navy, new Vector2(850f, 545f), new Vector2(15f, -8f));
        AddOutline(panel.gameObject, new Color(0f, 0.39f, 0.95f, 0.9f), new Vector2(3f, 3f));
        AddShadow(panel.gameObject, new Color(0f, 0.18f, 0.55f, 0.65f), new Vector2(0f, -14f));

        var header = NewImage(root, "Top_Header_Navy_Blue_Bar", navy2, new Vector2(770f, 105f), new Vector2(55f, 214f));
        AddShadow(header.gameObject, new Color(0f, 0.35f, 1f, 0.48f), new Vector2(12f, -4f));
        NewImage(root, "Header_Right_Blue_Glow", new Color(0f, 0.48f, 1f, 0.45f), new Vector2(360f, 10f), new Vector2(245f, 178f));

        // Flask emblem at the left of the title like the reference.
        var flaskTile = NewImage(root, "Title_Flask_Icon_Tile", new Color(0.02f, 0.13f, 0.28f, 1f), new Vector2(82f, 82f), new Vector2(-330f, 216f));
        AddOutline(flaskTile.gameObject, new Color(0.75f, 0.92f, 1f, 1f), new Vector2(3f, 3f));
        AddShadow(flaskTile.gameObject, new Color(0f, 0.42f, 1f, 0.48f), new Vector2(0f, -7f));
        NewTMP(flaskTile.rectTransform, "Flask_Icon", "⚗", 54f, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(82f, 82f));

        // Left info card from the prototype, subdued and useful.
        var info = NewImage(root, "Left_Info_Card_MetaQuest", new Color(0.035f, 0.105f, 0.18f, 0.94f), new Vector2(210f, 210f), new Vector2(-355f, -67f));
        AddOutline(info.gameObject, new Color(0.04f, 0.28f, 0.58f, 0.9f), new Vector2(2f, 2f));
        NewTMP(info.rectTransform, "Info_Title", "Selecciona una opción", 17f, FontStyles.Bold, Color.white, new Vector2(0f, 54f), new Vector2(180f, 30f));
        NewTMP(info.rectTransform, "Info_Copy", "Explora, aprende y realiza\nexperimentos de química\nen un entorno seguro\ne inmersivo.", 13f, FontStyles.Normal, new Color(0.82f, 0.9f, 1f, 1f), new Vector2(0f, -10f), new Vector2(178f, 100f));
        NewTMP(info.rectTransform, "Info_Quest", "Optimizado para\nMeta Quest 2", 13f, FontStyles.Bold, new Color(0.35f, 0.75f, 1f, 1f), new Vector2(16f, -75f), new Vector2(160f, 44f));

        CreateCube(root, "Panel_Real_Back_Depth", new Vector3(15f, -8f, 36f), new Vector3(850f, 545f, 28f), buttonSide);
    }

    void StyleTexts()
    {
        foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.name.Equals("Labvirtvr", StringComparison.OrdinalIgnoreCase))
            {
                tmp.text = title;
                tmp.color = Color.white;
                tmp.fontSize = 56f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;
                var r = tmp.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.sizeDelta = new Vector2(560f, 65f);
                r.anchoredPosition3D = new Vector3(-275f, 235f, -8f);
            }
            else if (tmp.transform.parent == transform && tmp.name == "Text (TMP)")
            {
                tmp.text = subtitle;
                tmp.color = new Color(0.78f, 0.88f, 0.98f, 1f);
                tmp.fontSize = 27f;
                tmp.fontStyle = FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Left;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;
                var r = tmp.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.sizeDelta = new Vector2(580f, 44f);
                r.anchoredPosition3D = new Vector3(-272f, 191f, -8f);
            }
        }
    }

    void StyleButtons()
    {
        var parent = transform.Find("Panel botones") as RectTransform;
        if (!parent) return;
        parent.anchorMin = parent.anchorMax = new Vector2(0.5f, 0.5f);
        parent.sizeDelta = new Vector2(660f, 410f);
        parent.anchoredPosition3D = new Vector3(95f, -45f, -18f);
        parent.localRotation = Quaternion.Euler(0f, 0f, -3f);

        var buttons = parent.GetComponentsInChildren<Button>(true);
        Array.Sort(buttons, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            var rt = b.transform as RectTransform;
            if (!rt) continue;

            ClearGeneratedButtonChildren(rt);
            var old = b.GetComponent<VRButton3DFeedback>(); if (old) DestroyImmediateSafe(old);

            var layout = b.GetComponent<LayoutGroup>(); if (layout) layout.enabled = false;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 86f);
            rt.anchoredPosition3D = new Vector3(0f, 135f - i * 105f, 0f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;

            var img = b.GetComponent<Image>() ?? b.gameObject.AddComponent<Image>();
            img.sprite = Rounded("Reference_Button_Face", 180, 80, 26);
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.01f);
            img.raycastTarget = true;
            b.transition = Selectable.Transition.ColorTint;
            var colors = b.colors;
            colors.normalColor = buttonFace;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.82f, 0.91f, 1f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.06f;
            colors.colorMultiplier = 1f;
            b.colors = colors;

            // Back glow, blue body, bevel rings, elevated face, icon tile.
            var glow = NewImage(rt, "Premium_Blue_Outer_Glow", glowBlue, new Vector2(630f, 112f), new Vector2(0f, -8f));
            glow.transform.SetAsFirstSibling();
            CreateCube(rt, "Premium_Blue_3D_Body", new Vector3(10f, -12f, 30f), new Vector3(600f, 86f, 34f), buttonSide).SetAsFirstSibling();
            var bevel = NewImage(rt, "Premium_Bevel_Blue_Rim", brightBlue, new Vector2(612f, 98f), new Vector2(0f, -4f));
            bevel.transform.SetSiblingIndex(1);
            var face = NewImage(rt, "Premium_Raised_White_Face", buttonFace, new Vector2(570f, 70f), new Vector2(18f, 0f));
            face.transform.SetAsLastSibling();
            AddOutline(face.gameObject, new Color(0.72f, 0.86f, 1f, 0.92f), new Vector2(2f, 2f));
            AddShadow(face.gameObject, new Color(0f, 0.2f, 0.58f, 0.38f), new Vector2(0f, -4f));
            b.targetGraphic = face;
            
var inner = NewImage(rt, "Premium_Inner_Highlight", new Color(1f, 1f, 1f, 0.22f), new Vector2(530f, 15f), new Vector2(48f, 25f));
            inner.transform.SetAsLastSibling();

            var iconTile = NewImage(rt, "Premium_Left_Icon_Tile", new Color(0f, 0.28f, 0.72f, 1f), new Vector2(78f, 66f), new Vector2(-245f, 0f));
            AddShadow(iconTile.gameObject, new Color(0f, 0.1f, 0.3f, 0.55f), new Vector2(0f, -5f));
            AddOutline(iconTile.gameObject, new Color(0f, 0.58f, 1f, 1f), new Vector2(2f, 2f));
            var icon = NewTMP(iconTile.rectTransform, "Icon", icons[Mathf.Min(i, icons.Length - 1)], 37f, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(78f, 66f));

            var label = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label)
            {
                label.text = labels[Mathf.Min(i, labels.Length - 1)];
                label.color = textColor;
                label.fontSize = 30f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.enableWordWrapping = false;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.sizeDelta = new Vector2(420f, 55f);
                label.rectTransform.anchoredPosition3D = new Vector3(58f, 0f, -12f);
                label.transform.SetAsLastSibling();
            }

            var dot = NewImage(rt, "Reference_Right_Blue_Dot", new Color(0.25f, 0.82f, 1f, 1f), new Vector2(14f, 14f), new Vector2(250f, 0f));
            AddShadow(dot.gameObject, new Color(0f, 0.58f, 1f, 0.9f), new Vector2(0f, 0f));

            var feedback = b.GetComponent<VRMenuPremiumFeedback>() ?? b.gameObject.AddComponent<VRMenuPremiumFeedback>();
            feedback.frontLayer = rt;
            feedback.glowLayer = glow.rectTransform;
            feedback.faceGraphic = face;
            feedback.borderGraphic = bevel;
            feedback.iconTile = iconTile;
            feedback.iconGraphic = icon;
            feedback.labelGraphic = label;
        }
    }

    void ClearGeneratedButtonChildren(RectTransform rt)
    {
        string[] prefixes = { "VR_", "Premium_", "Reference_" };
        for (int i = rt.childCount - 1; i >= 0; i--)
        {
            var child = rt.GetChild(i);
            foreach (var p in prefixes)
            {
                if (child.name.StartsWith(p, StringComparison.Ordinal))
                {
                    DestroyImmediateSafe(child.gameObject);
                    break;
                }
            }
        }
        var shadows = rt.GetComponents<Shadow>();
        foreach (var s in shadows) DestroyImmediateSafe(s);
        var outlines = rt.GetComponents<Outline>();
        foreach (var o in outlines) DestroyImmediateSafe(o);
    }

    RectTransform NewRect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return rt;
    }

    Image NewImage(Transform parent, string name, Color color, Vector2 size, Vector2 pos)
    {
        var rt = NewRect(parent, name, pos, size);
        var canvasRenderer = rt.gameObject.AddComponent<CanvasRenderer>();
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Rounded(name + "_Rounded", 160, 90, name.Contains("Panel") || name.Contains("Card") ? 18 : 24);
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    TextMeshProUGUI NewTMP(Transform parent, string name, string text, float size, FontStyles style, Color color, Vector2 pos, Vector2 rectSize)
    {
        var rt = NewRect(parent, name, pos, rectSize);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    Transform CreateCube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>(); if (col) DestroyImmediateSafe(col);
        var r = go.GetComponent<Renderer>(); if (r) r.sharedMaterial = Mat(name + "_Mat", color);
        return go.transform;
    }

    Sprite Rounded(string name, int width, int height, int radius)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu");
        string path = folder + "/" + name.Replace('/', '_') + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Max(radius - x, 0, x - (width - radius));
                float dy = Mathf.Max(radius - y, 0, y - (height - radius));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(radius, radius, radius, radius);
                importer.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
        return null;
#endif
    }

    Material Mat(string name, Color color)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        return new Material(Shader.Find("Standard")) { color = color };
#endif
    }

    void AddShadow(GameObject go, Color c, Vector2 d)
    {
        var s = go.GetComponent<Shadow>() ?? go.AddComponent<Shadow>();
        s.effectColor = c; s.effectDistance = d; s.useGraphicAlpha = true;
    }

    void AddOutline(GameObject go, Color c, Vector2 d)
    {
        var o = go.GetComponent<Outline>() ?? go.AddComponent<Outline>();
        o.effectColor = c; o.effectDistance = d; o.useGraphicAlpha = true;
    }

    void DeleteChild(string childName)
    {
        var t = transform.Find(childName);
        if (t) DestroyImmediateSafe(t.gameObject);
    }

    void DestroyImmediateSafe(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
}
