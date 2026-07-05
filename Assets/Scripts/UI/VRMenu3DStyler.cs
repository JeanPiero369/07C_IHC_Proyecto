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
public class VRMenu3DStyler : MonoBehaviour
{
    [Header("Menu Copy")]
    public string title = "Laboratorio Virtual VR";
    public string subtitle = "Prácticas de química en realidad virtual";
    public string[] buttonLabels = { "Iniciar experimento", "Modo libre", "Acerca de", "Salir" };

    [Header("Layout")]
    public Vector3 worldPosition = new Vector3(0.104f, 1.2364f, 2.0f);
    public Vector3 worldRotation = Vector3.zero;
    public Vector2 canvasSize = new Vector2(900f, 620f);
    public Vector2 buttonSize = new Vector2(560f, 74f);
    public float buttonSpacing = 92f;

    [Header("Style")]
    public Color darkPanel = new Color(0.018f, 0.035f, 0.065f, 0.88f);
    public Color blue = new Color(0.0f, 0.5f, 1f, 1f);
    public Color cyan = new Color(0.15f, 0.78f, 1f, 1f);
    public Color white = new Color(1f, 1f, 1f, 0.98f);
    public Color titleColor = new Color(0.86f, 0.97f, 1f, 1f);
    public Color textDark = new Color(0.02f, 0.09f, 0.17f, 1f);

    const string GeneratedRootName = "VR_3D_Visuals";
    const string PanelName = "VR_Floating_Lab_Panel";

    void OnEnable()
    {
        if (!Application.isPlaying)
            ApplyStyle();
    }

    [ContextMenu("Apply VR 3D Menu Style")]
    public void ApplyStyle()
    {
        var canvas = GetComponent<Canvas>();
        if (!canvas)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        transform.position = worldPosition;
        transform.rotation = Quaternion.Euler(worldRotation);
        transform.localScale = Vector3.one * 0.002f;

        var rect = transform as RectTransform;
        if (rect)
            rect.sizeDelta = canvasSize;

        var scaler = GetComponent<CanvasScaler>();
        if (!scaler)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 16f;
        scaler.referencePixelsPerUnit = 100f;

        if (!GetComponent<TrackedDeviceGraphicRaycaster>())
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        EnsureEventSystemForXR();
        EnsureCamera(canvas);
        BuildPanelVisuals();
        StyleTexts();
        StyleButtons();

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void EnsureCamera(Canvas canvas)
    {
        var cam = Camera.main;
        if (!cam)
            cam = FindObjectOfType<Camera>();
        if (cam)
            canvas.worldCamera = cam;
    }

    void EnsureEventSystemForXR()
    {
        var eventSystem = EventSystem.current;
        if (!eventSystem)
            eventSystem = FindObjectOfType<EventSystem>(true);

        if (!eventSystem)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem = go.GetComponent<EventSystem>();
        }

        if (!eventSystem.GetComponent<XRUIInputModule>())
            eventSystem.gameObject.AddComponent<XRUIInputModule>();
    }

    void BuildPanelVisuals()
    {
        var root = transform.Find(GeneratedRootName) as RectTransform;
        if (!root)
        {
            var go = new GameObject(GeneratedRootName, typeof(RectTransform));
            root = go.GetComponent<RectTransform>();
            root.SetParent(transform, false);
        }
        root.SetAsFirstSibling();
        Stretch(root);

        var panel = EnsureUI(root, PanelName, darkPanel, new Vector2(760f, 530f), Vector2.zero);
        panel.transform.SetAsFirstSibling();
        AddOrSetShadow(panel.gameObject, new Color(0f, 0.42f, 1f, 0.48f), new Vector2(0f, -14f));
        AddOrSetOutline(panel.gameObject, new Color(0.05f, 0.58f, 1f, 0.75f), new Vector2(4f, 4f));

        EnsureUI(root, "VR_Blue_Header_Glow", new Color(0f, 0.55f, 1f, 0.18f), new Vector2(650f, 84f), new Vector2(0f, 182f));
        EnsureUI(root, "VR_Lab_Backdrop_Grid", new Color(0.02f, 0.11f, 0.17f, 0.38f), new Vector2(690f, 380f), new Vector2(0f, -35f));

        CreateOrUpdatePrimitive(root, "VR_Panel_Depth_Slab", new Vector3(0f, 0f, 34f), new Vector3(760f, 530f, 24f), MakeMat("VRMenu_PanelDepth", new Color(0.0f, 0.16f, 0.32f, 1f)));
        CreateOrUpdatePrimitive(root, "VR_Left_Chemistry_Capsule", new Vector3(-335f, -130f, -8f), new Vector3(18f, 220f, 18f), MakeMat("VRMenu_CyanGlass", new Color(0.04f, 0.72f, 1f, 0.72f)));
        CreateOrUpdatePrimitive(root, "VR_Right_Chemistry_Capsule", new Vector3(335f, 90f, -8f), new Vector3(18f, 260f, 18f), MakeMat("VRMenu_BlueGlass", new Color(0.0f, 0.32f, 0.95f, 0.7f)));
    }

    void StyleTexts()
    {
        var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            if (tmp.name.Equals("Labvirtvr", StringComparison.OrdinalIgnoreCase))
            {
                tmp.text = title;
                tmp.color = titleColor;
                tmp.fontSize = 42f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                var r = tmp.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = new Vector2(840f, 80f);
                r.anchoredPosition = new Vector2(0f, 208f);
            }
            else if (tmp.transform.parent == transform || tmp.name == "Text (TMP)")
            {
                // The loose subtitle object in the original menu.
                if (tmp.transform.parent == transform)
                {
                    tmp.text = subtitle;
                    tmp.color = new Color(0.65f, 0.86f, 1f, 1f);
                    tmp.fontSize = 28f;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.enableWordWrapping = false;
                    tmp.rectTransform.sizeDelta = new Vector2(740f, 54f);
                    tmp.rectTransform.anchoredPosition = new Vector2(0f, 150f);
                }
            }
        }
    }

    void StyleButtons()
    {
        var buttons = GetComponentsInChildren<Button>(true);
        Array.Sort(buttons, (a, b) => string.Compare(a.transform.GetSiblingIndex().ToString("D3") + a.name, b.transform.GetSiblingIndex().ToString("D3") + b.name, StringComparison.Ordinal));

        RectTransform panelParent = transform.Find("Panel botones") as RectTransform;
        if (panelParent)
        {
            panelParent.anchorMin = panelParent.anchorMax = new Vector2(0.5f, 0.5f);
            panelParent.sizeDelta = new Vector2(620f, 390f);
            panelParent.anchoredPosition = new Vector2(0f, -78f);
            panelParent.localPosition = new Vector3(panelParent.localPosition.x, panelParent.localPosition.y, -12f);
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var rt = button.transform as RectTransform;
            if (!rt)
                continue;

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = buttonSize;
            rt.anchoredPosition = new Vector2(0f, 118f - i * buttonSpacing);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;

            var layout = button.GetComponent<LayoutGroup>();
            if (layout)
                layout.enabled = false;

            
var image = button.GetComponent<Image>();
            if (!image)
                image = button.gameObject.AddComponent<Image>();
            image.sprite = GetRoundedSprite("VRMenu_RoundedButton", 128, 64, 24);
            image.type = Image.Type.Sliced;
            
image.color = white;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = white;
            colors.highlightedColor = new Color(0.88f, 0.97f, 1f, 1f);
            colors.pressedColor = new Color(0.68f, 0.9f, 1f, 1f);
            colors.selectedColor = new Color(0.82f, 0.95f, 1f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            AddOrSetOutline(button.gameObject, new Color(0f, 0.48f, 1f, 0.95f), new Vector2(3f, 3f));
            AddOrSetShadow(button.gameObject, new Color(0f, 0.35f, 1f, 0.55f), new Vector2(0f, -9f));

            var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp)
            {
                if (i < buttonLabels.Length)
                    tmp.text = buttonLabels[i];
                tmp.color = textDark;
                tmp.fontSize = 32f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
                tmp.raycastTarget = false;
                tmp.rectTransform.anchorMin = Vector2.zero;
                tmp.rectTransform.anchorMax = Vector2.one;
                tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                tmp.rectTransform.offsetMin = Vector2.zero;
                tmp.rectTransform.offsetMax = Vector2.zero;
                tmp.rectTransform.anchoredPosition3D = new Vector3(0f, 0f, -8f);
            }

            var depth = CreateOrUpdatePrimitive(rt, "VR_Real_3D_Button_Depth", new Vector3(8f, -8f, 22f), new Vector3(buttonSize.x, buttonSize.y, 26f), MakeMat("VRMenu_ButtonDepth", new Color(0.0f, 0.23f, 0.55f, 1f)));
            depth.SetAsFirstSibling();
            var glow = EnsureUI(rt, "VR_Button_Blue_Glow", new Color(0f, 0.55f, 1f, 0.2f), buttonSize + new Vector2(28f, 22f), new Vector2(0f, -4f));
            glow.transform.SetAsFirstSibling();

            var feedback = button.GetComponent<VRButton3DFeedback>();
            if (!feedback)
                feedback = button.gameObject.AddComponent<VRButton3DFeedback>();
            feedback.movingRoot = rt;
            feedback.faceGraphic = image;
            feedback.labelGraphic = tmp;
            feedback.depthBody = depth.transform;
        }
    }

    Image EnsureUI(Transform parent, string name, Color color, Vector2 size, Vector2 anchoredPosition)
    {
        var child = parent.Find(name) as RectTransform;
        if (!child)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child = go.GetComponent<RectTransform>();
            child.SetParent(parent, false);
        }
        child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
        child.sizeDelta = size;
        child.anchoredPosition = anchoredPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        var image = child.GetComponent<Image>();
        image.sprite = GetRoundedSprite(name.Contains("Panel") ? "VRMenu_RoundedPanel" : "VRMenu_RoundedSoft", 128, 128, name.Contains("Panel") ? 18 : 22);
        image.type = Image.Type.Sliced;
        
image.color = color;
        image.raycastTarget = false;
        return image;
    }

    Transform CreateOrUpdatePrimitive(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material mat)
    {
        var t = parent.Find(name);
        GameObject go;
        if (!t)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            var collider = go.GetComponent<Collider>();
            if (collider)
                DestroyImmediateSafe(collider);
        }
        else
        {
            go = t.gameObject;
        }

        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = localScale;
        var renderer = go.GetComponent<Renderer>();
        if (renderer)
            renderer.sharedMaterial = mat;
        return go.transform;
    }

    Material MakeMat(string name, Color color)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        var runtimeMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard"));
        runtimeMat.color = color;
        return runtimeMat;
#endif
    }

    Sprite GetRoundedSprite(string name, int width, int height, int radius)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu");
        string path = folder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(radius - x, 0, x - (width - radius));
                    float dy = Mathf.Max(radius - y, 0, y - (height - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
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
        var tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
#endif
    }

    
void AddOrSetShadow(GameObject go, Color color, Vector2 distance)
    {
        var shadow = go.GetComponent<Shadow>();
        if (!shadow)
            shadow = go.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    void AddOrSetOutline(GameObject go, Color color, Vector2 distance)
    {
        var outline = go.GetComponent<Outline>();
        if (!outline)
            outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    void DestroyImmediateSafe(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
