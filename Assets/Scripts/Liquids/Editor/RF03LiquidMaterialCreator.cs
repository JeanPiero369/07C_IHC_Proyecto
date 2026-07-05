#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper for RF-03. Creates a reusable transparent liquid material asset
/// configured for URP/Lit when available, with a Standard fallback.
/// Menu: Tools/RF-03/Create Transparent Liquid Material
/// </summary>
public static class RF03LiquidMaterialCreator
{
    private const string MaterialDirectory = "Assets/Materials/RF03_Liquids";
    private const string MaterialPath = MaterialDirectory + "/RF03_TransparentLiquid_URP.mat";

    [MenuItem("Tools/RF-03/Create Transparent Liquid Material")]
    public static void CreateTransparentLiquidMaterial()
    {
        Material material = GetOrCreateTransparentLiquidMaterial();
        Selection.activeObject = material;
        Debug.Log("RF-03 transparent liquid material ready: " + MaterialPath);
    }

    [MenuItem("Tools/RF-03/Add Liquid Visual To Selected Container")]
    public static void AddLiquidVisualToSelectedContainer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("RF-03", "Selecciona primero un recipiente en la jerarquía, por ejemplo FlaskGrabbable o Tube01Grabbable.", "OK");
            return;
        }

        GameObject liquid = new GameObject("Liquid_RF03");
        Undo.RegisterCreatedObjectUndo(liquid, "Add RF-03 Liquid Visual");
        liquid.transform.SetParent(selected.transform, false);
        liquid.transform.localPosition = Vector3.zero;
        liquid.transform.localRotation = Quaternion.identity;
        liquid.transform.localScale = Vector3.one;

        LiquidVisualController controller = Undo.AddComponent<LiquidVisualController>(liquid);
        SerializedObject serializedController = new SerializedObject(controller);
        SerializedProperty materialProperty = serializedController.FindProperty("liquidMaterial");
        if (materialProperty != null)
        {
            materialProperty.objectReferenceValue = GetOrCreateTransparentLiquidMaterial();
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.activeGameObject = liquid;
        EditorGUIUtility.PingObject(liquid);
        Debug.Log("RF-03 liquid visual added under: " + selected.name);
    }

    [MenuItem("Tools/RF-03/Disable Legacy Liquid Visuals In Selected")]
    public static void DisableLegacyLiquidVisualsInSelected()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("RF-03", "Selecciona un envase o un objeto padre que contenga los líquidos antiguos.", "OK");
            return;
        }

        int disabledCount = 0;
        Transform[] children = selected.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == selected.transform)
            {
                continue;
            }

            string childName = child.name.ToLowerInvariant();
            bool looksLikeOldLiquid = childName.Contains("liquid") || childName.Contains("liquids") || childName.Contains("agua") || childName.Contains("solution");
            bool isRF03 = childName.Contains("rf03") || childName.Contains("liquid_rf03");

            if (!looksLikeOldLiquid || isRF03)
            {
                continue;
            }

            Renderer renderer = child.GetComponent<Renderer>();
            ParticleSystem particleSystem = child.GetComponent<ParticleSystem>();
            if (renderer == null && particleSystem == null)
            {
                continue;
            }

            Undo.RecordObject(child.gameObject, "Disable legacy liquid visual");
            child.gameObject.SetActive(false);
            disabledCount++;
        }

        Debug.Log($"RF-03 disabled {disabledCount} legacy liquid visual object(s) under {selected.name}. Liquid_RF03 objects were preserved.");
    }

    private static Material GetOrCreateTransparentLiquidMaterial()
    {
        if (!Directory.Exists(MaterialDirectory))
        {
            Directory.CreateDirectory(MaterialDirectory);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.name = "RF03_TransparentLiquid_URP";
        material.color = new Color(0.2f, 0.65f, 1f, 0.45f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // URP/Lit properties.
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.78f);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.2f, 0.65f, 1f, 0.45f));

        // Built-in Standard fallback properties.
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.2f, 0.65f, 1f, 0.45f));

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return material;
    }
}
#endif
