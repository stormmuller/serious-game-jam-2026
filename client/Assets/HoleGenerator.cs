using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class HoleGenerator : MonoBehaviour
{
    [Header("Interior Size")]
    [Min(1f)] [SerializeField] private float width = 8f;
    [Min(1f)] [SerializeField] private float depth = 8f;
    [Min(1f)] [SerializeField] private float height = 8f;
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
        float outerDepth = depth + wallThickness * 2f;
        float wallY = height * 0.5f;
        float wallX = width * 0.5f + wallThickness * 0.5f;
        float wallZ = depth * 0.5f + wallThickness * 0.5f;

        UpdatePanel(
            "Floor",
            new Vector3(0f, -wallThickness * 0.5f, 0f),
            new Vector3(outerWidth, wallThickness, outerDepth),
            floorMaterial);

        UpdatePanel(
            "North Wall",
            new Vector3(0f, wallY, wallZ),
            new Vector3(outerWidth, height, wallThickness),
            wallMaterial);
        UpdatePanel(
            "South Wall",
            new Vector3(0f, wallY, -wallZ),
            new Vector3(outerWidth, height, wallThickness),
            wallMaterial);
        UpdatePanel(
            "East Wall",
            new Vector3(wallX, wallY, 0f),
            new Vector3(wallThickness, height, depth),
            wallMaterial);
        UpdatePanel(
            "West Wall",
            new Vector3(-wallX, wallY, 0f),
            new Vector3(wallThickness, height, depth),
            wallMaterial);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
        if (movement == null)
        {
            return;
        }

        Transform player = movement.transform;
        Vector3 localPosition = transform.InverseTransformPoint(player.position);
        if (localPosition.y >= height)
        {
            return;
        }

        const float playerPadding = 0.5f;
        localPosition.x = Mathf.Clamp(localPosition.x, -width * 0.5f + playerPadding, width * 0.5f - playerPadding);
        localPosition.z = Mathf.Clamp(localPosition.z, -depth * 0.5f + playerPadding, depth * 0.5f - playerPadding);
        player.position = transform.TransformPoint(localPosition);
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
