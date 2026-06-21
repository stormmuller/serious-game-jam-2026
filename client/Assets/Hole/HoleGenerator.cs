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
    [Tooltip("World-space size covered by one UV tile before material tiling is applied.")]
    [Min(0.01f)] [SerializeField] private float textureWorldSize = 20f;

    public float Width => width;
    public float Depth => depth;
    public float Height => height;

    public void SetSize(float newWidth, float newHeight, float newDepth)
    {
        width = Mathf.Max(1f, newWidth);
        height = Mathf.Max(1f, newHeight);
        depth = Mathf.Max(1f, newDepth);
        Rebuild();
    }

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
        textureWorldSize = Mathf.Max(0.01f, textureWorldSize);

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
        panel.localScale = Vector3.one;

        MeshFilter meshFilter = panel.GetComponent<MeshFilter>();
        ReplaceGeneratedMesh(meshFilter, CreatePanelMesh(localScale, localPosition));

        BoxCollider boxCollider = panel.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.center = Vector3.zero;
            boxCollider.size = localScale;
        }

        if (material != null)
        {
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
        }
    }

    private Mesh CreatePanelMesh(Vector3 size, Vector3 localPosition)
    {
        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float z = size.z * 0.5f;
        float uvScale = 1f / textureWorldSize;

        Vector3[] vertices =
        {
            // Front (+Z)
            new(-x, -y,  z), new( x, -y,  z), new( x,  y,  z), new(-x,  y,  z),
            // Back (-Z)
            new( x, -y, -z), new(-x, -y, -z), new(-x,  y, -z), new( x,  y, -z),
            // Right (+X)
            new( x, -y,  z), new( x, -y, -z), new( x,  y, -z), new( x,  y,  z),
            // Left (-X)
            new(-x, -y, -z), new(-x, -y,  z), new(-x,  y,  z), new(-x,  y, -z),
            // Top (+Y)
            new(-x,  y,  z), new( x,  y,  z), new( x,  y, -z), new(-x,  y, -z),
            // Bottom (-Y)
            new(-x, -y, -z), new( x, -y, -z), new( x, -y,  z), new(-x, -y,  z),
        };

        Vector3[] normals =
        {
            Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward,
            Vector3.back, Vector3.back, Vector3.back, Vector3.back,
            Vector3.right, Vector3.right, Vector3.right, Vector3.right,
            Vector3.left, Vector3.left, Vector3.left, Vector3.left,
            Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            Vector3.down, Vector3.down, Vector3.down, Vector3.down,
        };

        Vector2[] uvs = new Vector2[24];
        SetProjectedFaceUvs(uvs, vertices, 0, localPosition, uvScale, 0, 1);
        SetProjectedFaceUvs(uvs, vertices, 4, localPosition, uvScale, 0, 1);
        SetProjectedFaceUvs(uvs, vertices, 8, localPosition, uvScale, 2, 1);
        SetProjectedFaceUvs(uvs, vertices, 12, localPosition, uvScale, 2, 1);
        SetProjectedFaceUvs(uvs, vertices, 16, localPosition, uvScale, 0, 2);
        SetProjectedFaceUvs(uvs, vertices, 20, localPosition, uvScale, 0, 2);

        int[] triangles = new int[36];
        for (int face = 0; face < 6; face++)
        {
            int vertex = face * 4;
            int triangle = face * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        Mesh mesh = new()
        {
            name = "Hole Panel Mesh",
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles,
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetProjectedFaceUvs(
        Vector2[] uvs,
        Vector3[] vertices,
        int start,
        Vector3 localPosition,
        float uvScale,
        int uAxis,
        int vAxis)
    {
        for (int index = start; index < start + 4; index++)
        {
            Vector3 point = vertices[index] + localPosition;
            uvs[index] = new Vector2(point[uAxis], point[vAxis]) * uvScale;
        }
    }

    private static void ReplaceGeneratedMesh(MeshFilter meshFilter, Mesh mesh)
    {
        Mesh oldMesh = meshFilter.sharedMesh;
        meshFilter.sharedMesh = mesh;

        if (oldMesh == null || oldMesh.name != "Hole Panel Mesh")
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(oldMesh);
        }
        else
        {
            DestroyImmediate(oldMesh);
        }
    }
}
