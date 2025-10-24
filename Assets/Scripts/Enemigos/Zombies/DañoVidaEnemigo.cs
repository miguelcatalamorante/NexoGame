using System;
using UnityEngine;

public class DañoVidaEnemigo : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 50f;
    public float vidaActual = 50f;
    public bool destruirAlMorir = true;

    [Header("Detección y movimiento")]
    public string targetTag = "Player";
    public float detectionRange = 10f;
    public float moveSpeed = 3f;
    public float runSpeed = 5f;
    public float stoppingDistance = 1.2f;
    public float rotationSpeed = 10f;

    [Header("Ataque cuerpo a cuerpo")]
    public float dañoMelee = 15f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public LayerMask targetLayers;
    [Tooltip("Tiempo que el enemigo permanece parado cuando realiza el ataque")]
    public float attackStopDuration = 0.6f;

    public event Action OnDeath;

    // ---------------- Animator ----------------
    [Header("Animator")]
    public Animator anim;                 // arrastrar en el Inspector
    [Tooltip("Nombre del parámetro float para el Blend Tree")]
    public string pSpeed = "Speed";
    [Tooltip("Nombre trigger Attack en el Animator")]
    public string pAttack = "Attack";
    // ------------------------------------------

    private Transform target;
    private float lastAttackTime = -999f;

    // Movimiento seguro
    private Rigidbody rb;
    private Vector3 desiredVelocity = Vector3.zero;

    // Estado de ataque para detener movimiento mientras ataca
    private bool isAttacking = false;
    private float attackEndTime = 0f;

    // Suavizado de Speed para el Blend Tree
    private float speedAnim;          // valor actual que enviamos al Animator
    private float speedAnimVelRef;    // ref para SmoothDamp
    public float speedAnimSmooth = 0.08f; // cuanto más pequeño, más suave

    void Start()
    {
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (anim) { anim.enabled = true; anim.applyRootMotion = false; }

        rb = GetComponent<Rigidbody>();

        GameObject go = GameObject.FindGameObjectWithTag(targetTag);
        if (go) target = go.transform;

        // empezamos en idle
        SetSpeedParam(0f, true);
    }

    void Update()
    {
        // si está atacando, no mover
        if (isAttacking)
        {
            if (Time.time >= attackEndTime) isAttacking = false;
            else
            {
                desiredVelocity = Vector3.zero;
                SetSpeedParam(0f); // idle mientras dura el ataque
                return;
            }
        }

        // buscar target si se perdió
        if (target == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        float d = Vector3.Distance(transform.position, target.position);

        // fuera de rango -> quieto
        if (d > detectionRange)
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        // girar hacia el jugador (suave)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // ¿debemos avanzar?
        float distToStop = d - stoppingDistance;

        if (distToStop > 0.05f)
        {
            // más lejos → corre; cerca → camina
            bool shouldRun = distToStop > 3f;
            float speed = shouldRun ? runSpeed : moveSpeed;

            desiredVelocity = transform.forward * speed;

            // enviar Speed al Animator suavizado
            SetSpeedParam(speed);
        }
        else
        {
            // hemos llegado a stoppingDistance -> nos quedamos quietos
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);

            // si además estamos dentro del rango de ataque, atacamos
            if (d <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                RealizarAtaque();
            }
        }
    }

    void FixedUpdate()
    {
        // Aplicar movimiento de forma estable
        if (rb != null && !rb.isKinematic)
        {
            // Parche anti “overshoot”: si no queremos movernos, frenamos del todo
            if (desiredVelocity.sqrMagnitude < 0.0001f)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                return;
            }

            rb.MovePosition(rb.position + desiredVelocity * Time.fixedDeltaTime);
            rb.MoveRotation(transform.rotation);
        }
        else
        {
            if (desiredVelocity.sqrMagnitude > 0.000001f)
                transform.position += desiredVelocity * Time.fixedDeltaTime;
        }
    }

    void SetSpeedParam(float targetSpeed, bool instant = false)
    {
        if (anim == null) return;

        // Convertimos a "unidades de blend" (tu Blend Tree usa ~0, 1.5 y 4-5)
        float blendSpeed = targetSpeed; // ya usamos las mismas unidades (m/s)

        if (instant)
            speedAnim = blendSpeed;
        else
            speedAnim = Mathf.SmoothDamp(speedAnim, blendSpeed, ref speedAnimVelRef, speedAnimSmooth);

        anim.SetFloat(pSpeed, speedAnim);
    }

    void RealizarAtaque()
    {
        isAttacking = true;
        attackEndTime = Time.time + attackStopDuration;
        desiredVelocity = Vector3.zero;

        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (anim) anim.SetTrigger(pAttack);

        // Daño (puedes moverlo a un Animation Event si prefieres)
        if (targetLayers.value != 0)
        {
            foreach (var c in Physics.OverlapSphere(transform.position, attackRange, targetLayers))
            {
                var vd = c.GetComponentInParent<VidaDañoPlayer>();
                if (vd != null) vd.RecibirDaño(dañoMelee, gameObject);
            }
        }
        else
        {
            var vdDirect = target.GetComponentInParent<VidaDañoPlayer>();
            if (vdDirect != null) vdDirect.RecibirDaño(dañoMelee, gameObject);
        }
    }

    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f) return;
        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0f, vidaMaxima);
        if (vidaActual <= 0f) Morir(atacante);
    }

    public void Curar(float cantidad)
    {
        if (cantidad <= 0f) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
    }

    void Morir(GameObject atacante)
    {
        OnDeath?.Invoke();
        if (destruirAlMorir) Destroy(gameObject);
        else enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
