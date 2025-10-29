using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DañoVidaEnemigo : MonoBehaviour
{
    // ------------ Vida ------------
    [Header("Vida")]
    public float vidaMaxima = 50f;
    public float vidaActual = 50f;
    public bool destruirAlMorir = true;
    public float tiempoDesaparecerAlMorir = 4f;

    // ------------ Detección / movimiento ------------
    [Header("Detección y movimiento")]
    public string targetTag = "Player";
    public float detectionRange = 12f;
    public float moveSpeed = 2.0f;    // caminar
    public float runSpeed = 4.0f;     // correr
    public float stoppingDistance = 1.2f;
    public float rotationSpeed = 8f;

    // ------------ Ataque ------------
    [Header("Ataque cuerpo a cuerpo")]
    public float dañoMelee = 15f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public LayerMask targetLayers;
    [Tooltip("Tiempo que el enemigo permanece parado cuando realiza el ataque")]
    public float attackStopDuration = 0.6f;

    // ------------ Animator ------------
    [Header("Animator")]
    public Animator anim;
    public string pSpeed = "Speed";
    public string pAttack = "Attack";
    public string pAttackIndex = "AttackIndex";
    public string pScream = "Scream";
    public string pDead = "Dead";
    [Tooltip("Cuántas variantes de ataque hay (0..N-1)")]
    public int attackVariants = 4;

    // ------------ Eventos ------------
    public event Action OnDeath;

    // ------------ Internos ------------
    private Transform target;
    private float lastAttackTime = -999f;
    private bool isAttacking = false;
    private float attackEndTime = 0f;

    private Rigidbody rb;
    private Vector3 desiredVelocity = Vector3.zero;

    // Suavizado del Blend Tree
    private float speedAnim;
    private float speedAnimVelRef;
    public float speedAnimSmooth = 0.08f;

    private bool hasScreamed = false;

    void Start()
    {
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (anim) { anim.enabled = true; anim.applyRootMotion = false; }

        rb = GetComponent<Rigidbody>();

        // ⚙️ Configuración segura del Rigidbody
        if (rb)
        {
            rb.isKinematic = false;                   // lo movemos por física
            rb.useGravity = false;                    // no queremos que se caiga
            rb.constraints = RigidbodyConstraints.FreezeRotation; // solo rotación bloqueada
        }

        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go) target = go.transform;

        SetSpeedParam(0f, true);
    }

    void Update()
    {
        // Bloqueo durante la animación de ataque
        if (isAttacking)
        {
            if (Time.time >= attackEndTime) isAttacking = false;
            else
            {
                desiredVelocity = Vector3.zero;
                SetSpeedParam(0f);
                return;
            }
        }

        // Buscar o re-adquirir al jugador
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;

            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        float d = Vector3.Distance(transform.position, target.position);

        // Fuera de rango → quieto
        if (d > detectionRange)
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            hasScreamed = false;
            return;
        }

        // Primera detección → grito
        if (!hasScreamed && anim && HasParameter(anim, pScream))
        {
            hasScreamed = true;
            anim.SetTrigger(pScream);
        }

        // Rotación suave hacia el jugador
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion q = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
        }

        float distToStop = d - stoppingDistance;

        if (distToStop > 0.05f)
        {
            // Corre si está lejos, camina si está cerca
            bool shouldRun = distToStop > 3f;
            float speed = shouldRun ? runSpeed : moveSpeed;

            desiredVelocity = transform.forward * speed;
            SetSpeedParam(speed);
        }
        else
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);

            // Si está en rango → atacar
            if (d <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                DoAttack();
            }
        }
    }

    void FixedUpdate()
    {
        // Si no hay intención de moverse → parar
        if (desiredVelocity.sqrMagnitude < 0.0001f)
        {
            if (rb && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

        // Movimiento físico si hay Rigidbody
        if (rb && !rb.isKinematic)
        {
            rb.MovePosition(rb.position + desiredVelocity * Time.fixedDeltaTime);
            rb.MoveRotation(transform.rotation);
        }
        else
        {
            transform.position += desiredVelocity * Time.fixedDeltaTime;
        }
    }

    // ---------- Ataque ----------
 void DoAttack()
{
    isAttacking = true;
    attackEndTime = Time.time + attackStopDuration;
    desiredVelocity = Vector3.zero;

    if (rb && !rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

    if (anim)
    {
        anim.CrossFadeInFixedTime("Ataque", 0.1f, 0); // nombre EXACTO del estado
    }

    TryDealDamage();
}


    // Método para Animation Event
    public void OnAttackHitEvent()
    {
        TryDealDamage();
    }

    void TryDealDamage()
    {
        if (targetLayers.value != 0)
        {
            foreach (var c in Physics.OverlapSphere(transform.position + transform.forward * (attackRange * 0.5f), attackRange, targetLayers))
            {
                var vd = c.GetComponentInParent<VidaDañoPlayer>();
                if (vd != null) vd.RecibirDaño(dañoMelee, gameObject);
            }
        }
        else if (target && Vector3.Distance(transform.position, target.position) <= attackRange + 0.2f)
        {
            var vd = target.GetComponentInParent<VidaDañoPlayer>();
            if (vd != null) vd.RecibirDaño(dañoMelee, gameObject);
        }
    }

    // ---------- Vida ----------
    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f || IsDead()) return;

        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0f, vidaMaxima);
        if (vidaActual <= 0f) Morir(atacante);
    }

    public void Curar(float cantidad)
    {
        if (cantidad <= 0f || IsDead()) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
    }

    bool IsDead() => vidaActual <= 0f;

    void Morir(GameObject atacante)
    {
        if (IsDead()) return;

        vidaActual = 0f;
        OnDeath?.Invoke();

        enabled = false;
        desiredVelocity = Vector3.zero;

        if (anim && HasParameter(anim, pDead)) anim.SetBool(pDead, true);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (destruirAlMorir)
            Destroy(gameObject, tiempoDesaparecerAlMorir);
    }

    // ---------- Utilidades ----------
    void SetSpeedParam(float targetSpeed, bool instant = false)
    {
        if (!anim || !HasParameter(anim, pSpeed)) return;

        float blendSpeed = targetSpeed;

        if (instant)
            speedAnim = blendSpeed;
        else
            speedAnim = Mathf.SmoothDamp(speedAnim, blendSpeed, ref speedAnimVelRef, speedAnimSmooth);

        anim.SetFloat(pSpeed, speedAnim);
    }

    bool HasParameter(Animator a, string name)
    {
        if (a == null || string.IsNullOrEmpty(name)) return false;
        foreach (var p in a.parameters)
            if (p.name == name) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(center, attackRange);
    }
}
