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
    public float moveSpeed = 2.0f;
    public float runSpeed = 4.0f;
    public float stoppingDistance = 1.2f;
    public float rotationSpeed = 8f;

    // ------------ Ataque ------------
    [Header("Ataque cuerpo a cuerpo")]
    public float dañoMelee = 15f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f; // (opcional, ya no es imprescindible)
    public LayerMask targetLayers;
    [Tooltip("Tiempo parado durante el golpe (debe cubrir hasta justo después del impacto)")]
    public float attackStopDuration = 0.6f;

    [Header("Ritmo del ataque")]
    [Tooltip("Duración total aproximada de la animación de ataque (fallback si no hay evento de fin)")]
    public float attackAnimDuration = 0.9f;
    [Tooltip("Pausa tras el ataque antes de poder iniciar otro")]
    public float attackRecoverTime = 0.25f;

    [Header("Hitbox (origen del golpe)")]
    public Transform hitOrigin;      // hijo de la mano
    public float hitRadius = 0.35f;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool debugDrawHit = true;

    // ------------ Animator ------------
    [Header("Animator")]
    public Animator anim;
    public string pSpeed = "Speed";
    public string pScream = "Scream";
    public string pDead = "Dead";

    // ------------ Eventos ------------
    public event Action OnDeath;

    // ------------ Internos ------------
    private Transform target;

    private bool isAttacking = false;
    private float attackEndTime = 0f;          // fin de la “inmovilización” (parón)
    private float attackStartTime = 0f;        // para el fallback de fin de anim
    private float nextAttackAllowedTime = 0f;  // control del ritmo (anim + recuperación)

    private Rigidbody rb;
    private Vector3 desiredVelocity = Vector3.zero;

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
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go) target = go.transform;

        SetSpeedParam(0f, true);
    }

    void Update()
    {
        // 1) Fase de ataque: inmovilización durante la ventana del golpe
        if (isAttacking)
        {
            // Parado mientras dura el parón
            if (Time.time < attackEndTime)
            {
                desiredVelocity = Vector3.zero;
                SetSpeedParam(0f);
                return;
            }

            // ⚠️ Fallback: si NO llega OnAttackEndEvent, cortamos el estado
            // cuando haya pasado attackAnimDuration desde que empezó el ataque.
            if (Time.time >= attackStartTime + attackAnimDuration)
            {
                ForceEndAttack(); // desbloquea y garantiza recuperación
            }
        }

        // 2) Buscar target si se perdió
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go) target = go.transform;

            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        float d = Vector3.Distance(transform.position, target.position);

        // 3) Fuera de rango → quieto
        if (d > detectionRange)
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            hasScreamed = false;
            return;
        }

        // 4) Grito al detectar
        if (!hasScreamed && anim && HasParameter(anim, pScream))
        {
            hasScreamed = true;
            anim.SetTrigger(pScream);
        }

        // 5) Rotación suave hacia el jugador
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion q = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
        }

        float distToStop = d - stoppingDistance;

        // 6) Moverse o parar
        if (distToStop > 0.05f)
        {
            bool shouldRun = distToStop > 3f;
            float speed = shouldRun ? runSpeed : moveSpeed;

            desiredVelocity = transform.forward * speed;
            SetSpeedParam(speed);
        }
        else
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);

            // 7) Intentar atacar (solo si no está en ataque y pasó la ventana de ritmo)
            if (!isAttacking && Time.time >= nextAttackAllowedTime && d <= attackRange)
            {
                DoAttack();
            }
        }
    }

    void FixedUpdate()
    {
        if (desiredVelocity.sqrMagnitude < 0.0001f)
        {
            if (rb && !rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            return;
        }

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
        attackStartTime = Time.time;
        attackEndTime = Time.time + attackStopDuration; // parón (hasta justo después del impacto)

        desiredVelocity = Vector3.zero;
        if (rb && !rb.isKinematic) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        if (anim)
        {
            // Asegúrate de que el estado se llama EXACTAMENTE "Ataque"
            anim.CrossFadeInFixedTime("Ataque", 0.1f, 0);
        }

        // Bloqueamos el siguiente ataque hasta fin de anim + recuperación
        nextAttackAllowedTime = Time.time + attackAnimDuration + attackRecoverTime;

        if (debugLogs) Debug.Log("[Enemigo] Comienza ataque (Start=" + attackStartTime.ToString("F2") + ")");
    }

    // Evento de impacto (Animation Event en el frame de contacto)
    public void OnAttackHitEvent()
    {
        if (debugLogs) Debug.Log("[Enemigo] OnAttackHitEvent");
        TryDealDamage();
    }

    // Evento final (Animation Event al acabar el clip)
    public void OnAttackEndEvent()
    {
        if (debugLogs) Debug.Log("[Enemigo] OnAttackEndEvent");
        ForceEndAttack();
    }

    // Fallback / final común del ataque
    private void ForceEndAttack()
    {
        isAttacking = false;

        // Garantiza al menos la recuperación configurada
        float minNext = Time.time + attackRecoverTime;
        if (minNext > nextAttackAllowedTime)
            nextAttackAllowedTime = minNext;
    }

    void TryDealDamage()
    {
        Vector3 center;
        float radius;

        if (hitOrigin)
        {
            center = hitOrigin.position;
            radius = Mathf.Max(0.01f, hitRadius);
        }
        else
        {
            center = transform.position + transform.forward * (attackRange * 0.5f);
            radius = attackRange;
        }

        int hits = 0;

        if (targetLayers.value != 0)
        {
            var cols = Physics.OverlapSphere(center, radius, targetLayers, QueryTriggerInteraction.Collide);
            foreach (var c in cols)
            {
                var vd = c.GetComponentInParent<VidaDañoPlayer>();
                if (vd != null)
                {
                    vd.RecibirDaño(dañoMelee, gameObject);
                    hits++;
                }
            }
        }

        if (hits == 0 && target)
        {
            float dist = Vector3.Distance(center, target.position);
            if (dist <= radius + 0.2f)
            {
                var vd = target.GetComponentInParent<VidaDañoPlayer>();
                if (vd != null)
                {
                    vd.RecibirDaño(dañoMelee, gameObject);
                    hits++;
                }
            }
        }

        if (debugLogs) Debug.Log($"[Enemigo] Impacto evaluado. hits={hits}");
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

        if (hitOrigin)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(hitOrigin.position, Mathf.Max(0.01f, hitRadius));
        }
    }

    void OnDrawGizmos()
    {
        if (!debugDrawHit || !hitOrigin) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawSphere(hitOrigin.position, Mathf.Max(0.01f, hitRadius));
    }
}
