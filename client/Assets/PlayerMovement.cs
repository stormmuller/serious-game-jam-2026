using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActions;
    public string moveActionName = "Player/Move";

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSpeed = 10f;

    private InputAction moveAction;

    private void Awake()
    {
        if (inputActions != null)
        {
            moveAction = inputActions.FindAction(moveActionName, throwIfNotFound: true);
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.Disable();
        }
    }

    private void Update()
    {
        if (moveAction == null)
            return;

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(moveValue.x, 0f, moveValue.y);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
