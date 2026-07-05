using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.XR.Interaction.Toolkit.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class VRExperimentSelectionBuilder : MonoBehaviour
{
    const string RootName = "ExperimentSelectionScreen";
    Color navy = new Color(0.012f, 0.05f, 0.105f, 0.92f);
    Color header = new Color(0.012f, 0.07f, 0.17f, 0.96f);
    Color blue = new Color(0f, 0.33f, 0.86f, 1f);
    Color brightBlue = new Color(0f, 0.62f, 1f, 1f);
    Color white = new Color(0.94f, 0.965f, 0.99f, 1f);
    Color textDark = new Color(0.035f, 0.10f, 0.21f, 1f);

    void OnEnable() { 
        //if (!Application.isPlaying) Build(); 
    }

    [ContextMenu("Build Experiment Selection Screen")]
    public void Build()
    {
        ConfigureXR();
        DeleteOld();
        var root = NewRoot();
        var controller = root.gameObject.AddComponent<VRExperimentSelectionController>();
        controller.mainMenuRoot = transform.Find("VR_REAL_3D_MENU") ? transform.Find("VR_REAL_3D_MENU").gameObject : null;
        controller.selectionScreenRoot = root.gameObject;

        BuildBackground(root);
        BuildBackButton(root, controller);
        BuildHeader(root);
        BuildInfoCard(root);
        var c1 = BuildExperimentCard(root, controller, 0, new Vector3(5, 0, -36), "Titulación ácido-base", "• Aprende a titular una solución\n  ácida con una base.\n• Observa el punto de equivalencia\n  usando un indicador.", "⚗");
        var c2 = BuildExperimentCard(root, controller, 1, new Vector3(295, 0, -36), "Mezcla con cambio de color", "• Mezcla dos soluciones para\n  provocar un cambio de color.\n• Identifica las sustancias\n  involucradas.", "➜");
        controller.experimentCards = new[] { c1, c2 };
        BuildStartButton(root, controller);
        BuildHelpPanel(root);
        controller.SelectExperiment(0);

        if (controller.mainMenuRoot) controller.mainMenuRoot.SetActive(false);
        root.gameObject.SetActive(true);
#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    void ConfigureXR()
    {
        var canvas = GetComponent<Canvas>() ?? gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main ? Camera.main : FindObjectOfType<Camera>();
        canvas.sortingOrder = 40;
        transform.position = new Vector3(0.104f, 1.2364f, 2.0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * 0.0022f;
        if (!GetComponent<TrackedDeviceGraphicRaycaster>()) gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        var es = EventSystem.current ? EventSystem.current : FindObjectOfType<EventSystem>(true);
        if (!es) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
        if (!es.GetComponent<XRUIInputModule>()) es.gameObject.AddComponent<XRUIInputModule>();
        foreach (var ray in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(true)) ray.enableUIInteraction = true;
    }

    Transform NewRoot()
    {
        var root = new GameObject(RootName).transform;
        root.SetParent(transform, false);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        return root;
    }

    void DeleteOld()
    {
        var old = transform.Find(RootName);
        if (old) DestroyImmediateSafe(old.gameObject);
    }

    void BuildBackground(Transform root)
    {
        CreatePlane(root, "fndooo_Background", new Vector3(0, 0, 145), new Vector2(1080, 620), MatTex("M_Experiment_fndooo", LoadFndooo(), Color.white));
        CreateRoundedPrism(root, "Header_Navy_Bar", new Vector3(145, 226, 16), new Vector2(635, 82), 24, 16, Mat("M_Experiment_Header_Navy", new Color(0.012f, 0.055f, 0.13f, 0.72f)));
        CreateRoundedPrism(root, "Header_Blue_Light_Streak", new Vector3(332, 198, -8), new Vector2(330, 6), 3, 4, Mat("M_Experiment_Header_Glow", new Color(0f, 0.62f, 1f, 0.85f)));
    }

    void BuildBackButton(Transform root, VRExperimentSelectionController controller)
    {
        var holder = NewButtonRoot(root, "BackButton", new Vector3(-435, 258, -38), new Vector3(122, 50, 45));
        CreateRoundedPrism(holder, "Back_Body", new Vector3(4, -3, 14), new Vector2(102, 38), 12, 16, Mat("M_Back_Navy", new Color(0.02f, 0.10f, 0.22f, 1f)));
        var face = CreateRoundedPrism(holder, "Back_Face", new Vector3(0, 0, -10), new Vector2(102, 38), 12, 10, Mat("M_Back_Face", new Color(0.04f, 0.15f, 0.30f, 1f)));
        var rim = CreateRoundedPrism(holder, "Back_Rim", new Vector3(0, 0, -18), new Vector2(108, 44), 13, 6, Mat("M_Back_Rim", blue));
        AddText(holder, "Back_Text", "←  Volver", new Vector3(0, 0, -42), 15, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(112, 40));
        var b = holder.gameObject.AddComponent<VRExperiment3DButton>();
        b.controller = controller; b.actionKind = VRExperiment3DButton.ActionKind.Back; b.movableRoot = holder; b.faceRenderer = face.GetComponent<Renderer>(); b.rimRenderer = rim.GetComponent<Renderer>(); b.iconRenderer = rim.GetComponent<Renderer>();
    }

    void BuildHeader(Transform root)
    {
        CreateRoundedPrism(root, "Header_Flask_Icon_Tile", new Vector3(-118, 226, -16), new Vector2(58, 58), 16, 16, Mat("M_Header_Icon_Blue", blue));
        AddText(root, "Header_Flask_Icon", "⚗", new Vector3(-118, 226, -58), 36, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(64, 58));
        AddText(root, "Header_Title", "Selecciona un experimento", new Vector3(155, 242, -60), 34, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, new Vector2(545, 45));
        AddText(root, "Header_Subtitle", "Elige una práctica para comenzar tu experiencia en el laboratorio.", new Vector3(158, 209, -60), 17, FontStyles.Normal, new Color(0.84f, 0.91f, 1f, 1f), TextAlignmentOptions.Left, new Vector2(560, 32));
    }

    void BuildInfoCard(Transform root)
    {
        CreateRoundedPrism(root, "InfoCard_Left", new Vector3(-374, -52, 16), new Vector2(184, 205), 20, 18, Mat("M_Info_Experiment_Navy", new Color(0.012f, 0.055f, 0.12f, 0.88f)));
        CreateRoundedPrism(root, "InfoCard_Icon_Tile", new Vector3(-430, 30, -14), new Vector2(42, 42), 14, 14, Mat("M_Info_Experiment_Icon", blue));
        AddText(root, "InfoCard_Icon", "i", new Vector3(-430, 30, -44), 28, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(44, 42));
        AddText(root, "InfoCard_Title", "Elige un experimento", new Vector3(-360, 0, -42), 14, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, new Vector2(148, 32));
        AddText(root, "InfoCard_Copy", "Cada práctica incluye\ninstrucciones guiadas\ny seguimiento de resultados.", new Vector3(-362, -60, -42), 12, FontStyles.Normal, new Color(0.84f,0.91f,1f,1f), TextAlignmentOptions.Left, new Vector2(148, 82));
        AddText(root, "InfoCard_Quest", "Optimizado para\nMeta Quest 2", new Vector3(-345, -136, -42), 12, FontStyles.Bold, new Color(0.35f,0.78f,1f,1f), TextAlignmentOptions.Left, new Vector2(140, 38));
    }

    VRExperiment3DButton BuildExperimentCard(Transform root, VRExperimentSelectionController controller, int index, Vector3 pos, string title, string desc, string icon)
    {
        var card = NewButtonRoot(root, index == 0 ? "ExperimentCard_1" : "ExperimentCard_2", pos, new Vector3(250, 330, 70));
        CreateRoundedPrism(card, "Card_White_3D_Body", new Vector3(6, -8, 34), new Vector2(238, 316), 22, 24, Mat("M_Card_Body_BlueShadow", new Color(0f, 0.18f, 0.45f, 0.22f)));
        var rim = CreateRoundedPrism(card, "Card_Blue_Selected_Rim", new Vector3(0, 0, 8), new Vector2(248, 326), 24, 8, Mat("M_Card_Rim_Blue", new Color(0f,0.45f,1f,0.45f)));
        var face = CreateRoundedPrism(card, "Card_Raised_White_Face", new Vector3(0, 0, -18), new Vector2(238, 316), 22, 16, Mat("M_Card_White_Face", new Color(0.98f,0.985f,0.995f,1f)));
        CreateRoundedPrism(card, "Experiment_Image_Panel", new Vector3(0, 98, -42), new Vector2(205, 118), 14, 10, Mat("M_Card_Image_Light", new Color(0.88f,0.94f,0.99f,1f)));
        CreatePlane(card, "Experiment_Image_Lab_Photo", new Vector3(0, 98, -54), new Vector2(190, 103), MatTex("M_Card_Image_Photo", LoadFndooo(), Color.white));

        CreateRoundedPrism(card, "Experiment_Image_Blue_Liquid", new Vector3(-38, 78, -58), new Vector2(54, 36), 12, 12, Mat("M_Image_BlueLiquid", new Color(0f,0.45f,1f,1f)));
        CreateRoundedPrism(card, "Experiment_Image_Flask", new Vector3(-40, 103, -62), new Vector2(34, 62), 10, 8, Mat("M_Image_Flask_Glass", new Color(0.86f,0.96f,1f,0.65f)));
        if (index == 1)
        {
            CreateRoundedPrism(card, "Experiment_Image_Purple_Liquid", new Vector3(45, 78, -58), new Vector2(54, 36), 12, 12, Mat("M_Image_PurpleLiquid", new Color(0.45f,0.28f,0.85f,1f)));
            AddText(card, "Reaction_Arrow", "→", new Vector3(0, 92, -72), 36, FontStyles.Bold, brightBlue, TextAlignmentOptions.Center, new Vector2(55, 45));
        }
        AddText(card, "Card_Title", title, new Vector3(0, 28, -70), 15, FontStyles.Bold, new Color(0.03f,0.10f,0.25f,1f), TextAlignmentOptions.Center, new Vector2(215, 30));
        AddText(card, "Card_Description", desc, new Vector3(0, -42, -70), 10.5f, FontStyles.Normal, new Color(0.04f,0.12f,0.28f,1f), TextAlignmentOptions.Left, new Vector2(198, 86));
        AddText(card, "Card_Level", "Nivel: básico", new Vector3(-60, -105, -70), 10, FontStyles.Bold, new Color(0.1f,0.27f,0.55f,1f), TextAlignmentOptions.Center, new Vector2(92, 22));
        CreateRoundedPrism(card, "ChooseButton_Body", new Vector3(0, -132, -52), new Vector2(178, 36), 12, 12, Mat("M_ChooseButton_White", new Color(0.98f,0.99f,1f,1f)));
        AddText(card, "ChooseButton_Text", "Elegir                         ›", new Vector3(0, -132, -78), 13, FontStyles.Bold, new Color(0.04f,0.12f,0.30f,1f), TextAlignmentOptions.Center, new Vector2(178, 34));
        var b = card.gameObject.AddComponent<VRExperiment3DButton>();
        b.controller = controller; b.actionKind = VRExperiment3DButton.ActionKind.SelectExperiment; b.experimentIndex = index; b.movableRoot = card; b.faceRenderer = face.GetComponent<Renderer>(); b.rimRenderer = rim.GetComponent<Renderer>(); b.iconRenderer = rim.GetComponent<Renderer>();
        return b;
    }

    void BuildStartButton(Transform root, VRExperimentSelectionController controller)
    {
        var holder = NewButtonRoot(root, "StartButton", new Vector3(150, -244, -40), new Vector3(270, 58, 64));
        CreateRoundedPrism(holder, "Start_Blue_Body", new Vector3(6, -6, 26), new Vector2(270, 52), 16, 24, Mat("M_Start_Body", new Color(0f,0.16f,0.48f,1f)));
        var rim = CreateRoundedPrism(holder, "Start_Bright_Rim", new Vector3(0, -2, 2), new Vector2(280, 62), 18, 8, Mat("M_Start_Rim", brightBlue));
        var face = CreateRoundedPrism(holder, "Start_Raised_Blue_Face", new Vector3(0,0,-20), new Vector2(270,52), 16, 14, Mat("M_Start_Face", new Color(0f,0.34f,0.88f,1f)));
        AddText(holder, "Start_Text", "⚗  Comenzar", new Vector3(0,0,-62), 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, new Vector2(270,50));
        var b = holder.gameObject.AddComponent<VRExperiment3DButton>();
        b.controller = controller; b.actionKind = VRExperiment3DButton.ActionKind.StartSelected; b.movableRoot = holder; b.faceRenderer = face.GetComponent<Renderer>(); b.rimRenderer = rim.GetComponent<Renderer>(); b.iconRenderer = rim.GetComponent<Renderer>();
    }

    void BuildHelpPanel(Transform root)
    {
        CreateRoundedPrism(root, "HelpPanel", new Vector3(-392, -252, 8), new Vector2(160, 48), 13, 10, Mat("M_Help_White", new Color(0.94f,0.97f,1f,0.92f)));
        AddText(root, "HelpPanel_Text", "?  Usa los controles para\napuntar y seleccionar", new Vector3(-390, -252, -34), 9.5f, FontStyles.Bold, new Color(0.07f,0.18f,0.38f,1f), TextAlignmentOptions.Center, new Vector2(152,44));
    }

    Transform NewButtonRoot(Transform parent, string name, Vector3 pos, Vector3 colliderSize)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false); t.localPosition = pos; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;
        var col = t.gameObject.AddComponent<BoxCollider>(); col.size = colliderSize; col.center = Vector3.zero;
        t.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        return t;
    }

    Transform CreateRoundedPrism(Transform parent, string name, Vector3 pos, Vector2 size, float radius, float depth, Material mat)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localRotation = Quaternion.identity;
        go.GetComponent<MeshFilter>().sharedMesh = RoundedPrismMesh(size.x, size.y, radius, depth, 8); go.GetComponent<MeshRenderer>().sharedMaterial = mat; return go.transform;
    }

    Mesh RoundedPrismMesh(float w, float h, float r, float d, int seg)
    {
        r = Mathf.Min(r, w*.48f, h*.48f); var pts = new List<Vector2>(); AddCorner(pts,new Vector2(w/2-r,h/2-r),r,0,90,seg); AddCorner(pts,new Vector2(-w/2+r,h/2-r),r,90,180,seg); AddCorner(pts,new Vector2(-w/2+r,-h/2+r),r,180,270,seg); AddCorner(pts,new Vector2(w/2-r,-h/2+r),r,270,360,seg);
        var v = new List<Vector3>(); var tr = new List<int>(); float zf=-d*.5f, zb=d*.5f; v.Add(new Vector3(0,0,zf)); foreach(var p in pts) v.Add(new Vector3(p.x,p.y,zf)); int bc=v.Count; v.Add(new Vector3(0,0,zb)); foreach(var p in pts) v.Add(new Vector3(p.x,p.y,zb)); int n=pts.Count;
        for(int i=0;i<n;i++){tr.Add(0);tr.Add(1+i);tr.Add(1+(i+1)%n);} for(int i=0;i<n;i++){tr.Add(bc);tr.Add(bc+1+(i+1)%n);tr.Add(bc+1+i);} for(int i=0;i<n;i++){int f1=1+i,f2=1+(i+1)%n,b1=bc+1+i,b2=bc+1+(i+1)%n;tr.Add(f1);tr.Add(b1);tr.Add(f2);tr.Add(f2);tr.Add(b1);tr.Add(b2);} var m=new Mesh(); m.SetVertices(v); m.SetTriangles(tr,0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }
    void AddCorner(List<Vector2> pts, Vector2 c, float r, float a0, float a1, int seg){ for(int i=0;i<=seg;i++){float a=Mathf.Lerp(a0,a1,i/(float)seg)*Mathf.Deg2Rad; pts.Add(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);} }
    Transform CreatePlane(Transform parent, string name, Vector3 pos, Vector2 size, Material mat){ var go=GameObject.CreatePrimitive(PrimitiveType.Quad); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=new Vector3(size.x,size.y,1); var c=go.GetComponent<Collider>(); if(c) DestroyImmediateSafe(c); go.GetComponent<Renderer>().sharedMaterial=mat; return go.transform; }
    TMP_Text AddText(Transform parent,string name,string text,Vector3 pos,float fontSize,FontStyles style,Color color,TextAlignmentOptions align,Vector2 box){ var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localRotation=Quaternion.identity; var rt=go.GetComponent<RectTransform>(); rt.sizeDelta=box; var tmp=go.AddComponent<TextMeshProUGUI>(); tmp.text=text; tmp.fontSize=fontSize; tmp.fontStyle=style; tmp.color=color; tmp.alignment=align; tmp.enableWordWrapping=true; tmp.overflowMode=TextOverflowModes.Ellipsis; tmp.raycastTarget=false; return tmp; }

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
        const string folder = "Assets/Generated/ExperimentSelection";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "ExperimentSelection");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = color.a < .99f ? (Shader.Find("Legacy Shaders/Transparent/Diffuse") ? Shader.Find("Legacy Shaders/Transparent/Diffuse") : Shader.Find("Standard")) : Shader.Find("Standard");
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.color = color;
        mat.renderQueue = color.a < .99f ? 3000 : 2000;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        var m = new Material(Shader.Find("Standard"));
        m.color = color;
        return m;
#endif
    }
Material MatTex(string name, Texture2D tex, Color color)
    {
#if UNITY_EDITOR
        const string folder = "Assets/Generated/ExperimentSelection";
        if (!AssetDatabase.IsValidFolder("Assets/Generated")) AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Generated", "ExperimentSelection");
        string path = folder + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find("Unlit/Texture") ? Shader.Find("Unlit/Texture") : Shader.Find("Standard");
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.mainTexture = tex;
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
#else
        return new Material(Shader.Find("Standard"));
#endif
    }
    void DestroyImmediateSafe(UnityEngine.Object o){ if(!o)return; if(Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
}
