using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// I use Physics.gravity a lot instead of Vector3.up because you can point the gravity to a different direction and i want the controller to work fine
[RequireComponent(typeof(Rigidbody))]
public class SFPSC_PlayerMovement : MonoBehaviour
{
    private static Vector3 vecZero = Vector3.zero;
    private Rigidbody rb;

    private bool enableMovement = true;

    [Header("Movement properties")]
    public float walkSpeed = 8.0f;
    public float runSpeed = 12.0f;
    public float changeInStageSpeed = 10.0f; // Lerp from walk to run and backwards speed
    public float maximumPlayerSpeed = 150.0f;
    [HideInInspector] public float vInput, hInput;
    public Transform groundChecker;
    [Tooltip("Reference to the camera for movement alignment.")] // ADDED: New variable for camera reference
    public Transform cameraReference;
    public float groundCheckerDist = 0.2f;

    [Header("Jump")]
    public float jumpForce = 500.0f;
    public float jumpCooldown = 1.0f;
    private bool jumpBlocked = false;

    private SFPSC_WallRun wallRun;
    private SFPSC_GrapplingHook grapplingHook;

    private void Start()
    {
        rb = this.GetComponent<Rigidbody>();

        TryGetWallRun();
        TryGetGrapplingHook();
    }

    public void TryGetWallRun()
    {
        this.TryGetComponent<SFPSC_WallRun>(out wallRun);
    }

    public void TryGetGrapplingHook()
    {
        this.TryGetComponent<SFPSC_GrapplingHook>(out grapplingHook);
    }

    private bool isGrounded = false;
    public bool IsGrounded { get { return isGrounded; } }

    private Vector3 inputForce;
    private int i = 0;
    private float prevY;
    private void FixedUpdate()
    {
        if ((wallRun != null && wallRun.IsWallRunning) || (grapplingHook != null && grapplingHook.IsGrappling))
            isGrounded = false;
        else
        {
            // I recieved several messages that there are some bugs and I found out that the ground check is not working properly
            // so I made this one. It's faster and all it needs is the velocity of the rigidbody in two frames.
            // It works pretty well!
            isGrounded = (Mathf.Abs(rb.linearVelocity.y - prevY) < .1f) && (Physics.OverlapSphere(groundChecker.position, groundCheckerDist).Length > 1); // > 1 because it also counts the player
            prevY = rb.linearVelocity.y;
        }

        // Input
        vInput = Input.GetAxisRaw("Vertical");
        hInput = Input.GetAxisRaw("Horizontal");

        // Clamping speed
        rb.linearVelocity = ClampMag(rb.linearVelocity, maximumPlayerSpeed);

        if (!enableMovement)
            return;

        // ***** START MODIFIED MOVEMENT LOGIC *****

        // Get the camera's forward and right vectors, but zero out the Y component 
        // so the player doesn't fly up or down when looking up or down.

        Vector3 camForward = Vector3.Scale(cameraReference.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(cameraReference.right, new Vector3(1, 0, 1)).normalized;

        inputForce = (camForward * vInput + camRight * hInput).normalized * (Input.GetKey(SFPSC_KeyManager.Run) ? runSpeed : walkSpeed);

        // ***** END MODIFIED MOVEMENT LOGIC *****

        if (isGrounded)
        {
            // Jump
            if (Input.GetButton("Jump") && !jumpBlocked)
            {
                rb.AddForce(-jumpForce * rb.mass * Vector3.down);
                jumpBlocked = true;
                Invoke("UnblockJump", jumpCooldown);
            }
            // Ground controller
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, inputForce, changeInStageSpeed * Time.fixedDeltaTime);
        }
        else
            // Air control
            rb.linearVelocity = ClampSqrMag(rb.linearVelocity + inputForce * Time.fixedDeltaTime, rb.linearVelocity.sqrMagnitude);
    }

    private static Vector3 ClampSqrMag(Vector3 vec, float sqrMag)
    {
        if (vec.sqrMagnitude > sqrMag)
            vec = vec.normalized * Mathf.Sqrt(sqrMag);
        return vec;
    }

    private static Vector3 ClampMag(Vector3 vec, float maxMag)
    {
        if (vec.sqrMagnitude > maxMag * maxMag)
            vec = vec.normalized * maxMag;
        return vec;
    }

    #region Previous Ground Check
    /*private void OnCollisionStay(Collision collision)
    {
        isGrounded = false;
        Debug.Log(collision.contactCount);
        for(int i = 0; i < collision.contactCount; ++i)
        {
            if (Vector3.Dot(Vector3.up, collision.contacts[i].normal) > .2f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }*/
    #endregion

    private void UnblockJump()
    {
        jumpBlocked = false;
    }


    // Enables jumping and player movement
    public void EnableMovement()
    {
        enableMovement = true;
    }

    // Disables jumping and player movement
    public void DisableMovement()
    {
        enableMovement = false;
    }
}