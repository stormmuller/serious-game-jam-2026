using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[System.Serializable]
public class PrizeSlot
{
    public Prize prize;

    [Tooltip("Relative chance of landing on this prize, specific to this wheel. A slot with weight 2 is twice as likely as one with weight 1; the wheel segment size scales the same way.")]
    [Min(0.0001f)]
    public float weight = 1f;
}

[RequireComponent(typeof(Renderer), typeof(AudioSource))]
public class WheelSpinner : MonoBehaviour
{
    [SerializeField] private PrizeSlot[] prizes;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string spinActionName = "Wheel/Spin";

    [Header("Spin")]
    [SerializeField] private float minSpinSpeed = 720f;
    [SerializeField] private float maxSpinSpeed = 1080f;
    [SerializeField] private float deceleration = 250f;
    [SerializeField] private UnityEvent<Prize> onWheelStopped;

    [Header("Audio")]
    [SerializeField] private AudioClip wheelSpinClip;
    [SerializeField] private AudioClip itemSelectionClip;

    [Header("Visuals")]
    [SerializeField] private Renderer wheelRenderer;

    // Must match MAX_SEGMENTS in WheelShader.shader.
    private const int MaxSegments = 32;
    private const float MinWeight = 0.0001f;

    private static readonly int SegmentsId = Shader.PropertyToID("_Segments");
    private static readonly int PaletteTexId = Shader.PropertyToID("_PaletteTex");
    private static readonly int BoundariesId = Shader.PropertyToID("_Boundaries");

    private InputAction spinAction;
    private AudioSource audioSource;
    private float currentSpeed;
    private bool isSpinning;

    private MaterialPropertyBlock propertyBlock;
    private Texture2D paletteTexture;
    private readonly float[] boundariesBuffer = new float[MaxSegments];

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (inputActions != null)
        {
            spinAction = inputActions.FindAction(spinActionName, throwIfNotFound: true);
        }
    }

    private void OnEnable()
    {
        if (spinAction != null)
        {
            spinAction.performed += OnSpinPerformed;
            spinAction.Enable();
        }

        UpdateWheelVisuals();
    }

    private void OnDisable()
    {
        if (spinAction != null)
        {
            spinAction.performed -= OnSpinPerformed;
            spinAction.Disable();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    private void OnDestroy()
    {
        ReleasePaletteTexture();
    }

    private void OnValidate()
    {
        UpdateWheelVisuals();
    }

    private void OnSpinPerformed(InputAction.CallbackContext context)
    {
        Spin();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isSpinning) return;

        transform.Rotate(Vector3.forward, -currentSpeed * Time.deltaTime);
        currentSpeed = Mathf.Max(0f, currentSpeed - deceleration * Time.deltaTime);

        if (currentSpeed <= 0f)
        {
            isSpinning = false;
            StopSpinSound();

            if (itemSelectionClip != null)
            {
                audioSource.PlayOneShot(itemSelectionClip);
            }

            Debug.Log($"Wheel stopped on segment {GetCurrentPrizeSlot().prize.name} (index {GetCurrentSegment()})");
            onWheelStopped.Invoke(GetCurrentPrizeSlot().prize);
        }
    }

    public void Spin()
    {
        currentSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
        isSpinning = true;

        if (wheelSpinClip != null)
        {
            audioSource.Stop();
            audioSource.clip = wheelSpinClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void StopSpinSound()
    {
        if (audioSource == null || audioSource.clip != wheelSpinClip) return;

        audioSource.Stop();
        audioSource.clip = null;
        audioSource.loop = false;
    }

    private int GetCurrentSegment()
    {
        float[] cumulativeWeights = ComputeCumulativeWeights();
        // Segment 0 starts centered at the top (local up). The wheel is rotated via -Z each
        // frame, so eulerAngles.z grows the opposite way to the visual spin; flipping it here
        // converts that back into "how far the wheel has turned" so it maps to the right segment.
        float angle = (360f - transform.eulerAngles.z) % 360f;
        float angleNormalized = angle / 360f;

        for (int i = 0; i < cumulativeWeights.Length; i++)
        {
            if (angleNormalized < cumulativeWeights[i]) return i;
        }

        return cumulativeWeights.Length - 1;
    }

    private PrizeSlot GetCurrentPrizeSlot()
    {
        int segment = GetCurrentSegment();
        return prizes[segment];
    }

    // Cumulative, normalized (0-1) weight boundary of each prize, in the same order as the
    // palette texture. Boundary[i] is where prize i's segment ends; prize 0 starts at 0.
    private float[] ComputeCumulativeWeights()
    {
        var cumulative = new float[prizes.Length];

        float total = 0f;
        for (int i = 0; i < prizes.Length; i++)
        {
            total += GetWeight(i);
        }

        float running = 0f;
        for (int i = 0; i < prizes.Length; i++)
        {
            running += GetWeight(i);
            cumulative[i] = running / total;
        }

        // Force the last boundary to exactly 1 to avoid leaving a sliver unmapped due to float error.
        cumulative[^1] = 1f;
        return cumulative;
    }

    // Shader's _Boundaries array is fixed at MaxSegments; MaterialPropertyBlock won't allow
    // the array size to change between calls (Unity caps it to the first size used and warns),
    // so we always upload a constant-size buffer regardless of the current prize count.
    private float[] ComputeBoundariesBuffer()
    {
        float[] cumulative = ComputeCumulativeWeights();
        int count = Mathf.Min(cumulative.Length, MaxSegments);

        for (int i = 0; i < count; i++)
        {
            boundariesBuffer[i] = cumulative[i];
        }

        for (int i = count; i < MaxSegments; i++)
        {
            boundariesBuffer[i] = 1f;
        }

        return boundariesBuffer;
    }

    private float GetWeight(int index)
    {
        PrizeSlot slot = prizes[index];
        return slot != null ? Mathf.Max(slot.weight, MinWeight) : MinWeight;
    }

    public void UpdateWheelVisuals()
    {
        if (wheelRenderer == null)
        {
            wheelRenderer = GetComponent<Renderer>();
        }

        if (wheelRenderer == null) return;

        if (prizes == null || prizes.Length == 0)
        {
            ReleasePaletteTexture();
            wheelRenderer.SetPropertyBlock(null);
            return;
        }

        if (prizes.Length > MaxSegments)
        {
            Debug.LogWarning($"WheelSpinner on {name} has {prizes.Length} prizes, but the shader only supports up to {MaxSegments}.", this);
        }

        RebuildPaletteTexture();

        propertyBlock ??= new MaterialPropertyBlock();
        wheelRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(SegmentsId, prizes.Length);
        propertyBlock.SetTexture(PaletteTexId, paletteTexture);
        propertyBlock.SetFloatArray(BoundariesId, ComputeBoundariesBuffer());
        wheelRenderer.SetPropertyBlock(propertyBlock);
    }

    private void RebuildPaletteTexture()
    {
        ReleasePaletteTexture();

        paletteTexture = new Texture2D(prizes.Length, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave,
        };

        for (int i = 0; i < prizes.Length; i++)
        {
            // Segment fills are always opaque: an unset/zero-alpha wheelColor would otherwise
            // render as an invisible wedge (only the border lines would be visible).
            Color color = prizes[i]?.prize != null ? prizes[i].prize.wheelColor : Color.magenta;
            color.a = 1f;
            paletteTexture.SetPixel(i, 0, color);
        }

        paletteTexture.Apply(false, false);
    }

    private void ReleasePaletteTexture()
    {
        if (paletteTexture == null) return;

        if (Application.isPlaying)
        {
            Destroy(paletteTexture);
        }
        else
        {
            DestroyImmediate(paletteTexture);
        }

        paletteTexture = null;
    }
}
