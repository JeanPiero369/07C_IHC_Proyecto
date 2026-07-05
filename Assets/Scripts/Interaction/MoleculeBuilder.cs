using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Constructor procedural de modelos moleculares en estilo "barras y esferas".
/// Crea átomos (esferas) y enlaces (cilindros) con efecto holográfico.
/// Usa colores CPK (Corey-Pauling-Koltun) estándar para cada elemento químico.
/// Incluye etiquetas de texto flotantes para cada átomo.
/// </summary>
[ExecuteInEditMode]
public class MoleculeBuilder : MonoBehaviour
{
    [Header("Apariencia")]
    public Color hologramColor = new Color(0f, 0.8f, 1f, 0.6f);
    public float scale = 0.15f;

    [Header("Atoms")]
    public float atomRadius = 0.5f;
    public float bondRadius = 0.08f;

    [Header("Etiquetas")]
    public float labelOffset = 1.3f;
    public int labelFontSize = 20;
    public float labelScale = 0.15f;
    public float labelBrightness = 1.5f;
    public Color labelShadowColor = new Color(0.25f, 0.25f, 0.25f, 0.9f);
    public float labelShadowOffset = 0.006f;

    // Estructura de datos para átomos
    [System.Serializable]
    public class Atom
    {
        public string symbol;
        public Vector3 position;
        public Color color;
        public float radius;

        public Atom(string sym, Vector3 pos, Color col, float rad)
        {
            symbol = sym;
            position = pos;
            color = col;
            radius = rad;
        }
    }

    // Estructura de datos para enlaces
    [System.Serializable]
    public class Bond
    {
        public int atomA;
        public int atomB;
        public BondType type;

        public Bond(int a, int b, BondType t = BondType.Single)
        {
            atomA = a;
            atomB = b;
            type = t;
        }
    }

    public enum BondType
    {
        Single,
        Double,
        Triple
    }

    // ============================================
    // Colores CPK estándar para elementos químicos
    // ============================================
    private static readonly Dictionary<string, Color> CPK_COLORS = new Dictionary<string, Color>
    {
        { "H",  new Color(1.0f, 1.0f, 1.0f, 1f) },   // Blanco
        { "O",  new Color(1.0f, 0.05f, 0.05f, 1f) }, // Rojo
        { "C",  new Color(0.2f, 0.2f, 0.2f, 1f) },   // Negro/Gris oscuro
        { "N",  new Color(0.2f, 0.2f, 0.8f, 1f) },  // Azul
        { "Cl", new Color(0.1f, 0.8f, 0.1f, 1f) },  // Verde
        { "Na", new Color(0.6f, 0.2f, 0.8f, 1f) },  // Violeta
        { "S",  new Color(1.0f, 0.8f, 0.2f, 1f) },   // Amarillo
        { "P",  new Color(1.0f, 0.5f, 0.0f, 1f) },   // Naranja
        { "F",  new Color(0.2f, 0.9f, 0.9f, 1f) },   // Cian
        { "Br", new Color(0.5f, 0.2f, 0.1f, 1f) },  // Marrón
        { "I",  new Color(0.5f, 0.0f, 0.5f, 1f) },  // Púrpura oscuro
        { "K",  new Color(0.6f, 0.0f, 0.3f, 1f) },   // Magenta oscuro
        { "Ca", new Color(0.4f, 0.4f, 0.4f, 1f) },   // Gris
        { "Fe", new Color(1.0f, 0.4f, 0.0f, 1f) },   // Naranja oscuro
        { "Mg", new Color(0.2f, 0.6f, 0.2f, 1f) },   // Verde oscuro
    };

    // Radios atómicos relativos (escala visual)
    private static readonly Dictionary<string, float> ATOM_RADII = new Dictionary<string, float>
    {
        { "H",  0.35f },
        { "O",  0.60f },
        { "C",  0.70f },
        { "N",  0.65f },
        { "Cl", 0.55f },
        { "Na", 0.65f },
        { "S",  1.04f },
        { "P",  1.00f },
        { "F",  0.50f },
        { "Br", 1.14f },
        { "I",  1.33f },
        { "K",  1.52f },
        { "Ca", 1.74f },
        { "Fe", 1.32f },
        { "Mg", 1.36f },
    };

    private List<GameObject> _createdObjects = new List<GameObject>();

    /// <summary>
    /// Obtiene el color CPK para un símbolo de elemento
    /// </summary>
    private Color GetCPKColor(string symbol)
    {
        if (CPK_COLORS.TryGetValue(symbol, out Color c))
            return c;
        return new Color(0.7f, 0.7f, 0.7f, 1f); // Gris por defecto
    }

    /// <summary>
    /// Obtiene el radio atómico para un símbolo de elemento
    /// </summary>
    private float GetAtomRadius(string symbol)
    {
        if (ATOM_RADII.TryGetValue(symbol, out float r))
            return r;
        return 0.5f; // Default
    }

    /// <summary>
    /// Construye la molécula especificada
    /// </summary>
    public void BuildMolecule(MolecularViewToggle.MoleculeType type)
    {
        ClearMolecule();

        List<Atom> atoms = new List<Atom>();
        List<Bond> bonds = new List<Bond>();

        switch (type)
        {
            case MolecularViewToggle.MoleculeType.H2O:
                BuildH2O(atoms, bonds);
                break;
            case MolecularViewToggle.MoleculeType.NaCl:
                BuildNaCl(atoms, bonds);
                break;
            case MolecularViewToggle.MoleculeType.HCl:
                BuildHCl(atoms, bonds);
                break;
            default:
                BuildH2O(atoms, bonds);
                break;
        }

        InstantiateMolecule(atoms, bonds);
    }

    /// <summary>
    /// Limpia la molécula actual
    /// </summary>
    public void ClearMolecule()
    {
        foreach (var obj in _createdObjects)
        {
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
        _createdObjects.Clear();
    }

    // ============================================
    // Definiciones de moléculas
    // ============================================

    private void BuildH2O(List<Atom> atoms, List<Bond> bonds)
    {
        // H₂O: geometría angular (~104.5°)
        float angle = 104.5f * Mathf.Deg2Rad / 2f;
        float bondLen = 1.0f;

        atoms.Add(new Atom("O", Vector3.zero, GetCPKColor("O"), GetAtomRadius("O")));
        atoms.Add(new Atom("H", new Vector3(-Mathf.Sin(angle) * bondLen, Mathf.Cos(angle) * bondLen, 0), GetCPKColor("H"), GetAtomRadius("H")));
        atoms.Add(new Atom("H", new Vector3(Mathf.Sin(angle) * bondLen, Mathf.Cos(angle) * bondLen, 0), GetCPKColor("H"), GetAtomRadius("H")));

        bonds.Add(new Bond(0, 1, BondType.Single));
        bonds.Add(new Bond(0, 2, BondType.Single));
    }

    private void BuildNaCl(List<Atom> atoms, List<Bond> bonds)
    {
        // NaCl: estructura lineal
        atoms.Add(new Atom("Na", new Vector3(-0.8f, 0, 0), GetCPKColor("Na"), GetAtomRadius("Na")));
        atoms.Add(new Atom("Cl", new Vector3(0.8f, 0, 0), GetCPKColor("Cl"), GetAtomRadius("Cl")));

        bonds.Add(new Bond(0, 1, BondType.Single));
    }

    private void BuildHCl(List<Atom> atoms, List<Bond> bonds)
    {
        // HCl: lineal
        atoms.Add(new Atom("H", new Vector3(-0.6f, 0, 0), GetCPKColor("H"), GetAtomRadius("H")));
        atoms.Add(new Atom("Cl", new Vector3(0.6f, 0, 0), GetCPKColor("Cl"), GetAtomRadius("Cl")));

        bonds.Add(new Bond(0, 1, BondType.Single));
    }

    // ============================================
    // Instanciación visual
    // ============================================

    private void InstantiateMolecule(List<Atom> atoms, List<Bond> bonds)
    {
        GameObject moleculeContainer = new GameObject("Molecule");
        moleculeContainer.transform.SetParent(transform, false);
        moleculeContainer.transform.localScale = Vector3.one * scale;
        _createdObjects.Add(moleculeContainer);

        for (int i = 0; i < atoms.Count; i++)
        {
            GameObject atomObj = CreateAtom(atoms[i], moleculeContainer.transform);
            _createdObjects.Add(atomObj);

            GameObject labelObj = CreateAtomLabel(atoms[i], moleculeContainer.transform);
            _createdObjects.Add(labelObj);
        }

        foreach (var bond in bonds)
        {
            if (bond.type == BondType.Single)
            {
                GameObject bondObj = CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform);
                _createdObjects.Add(bondObj);
            }
            else if (bond.type == BondType.Double)
            {
                Vector3 dir = (atoms[bond.atomB].position - atoms[bond.atomA].position).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized * 0.15f;

                _createdObjects.Add(CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform, perp));
                _createdObjects.Add(CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform, -perp));
            }
            else if (bond.type == BondType.Triple)
            {
                Vector3 dir = (atoms[bond.atomB].position - atoms[bond.atomA].position).normalized;
                Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized * 0.15f;

                _createdObjects.Add(CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform, perp));
                _createdObjects.Add(CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform, -perp));
                _createdObjects.Add(CreateBond(atoms[bond.atomA], atoms[bond.atomB], moleculeContainer.transform, Vector3.zero));
            }
        }
    }

    private GameObject CreateAtom(Atom atomData, Transform parent)
    {
        GameObject atom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        atom.name = "Atom_" + atomData.symbol;
        atom.transform.SetParent(parent, false);
        atom.transform.localPosition = atomData.position;
        atom.transform.localScale = Vector3.one * atomData.radius * atomRadius * 2f;

        DestroyComponentSafely(atom.GetComponent<Collider>());

        Material mat = CreateHologramMaterial(atomData.color);
        atom.GetComponent<Renderer>().material = mat;

        return atom;
    }

    private GameObject CreateBond(Atom atomA, Atom atomB, Transform parent, Vector3 offset = default)
    {
        Vector3 midPoint = (atomA.position + atomB.position) / 2f + offset;
        Vector3 direction = atomB.position - atomA.position;
        float distance = direction.magnitude;

        GameObject bond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bond.name = "Bond";
        bond.transform.SetParent(parent, false);
        bond.transform.localPosition = midPoint;

        bond.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        bond.transform.Rotate(90f, 0f, 0f);

        bond.transform.localScale = new Vector3(bondRadius, distance / 2f, bondRadius);

        DestroyComponentSafely(bond.GetComponent<Collider>());

        Material mat = CreateHologramMaterial(new Color(0.7f, 0.7f, 0.7f, 0.8f));
        bond.GetComponent<Renderer>().material = mat;

        return bond;
    }

    private GameObject CreateAtomLabel(Atom atomData, Transform parent)
    {
        Vector3 labelPos = atomData.position + Vector3.up * labelOffset;

        // Sombra del texto (detrás, ligeramente desplazada)
        GameObject shadowObj = new GameObject("LabelShadow_" + atomData.symbol);
        shadowObj.transform.SetParent(parent, false);
        shadowObj.transform.localPosition = labelPos + new Vector3(labelShadowOffset, -labelShadowOffset, 0f);
        shadowObj.transform.localScale = Vector3.one * labelScale;
        TextMesh shadowMesh = shadowObj.AddComponent<TextMesh>();
        shadowMesh.text = atomData.symbol;
        shadowMesh.fontSize = labelFontSize;
        shadowMesh.anchor = TextAnchor.MiddleCenter;
        shadowMesh.color = labelShadowColor;
        shadowMesh.alignment = TextAlignment.Center;
        shadowObj.AddComponent<BillboardLabel>();

        // Texto principal
        GameObject labelObj = new GameObject("Label_" + atomData.symbol);
        labelObj.transform.SetParent(parent, false);
        labelObj.transform.localPosition = labelPos;
        labelObj.transform.localScale = Vector3.one * labelScale;

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = atomData.symbol;
        textMesh.fontSize = labelFontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = new Color(labelBrightness, labelBrightness, labelBrightness, 1f);
        textMesh.alignment = TextAlignment.Center;

        labelObj.AddComponent<BillboardLabel>();

        return labelObj;
    }

    private Material CreateHologramMaterial(Color baseColor)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Mode", 3);
        mat.SetOverrideTag("RenderType", "Transparent");

        Color emissionColor = baseColor;
        emissionColor.a = 1f;

        mat.SetColor("_Color", new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Max(baseColor.a, 0.7f)));
        mat.SetColor("_EmissionColor", emissionColor * 1.2f);
        mat.EnableKeyword("_EMISSION");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.5f);
        mat.renderQueue = 3000;

        return mat;
    }

    private void DestroyComponentSafely(Component component)
    {
        if (component == null) return;

        if (Application.isPlaying)
        {
            Destroy(component);
        }
        else
        {
            DestroyImmediate(component);
        }
    }
}

/// <summary>
/// Componente simple para hacer que un objeto siempre mire a la cámara (billboard)
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;
        }

        transform.rotation = _mainCamera.transform.rotation;
    }
}
