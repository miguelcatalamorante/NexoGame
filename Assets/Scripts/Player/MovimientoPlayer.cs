using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovimientoPlayer : MonoBehaviour
{
    // ---------- Referencias ----------
    [Header("Referencias")]
    public CharacterController playerController;
    public Camera camaraPrincipal;
    public Animator animator;

    // ---------- Movimiento ----------
    [Header("Movimiento")]
    public float velocidadPlayer = 5f;
    public float runMultiplier = 1.8f;
    private Vector3 movimiento;
    private float verticalVelocity = 0f;

    // ---------- Mirar con mouse ----------
    [Header("Mirar con mouse")]
    public float mouseSensitivity = 300f;
    public bool lockCursor = true;
    private float xRotation = 0f;

    // ---------- Física ----------
    [Header("Física")]
    public float gravedad = -9.81f;

    // ---------- Agacharse ----------
    [Header("Agacharse")]
    public float crouchHeight = 1.0f;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private Vector3 originalCameraLocalPos;
    private bool isCrouching = false;

    // ---------- Salto ----------
    [Header("Salto (Y controlada por física)")]
    public float jumpSpeed = 5f; // impulso vertical

    // ---------- Root Motion Jump ----------
    [Header("Root Motion Jump")]
    public string jumpStateName = "Jumping";
    [Range(0.5f, 1.0f)] public float endJumpNormalizedTime = 0.9f;
    public float maxRootMotionJumpTime = 1.25f;

    private bool isJumpingRM = false;
    private float rmJumpStartTime = 0f;

    void Start()
    {
        if (!playerController) playerController = GetComponent<CharacterController>();
        if (!camaraPrincipal) camaraPrincipal = Camera.main;
        if (!animator) animator = GetComponent<Animator>();

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        originalControllerHeight = playerController.height;
        originalControllerCenter = playerController.center;
        if (camaraPrincipal) originalCameraLocalPos = camaraPrincipal.transform.localPosition;

        // El script controla cuándo se aplica Root Motion (solo durante el salto)
        if (animator) animator.applyRootMotion = false;
    }

    void Update()
    {
        // -------- Mirar con mouse --------
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (camaraPrincipal)
            camaraPrincipal.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

        // -------- Inputs movimiento (XZ) --------
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(inputX, 0f, inputZ);
        input = Vector3.ClampMagnitude(input, 1f);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool crouchKey = Input.GetKey(KeyCode.LeftControl);

        if (crouchKey && !isCrouching) SetCrouch(true);
        if (!crouchKey && isCrouching) SetCrouch(false);

        float speedMultiplier = 1f;
        if (isRunning && !isCrouching) speedMultiplier = runMultiplier;
        if (isCrouching) speedMultiplier *= 0.5f;

        Vector3 moveDir = (transform.right * input.x + transform.forward * input.z).normalized;
        Vector3 moveXZ = moveDir * velocidadPlayer * speedMultiplier;

        // -------- Suelo / Salto / Gravedad --------
        bool grounded = playerController.isGrounded;

        if (grounded && !isJumpingRM)
        {
            // "pegamos" al suelo si venimos cayendo
            if (verticalVelocity < 0f) verticalVelocity = -2f;

            // Iniciar salto: impulso vertical y activamos RM (para XZ del clip)
            if (Input.GetButtonDown("Jump") && !isCrouching)
            {
                verticalVelocity = jumpSpeed;   // IMPULSO EN Y AQUÍ
                isJumpingRM = true;
                rmJumpStartTime = Time.time;

                if (animator)
                {
                    animator.applyRootMotion = true; // a partir de ahora OnAnimatorMove mueve
                    animator.ResetTrigger("Jump");
                    animator.SetTrigger("Jump");
                }
            }
        }
        else
        {
            // Aplicar gravedad en el aire SIEMPRE (también durante RM)
            verticalVelocity += gravedad * Time.deltaTime;
        }

        // -------- Movimiento cuando NO hay Root Motion --------
        if (!isJumpingRM)
        {
            movimiento = moveXZ;
            movimiento.y = verticalVelocity;
            playerController.Move(movimiento * Time.deltaTime);
        }

        // -------- Lógica de salida del modo RM --------
        if (isJumpingRM && animator)
        {
            AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
            bool inJump = st.IsName(jumpStateName);

            if (!inJump || st.normalizedTime >= endJumpNormalizedTime ||
                Time.time - rmJumpStartTime > maxRootMotionJumpTime)
            {
                EndRootMotionJump();
            }
        }

        // -------- Flags del Animator --------
        if (animator)
        {
            float velX = input.x * speedMultiplier;
            float velY = input.z * speedMultiplier;

            animator.SetFloat("VelX", velX, 0.1f, Time.deltaTime);
            animator.SetFloat("VelY", velY, 0.1f, Time.deltaTime);
            animator.SetBool("Grounded", grounded);
            animator.SetBool("Crouching", isCrouching);
        }
    }

    // ---------- Root Motion: usamos XZ del clip y Y propia ----------
    void OnAnimatorMove()
    {
        if (!animator || !animator.applyRootMotion) return;

        Vector3 delta = animator.deltaPosition;

        // XZ viene del Root Motion del clip
        Vector3 deltaXZ = new Vector3(delta.x, 0f, delta.z);

        // Y viene de nuestra física (impulso + gravedad)
        float dy = verticalVelocity * Time.deltaTime;

        // Si tocamos suelo y la Y es hacia abajo, no hundimos el controller
        if (playerController.isGrounded && dy < 0f) dy = 0f;

        playerController.Move(deltaXZ + Vector3.up * dy);
    }

    private void EndRootMotionJump()
    {
        isJumpingRM = false;
        if (animator) animator.applyRootMotion = false;
        verticalVelocity = -2f; // engancha al suelo y vuelve a gravedad normal en Update
    }

    void SetCrouch(bool crouch)
    {
        if (!camaraPrincipal) return;

        if (crouch)
        {
            float delta = originalControllerHeight - crouchHeight;

            playerController.height = crouchHeight;

            Vector3 c = originalControllerCenter;
            c.y = originalControllerCenter.y - delta / 2f;
            playerController.center = c;

            camaraPrincipal.transform.localPosition = originalCameraLocalPos - new Vector3(0, delta / 2f, 0);
            isCrouching = true;
        }
        else
        {
            playerController.height = originalControllerHeight;
            playerController.center = originalControllerCenter;
            camaraPrincipal.transform.localPosition = originalCameraLocalPos;
            isCrouching = false;
        }
    }
}
