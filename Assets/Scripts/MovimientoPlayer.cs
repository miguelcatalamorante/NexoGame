using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoPlayer : MonoBehaviour
{
    public float movimientoHorizontal;
    public float movimientoVertical;
    public CharacterController playerController;
    private Vector3 playerInput;
    public float velocidadPlayer = 5f;
    private Vector3 movimientoPlayer;
    public Camera camaraPrincipal;
    // Mouse look
    private Vector3 camForward;
    private Vector3 camRight;
    public float mouseSensitivity = 100f;  
    public bool lockCursor = true;
    private float xRotation = 0f;
    // Gravity
    public float gravedad = -9.81f;
    private float verticalVelocity = 0f;

    // Jump / Run / Crouch
    public float jumpHeight = 1.5f;
    public float runMultiplier = 1.8f;
    public float crouchHeight = 1.0f;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;
    private Vector3 originalCameraLocalPos;
    private bool isCrouching = false;

    void Start()
    {
        playerController = GetComponent<CharacterController>();
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (playerController != null)
        {
            originalControllerHeight = playerController.height;
            originalControllerCenter = playerController.center;
        }
        if (camaraPrincipal != null)
        {
            originalCameraLocalPos = camaraPrincipal.transform.localPosition;
        }
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (camaraPrincipal != null)
            camaraPrincipal.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);

        movimientoHorizontal = Input.GetAxis("Horizontal");
        movimientoVertical = Input.GetAxis("Vertical");

        playerInput = new Vector3(movimientoHorizontal, 0, movimientoVertical);
        playerInput = Vector3.ClampMagnitude(playerInput, 1);

        Vector3 move = transform.right * playerInput.x + transform.forward * playerInput.z;
        move.y = 0f;
        move.Normalize();

        // Inputs para correr y agacharse
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool crouchKey = Input.GetKey(KeyCode.LeftControl);

        // Manejo de agacharse (mantener Ctrl para agacharse)
        if (crouchKey)
        {
            if (!isCrouching)
                SetCrouch(true);
        }
        else
        {
            if (isCrouching)
                SetCrouch(false); // para simplicidad no compruebo colisiones por encima
        }

        // Velocidad final según correr / agachar
        float speedModifier = 1f;
        if (isRunning && !isCrouching) speedModifier = runMultiplier;
        if (isCrouching) speedModifier *= 0.5f; // reducir velocidad al agacharse

        // Gravity & grounding
        if (playerController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Salto con Space (Input "Jump")
        if (playerController.isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravedad);
        }

        verticalVelocity += gravedad * Time.deltaTime;

        movimientoPlayer = move * velocidadPlayer * speedModifier;
        movimientoPlayer.y = verticalVelocity;

        playerController.Move(movimientoPlayer * Time.deltaTime);
    }

    void SetCrouch(bool crouch)
    {
        if (playerController == null || camaraPrincipal == null) return;

        if (crouch)
        {
            // Ajustar height y center para que la base del collider quede en la misma Y mundial
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
            // Restaurar height y center originales
            playerController.height = originalControllerHeight;
            playerController.center = originalControllerCenter;

            camaraPrincipal.transform.localPosition = originalCameraLocalPos;
            isCrouching = false;
        }
    }

    void camDirection()
    {
        camForward = camaraPrincipal.transform.forward;
        camRight = camaraPrincipal.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

    }
}
