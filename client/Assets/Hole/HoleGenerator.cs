using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HoleGenerator : MonoBehaviour
{
    [Header("Interior Size")]
    [Tooltip("Left/right size on the local Z axis.")]
    [Min(1f)] [SerializeField] private float width = 8f;
    [Tooltip("Vertical size on the local X axis.")]
    [Min(1f)] [SerializeField] private float height = 8f;
    [Tooltip("Front/back size on the local Y axis.")]
    [Min(1f)] [SerializeField] private float depth = 8f;
    [Min(0.1f)] [SerializeField] private float wallThickness = 1f;

    [Header("Optional Appearance")]
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Material wallMaterial;

    public float Width => width;
    public float Depth => depth;
    public float Height => height;

    private void Awake()
    {
        if (Application.isPlaying)
        {
            Rebuild();
        }
    }

    private void OnValidate()
    {
        width = Mathf.Max(1f, width);
        depth = Mathf.Max(1f, depth);
        height = Mathf.Max(1f, height);
        wallThickness = Mathf.Max(0.1f, wallThickness);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildAfterValidation;
        UnityEditor.EditorApplication.delayCall += RebuildAfterValidation;
#endif
    }

#if UNITY_EDITOR
    private void RebuildAfterValidation()
    {
        if (this != null && enabled && !Application.isPlaying && gameObject.scene.IsValid())
        {
            Rebuild();
        }
    }
#endif

    [ContextMenu("Rebuild Hole")]
    public void Rebuild()
    {
        float outerWidth = width + wallThickness * 2f;
        float wallX = height * 0.5f;
        float wallY = depth * 0.5f + wallThickness * 0.5f;
        float wallZ = width * 0.5f + wallThickness * 0.5f;

        UpdatePanel(
            "Floor",
            new Vector3(-wallThickness * 0.5f, 0f, 0f),
            new Vector3(wallThickness, depth, outerWidth),
            floorMaterial);

        UpdatePanel(
            "Wall",
            new Vector3(wallX, wallY, 0f),
            new Vector3(height, wallThickness, outerWidth),
            wallMaterial);
        UpdatePanel(
            "Left Wall",
            new Vector3(wallX, 0f, wallZ),
            new Vector3(height, depth, wallThickness),
            wallMaterial);
        UpdatePanel(
            "Right Wall",
            new Vector3(wallX, 0f, -wallZ),
            new Vector3(height, depth, wallThickness),
            wallMaterial);

        RemoveLegacyPanels();
    }

    private void RemoveLegacyPanels()
    {
        RemovePanel("North Wall");
        RemovePanel("South Wall");
        RemovePanel("East Wall");
        RemovePanel("West Wall");
    }

    private void RemovePanel(string panelName)
    {
        Transform panel = transform.Find(panelName);
        if (panel == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(panel.gameObject);
        }
        else
        {
            DestroyImmediate(panel.gameObject);
        }
    }

    private void UpdatePanel(string panelName, Vector3 localPosition, Vector3 localScale, Material material)
    {
        Transform panel = transform.Find(panelName);
        if (panel == null)
        {
            GameObject panelObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelObject.name = panelName;
            panel = panelObject.transform;
            panel.SetParent(transform, false);
        }

        panel.localPosition = localPosition;
        panel.localRotation = Quaternion.identity;
        panel.localScale = localScale;

        if (material != null)
        {
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
