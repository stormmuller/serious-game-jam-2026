using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class Player : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jumping")]
    [Min(0f)]
    [SerializeField] private float jumpHeight = 2f;
    [Min(0f)]
    [SerializeField] private float groundedGraceTime = 0.1f;

    private Rigidbody body;
    private InputAction moveAction;
    private InputAction jumpAction;
    private float moveInput;
    private float lastGroundedTime = float.NegativeInfinity;
    private bool jumpQueued;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints |= RigidbodyConstraints.FreezePositionZ |
                            RigidbodyConstraints.FreezeRotation;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick/x");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        moveInput = 0f;
        jumpQueued = false;
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        jumpAction.Dispose();
    }

    private void Update()
    {
        moveInput = moveAction.ReadValue<float>();

        if (jumpAction.WasPressedThisFrame())
        {
            jumpQueued = true;
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        velocity.z = 0f;

        if (jumpQueued && IsGrounded())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            lastGroundedTime = float.NegativeInfinity;
        }

        body.linearVelocity = velocity;
        jumpQueued = false;
    }

    private bool IsGrounded()
    {
        return Time.time - lastGroundedTime <= groundedGraceTime;
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int index = 0; index < collision.contactCount; index++)
        {
            if (collision.GetContact(index).normal.y >= 0.5f)
            {
                lastGroundedTime = Time.time;
                return;
            }
        }
    }
}