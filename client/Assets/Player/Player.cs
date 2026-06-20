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
    [Min(1f)]
    [SerializeField] private float riseGravityMultiplier = 1.75f;
    [Min(1f)]
    [SerializeField] private float fallGravityMultiplier = 2.5f;

    [Header("Character Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform characterVisual;
    [Min(0f)]
    [SerializeField] private float facingTurnSpeed = 540f;

    private Rigidbody body;
    private InputAction moveAction;
    private InputAction jumpAction;
    private float moveInput;
    private float lastGroundedTime = float.NegativeInfinity;
    private bool jumpQueued;

    private static readonly int SpeedParameter = Animator.StringToHash("Speed");
    private static readonly int InteractParameter = Animator.StringToHash("Interact");
    private static readonly int JumpParameter = Animator.StringToHash("Jump");
    private static readonly int GroundedParameter = Animator.StringToHash("Grounded");

    private const float IdleFacingAngle = 180f;
    private const float RightFacingAngle = 90f;
    private const float LeftFacingAngle = -90f;

    private bool HasAnimatorController =>
        animator != null && animator.runtimeAnimatorController != null;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints |= RigidbodyConstraints.FreezePositionZ |
                            RigidbodyConstraints.FreezeRotation;

        InputActionAsset projectActions = InputSystem.actions;
        if (projectActions == null)
        {
            Debug.LogError("No project-wide Input Actions asset is assigned.", this);
            enabled = false;
            return;
        }

        moveAction = projectActions.FindAction("Gameplay/Move", true);
        jumpAction = projectActions.FindAction("Gameplay/Jump", true);
    }


    private void OnDisable()
    {
        moveInput = 0f;
        jumpQueued = false;

        if (HasAnimatorController)
        {
            animator.SetFloat(SpeedParameter, 0f);
        }

        if (characterVisual != null)
        {
            characterVisual.localRotation = Quaternion.Euler(0f, IdleFacingAngle, 0f);
        }
    }


    private void Update()
    {
        moveInput = moveAction.ReadValue<float>();

        bool isGrounded = IsGrounded();
        if (HasAnimatorController)
        {
            animator.SetFloat(SpeedParameter, Mathf.Abs(moveInput));
            animator.SetBool(GroundedParameter, isGrounded);
        }

        if (characterVisual != null)
        {
            float targetAngle = Mathf.Approximately(moveInput, 0f)
                ? IdleFacingAngle
                : moveInput > 0f ? RightFacingAngle : LeftFacingAngle;
            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f);
            characterVisual.localRotation = Quaternion.RotateTowards(
                characterVisual.localRotation,
                targetRotation,
                facingTurnSpeed * Time.deltaTime);
        }

        if (jumpAction.WasPressedThisFrame())
        {
            jumpQueued = true;
        }
    }

    /// <summary>Plays the animation that the future interaction system can call.</summary>
    public void PlayInteractLeft()
    {
        if (HasAnimatorController)
        {
            animator.SetTrigger(InteractParameter);
        }
    }

    private void FixedUpdate()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.x = moveInput * moveSpeed;
        velocity.z = 0f;

        if (jumpQueued && IsGrounded())
        {
            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * Physics.gravity.y * riseGravityMultiplier);
            lastGroundedTime = float.NegativeInfinity;

            if (HasAnimatorController)
            {
                animator.SetBool(GroundedParameter, false);
                animator.SetTrigger(JumpParameter);
            }
        }

        if (!IsGrounded())
        {
            float gravityMultiplier = velocity.y > 0f
                ? riseGravityMultiplier
                : fallGravityMultiplier;
            velocity.y += Physics.gravity.y *
                          (gravityMultiplier - 1f) *
                          Time.fixedDeltaTime;
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