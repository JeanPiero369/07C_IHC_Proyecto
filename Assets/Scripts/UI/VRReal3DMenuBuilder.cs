using System;
using System.Collections.Generic;
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
public class VRReal3DMenuBuilder : MonoBehaviour
{
    const string RootName = "VR_REAL_3D_MENU";
    readonly string[] labels = { "Iniciar experimento", "Modo libre", "Acerca de", "Salir" };
    readonly string[] icons = { "⚗", "▤", "i", "⏻" };

    Color navy = new Color(0.012f, 0.05f, 0.105f, 1f);
    Color headerNavy = new Color(0.015f, 0.075f, 0.18f, 1f);
    Color blue = new Color(0f, 0.33f, 0.86f, 1f);
    Color brightBlue = new Color(0f, 0.62f, 1f, 1f);
    Color whiteFace = new Color(0.93f, 0.96f, 0.985f, 1f);
    Color sideBlue = new Color(0f, 0.18f, 0.48f, 1f);
    Color textDark = new Color(0.035f, 0.10f, 0.21f, 1f);

    void OnEnable()
    {
        //if (!Application.isPlaying) Build();
    }

    [ContextMenu("Build Real 3D Reference Menu")]
    public void Build()
    {
        ConfigureCanvasAndXR();
        RemovePreviousVisuals();
        var root = CreateRoot();
        BuildBackground(root);
        BuildHeader(root);
        BuildInfoCard(root);
        BuildButtons(root);
        HideOldUIVisualsButKeepCallbacks();
#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void ConfigureCanvasAndXR()
    {
        var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main ? Camera.main : FindObjectOfType<Camera>();
        canvas.sortingOrder = 30;
        transform.position = new Vector3(0.104f, 1.2364f, 2.0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.0022f;
        var rt = transform as RectTransform;
        if (rt) rt.sizeDelta = new Vector2(1040f, 650f);
        if (!GetComponent<TrackedDeviceGraphicRaycaster>()) gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        var es = EventSystem.current ? EventSystem.current : FindObjectOfType<EventSystem>(true);
        if (!es) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        if (!es.GetComponent<XRUIInputModule>()) es.gameObject.AddComponent<XRUIInputModule>();

        foreach (var ray in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true)) ray.enableUIInteraction = true;
    }

    void RemovePreviousVisuals()
    {
        foreach (string n in new[] { RootName, "VR_Reference_Menu_Visuals", "VR_3D_Visuals" })
        {
            var t = transform.Find(n);
            if (t) DestroyImmediateSafe(t.gameObject);
        }
    }

    Transform CreateRoot()
    {
        var root = new GameObject(RootName).transform;
        root.SetParent(transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        root.SetAsFirstSibling();
        return root;
    }

    void BuildBackground(Transform root)
    {
        var tex = LoadFndooo();
        var bg = CreatePlane(root, "fndooo_Lab_Background", new Vector3(0, 0, 130), new Vector2(1080, 620), tex ? MatTex("M_fndooo_Background", tex, Color.white) : Mat("M_Background_Navy", navy));
        bg.localRotation = Quaternion.identity;

        CreateRoundedPrism(root, "Main_Navy_Solid_Panel_3D", new Vector3(120, -12, 48), new Vector2(650, 430), 34, 18, Mat("M_MainPanel_Navy", new Color(0.01f, 0.045f, 0.09f, 0.30f)));
        CreateRoundedPrism(root, "Main_Panel_Back_Blue_Depth", new Vector3(130, -24, 70), new Vector2(650, 430), 34, 14, Mat("M_MainPanel_BlueDepth", new Color(0f, 0.15f, 0.38f, 0.22f)));
    }

    void BuildHeader(Transform root)
    {
        CreateRoundedPrism(root, "Top_Header_Bar_Real3D", new Vector3(132, 225, 8), new Vector2(705, 93), 26, 22, Mat("M_Header_Navy", headerNavy));
        CreateRoundedPrism(root, "Header_Right_Blue_Glow_3D", new Vector3(265, 190, -8), new Vector2(390, 9), 4, 5, Mat("M_HeaderGlow_Blue", brightBlue));
        CreateRoundedPrism(root, "Title_Flask_3D_Icon_Tile", new Vector3(-273, 226, -22), new Vector2(82, 82), 20, 22, Mat("M_FlaskTile_Blue", blue));
        AddText(root, "Title_Flask_Icon", "⚗", new Vector3(-273, 226, -72), 54, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(90, 90));
        AddText(root, "Title_Text_3D", "Laboratorio Virtual VR", new Vector3(92, 242, -72), 55, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, new Vector2(680, 70));
        AddText(root, "Subtitle_Text_3D", "Prácticas de química en realidad virtual", new Vector3(105, 197, -72), 25, FontStyles.Normal, new Color(0.82f, 0.9f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(650, 45));
    }

    void BuildInfoCard(Transform root)
    {
        CreateRoundedPrism(root, "Left_Info_Card_Real3D", new Vector3(-360, -60, 4), new Vector2(205, 205), 20, 18, Mat("M_InfoCard_Navy", new Color(0.025f, 0.09f, 0.16f, 0.98f)));
        CreateRoundedPrism(root, "Left_Info_Icon_Tile", new Vector3(-432, 18, -18), new Vector2(48, 48), 14, 12, Mat("M_InfoIcon_Blue", blue));
        AddText(root, "Left_Info_Icon", "⚗", new Vector3(-432, 18, -40), 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(55, 55));
        AddText(root, "Left_Info_Title", "Selecciona una opción", new Vector3(-355, 18, -40), 16, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, new Vector2(160, 30));
        AddText(root, "Left_Info_Copy", "Explora, aprende y realiza\nexperimentos de química\nen un entorno seguro\ne inmersivo.", new Vector3(-354, -45, -40), 13, FontStyles.Normal, new Color(0.82f, 0.9f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(165, 90));
        AddText(root, "Left_Info_Quest", "Optimizado para\nMeta Quest 2", new Vector3(-336, -124, -40), 13, FontStyles.Bold, new Color(0.35f, 0.78f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(150, 45));
    }

    void BuildButtons(Transform root)
    {
        var oldButtons = GetOldButtons();
        for (int i = 0; i < 4; i++)
        {
            Vector3 basePos = new Vector3(145, 100 - i * 88, -32);
            var holder = new GameObject("Real3D_Button_" + (i + 1) + "_" + labels[i]).transform;
            holder.SetParent(root, false);
            holder.localPosition = basePos;
            holder.localRotation = Quaternion.identity;
            holder.localScale = Vector3.one;

            // Actual modeled relief: separate blue body, bright rim, raised white face, raised icon block.
            CreateRoundedPrism(holder, "Blue_Thick_Button_Body", new Vector3(9, -9, 30), new Vector2(500, 62), 22, 28, Mat("M_ButtonBody_Blue", new Color(0f, 0.16f, 0.42f, 0.9f)));
            var rim = CreateRoundedPrism(holder, "Bright_Blue_Beveled_Rim", new Vector3(0, -2, 2), new Vector2(520, 76), 25, 10, Mat("M_ButtonRim_BrightBlue", new Color(0f, 0.52f, 1f, 0.95f)));
            var face = CreateRoundedPrism(holder, "Raised_White_Front_Face", new Vector3(22, 0, -20), new Vector2(500, 62), 22, 16, Mat("M_ButtonFace_White", whiteFace));
            var iconBlock = CreateRoundedPrism(holder, "Raised_Left_Blue_Icon_Block", new Vector3(-205, 0, -42), new Vector2(62, 54), 15, 22, Mat("M_ButtonIcon_Blue", blue));
            var glow = CreateRoundedPrism(holder, "Soft_Blue_Glow_Halo", new Vector3(0, -9, 18), new Vector2(535, 84), 28, 4, Mat("M_ButtonGlow_Blue", new Color(0f, 0.48f, 1f, 0.16f)));
            glow.SetAsFirstSibling();

            AddText(holder, "Icon_Text_3D", icons[i], new Vector3(-205, 1, -70), 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(64, 56));
            AddText(holder, "Button_Label_3D", labels[i], new Vector3(62, 0, -70), 26, FontStyles.Bold, textDark, TextAlignmentOptions.Center, new Vector2(360, 50));
            CreateRoundedPrism(holder, "Right_Blue_Dot_3D", new Vector3(220, 0, -55), new Vector2(10, 10), 5, 7, Mat("M_RightDot_Blue", brightBlue));

            var collider = holder.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(540, 92, 82);
            collider.center = new Vector3(0, -4, -10);
            var interact = holder.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            var feedback = holder.gameObject.AddComponent<VR3DMenuButtonInteractable>();
            feedback.linkedUIButton = i < oldButtons.Length ? oldButtons[i] : null;
            feedback.movableVisual = holder;
            feedback.faceRenderer = face.GetComponent<Renderer>();
            feedback.rimRenderer = rim.GetComponent<Renderer>();
            feedback.iconRenderer = iconBlock.GetComponent<Renderer>();
            feedback.glowRenderer = glow.GetComponent<Renderer>();
        }
    }

    Button[] GetOldButtons()
    {
        var parent = transform.Find("Panel botones");
        if (!parent) return new Button[0];
        var buttons = parent.GetComponentsInChildren<Button>(true);
        Array.Sort(buttons, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
        return buttons;
    }

    void HideOldUIVisualsButKeepCallbacks()
    {
        foreach (string n in new[] { "Labvirtvr", "Text (TMP)", "Panel botones" })
        {
            var t = transform.Find(n);
            if (t) t.gameObject.SetActive(false);
        }
    }

    Transform CreateRoundedPrism(Transform parent, string name, Vector3 pos, Vector2 size, float radius, float depth, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.GetComponent<MeshFilter>().sharedMesh = RoundedPrismMesh(size.x, size.y, radius, depth, 10);
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go.transform;
    }

    Mesh RoundedPrismMesh(float w, float h, float r, float d, int seg)
    {
        r = Mathf.Min(r, w * 0.48f, h * 0.48f);
        var pts = new List<Vector2>();
        AddCorner(pts, new Vector2(w/2-r, h/2-r), r, 0, 90, seg);
        AddCorner(pts, new Vector2(-w/2+r, h/2-r), r, 90, 180, seg);
        AddCorner(pts, new Vector2(-w/2+r, -h/2+r), r, 180, 270, seg);
        AddCorner(pts, new Vector2(w/2-r, -h/2+r), r, 270, 360, seg);

        var verts = new List<Vector3>();
        var tris = new List<int>();
        float zf = -d * 0.5f, zb = d * 0.5f;
        verts.Add(new Vector3(0,0,zf));
        for (int i=0;i<pts.Count;i++) verts.Add(new Vector3(pts[i].x, pts[i].y, zf));
        int backCenter = verts.Count;
        verts.Add(new Vector3(0,0,zb));
        for (int i=0;i<pts.Count;i++) verts.Add(new Vector3(pts[i].x, pts[i].y, zb));
        int n = pts.Count;
        for (int i=0;i<n;i++) { int a=1+i, b=1+((i+1)%n); tris.Add(0); tris.Add(a); tris.Add(b); }
        for (int i=0;i<n;i++) { int a=backCenter+1+i, b=backCenter+1+((i+1)%n); tris.Add(backCenter); tris.Add(b); tris.Add(a); }
        for (int i=0;i<n;i++)
        {
            int f1=1+i, f2=1+((i+1)%n), b1=backCenter+1+i, b2=backCenter+1+((i+1)%n);
            tris.Add(f1); tris.Add(b1); tris.Add(f2);
            tris.Add(f2); tris.Add(b1); tris.Add(b2);
        }
        var mesh = new Mesh();
        mesh.name = "RoundedPrismMesh";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void AddCorner(List<Vector2> pts, Vector2 c, float r, float a0, float a1, int seg)
    {
        for (int i=0;i<=seg;i++)
        {
            float a = Mathf.Lerp(a0, a1, i/((float)seg)) * Mathf.Deg2Rad;
            pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a))*r);
        }
    }

    Transform CreatePlane(Transform parent, string name, Vector3 pos, Vector2 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var col = go.GetComponent<Collider>(); if (col) DestroyImmediateSafe(col);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go.transform;
    }

TMP_Text AddText(Transform parent, string name, string text, Vector3 pos, float fontSize, FontStyles style, Color color, TextAlignmentOptions align, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = box;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = box;
        return tmp;
    }

    Texture2D LoadFndooo()
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/scripts/fndooo.png");
#else
        return null;
#endif
    }

Material Mat(string name, Color color)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu3D";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu3D");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = color.a < 0.99f
            ? (Shader.Find("Legacy Shaders/Transparent/Diffuse") ? Shader.Find("Legacy Shaders/Transparent/Diffuse") : Shader.Find("Standard"))
            : Shader.Find("Standard");
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.color = color;
        mat.renderQueue = color.a < 0.99f ? 3000 : 2000;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        var m = new Material(Shader.Find("Standard")); m.color = color; return m;
#endif
    }

Material MatTex(string name, Texture2D tex, Color color)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/VRMenu3D";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "VRMenu3D");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Unlit/Texture") ? Shader.Find("Unlit/Texture") : Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = Shader.Find("Unlit/Texture") ? Shader.Find("Unlit/Texture") : Shader.Find("Standard");
        mat.color = color;
        mat.mainTexture = tex;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        var m = new Material(Shader.Find("Unlit/Texture")); m.mainTexture = tex; m.color = color; return m;
#endif
    }

    void DestroyImmediateSafe(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj);
    }
}
