using UnityEngine;
using UnityEngine.InputSystem;

public class WheelSpinner : MonoBehaviour
{
    public int numberOfSegments = 8;

    [Header("Input")]
    public InputActionAsset inputActions;
    public string spinActionName = "Wheel/Spin";

    [Header("Spin")]
    [SerializeField] private float minSpinSpeed = 720f;
    [SerializeField] private float maxSpinSpeed = 1080f;
    [SerializeField] private float deceleration = 250f;

    private InputAction spinAction;
    private float currentSpeed;
    private bool isSpinning;

    private void Awake()
    {
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
    }

    private void OnDisable()
    {
        if (spinAction != null)
        {
            spinAction.performed -= OnSpinPerformed;
            spinAction.Disable();
        }
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
            Debug.Log($"Wheel stopped on segment {GetCurrentSegment()}");
        }
    }

    public void Spin()
    {
        if (isSpinning) return;
        currentSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
        isSpinning = true;
    }

    private int GetCurrentSegment()
    {
        float anglePerSegment = 360f / numberOfSegments;
        // Segment 0 starts centered at the top (local up). The wheel is rotated via -Z each
        // frame, so eulerAngles.z grows the opposite way to the visual spin; flipping it here
        // converts that back into "how far the wheel has turned" so it maps to the right segment.
        float angle = (360f - transform.eulerAngles.z) % 360f;
        return Mathf.FloorToInt(angle / anglePerSegment) % numberOfSegments;
    }
}
