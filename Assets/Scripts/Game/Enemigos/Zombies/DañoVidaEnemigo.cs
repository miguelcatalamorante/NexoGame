using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DañoVidaEnemigo : MonoBehaviour
{
    // ------------ Vida ------------
    [Header("Vida")]
    [Tooltip("Vida máxima del enemigo.")]
    public float vidaMaxima = 50f;

    [Tooltip("Vida inicial (se clampa a vidaMaxima al iniciar).")]
    public float vidaActual = 50f;

    [Tooltip("Si está activado, destruye el objeto al morir.")]
    public bool destruirAlMorir = true;

    [Tooltip("Tiempo que tarda en desaparecer después de morir.")]
    public float tiempoDesaparecerAlMorir = 4f;

    // Control interno de muerte
    private bool muerto = false;

    // ------------ Detección / movimiento ------------
    [Header("Detección y movimiento")]
    [Tooltip("Tag del objetivo al que va a perseguir (normalmente el Player).")]
    public string targetTag = "Player";

    [Tooltip("Solo para gizmo visual, no se usa en la lógica.")]
    public float detectionRange = 12f;

    [Tooltip("Velocidad de caminar.")]
    public float moveSpeed = 2.0f;

    [Tooltip("Velocidad de correr.")]
    public float runSpeed = 4.0f;

    [Tooltip("Distancia mínima a la que se planta y deja de acercarse.")]
    public float stoppingDistance = 1.2f;

    [Tooltip("Velocidad de giro hacia el objetivo.")]
    public float rotationSpeed = 8f;

    // ------------ Ataque ------------
    [Header("Ataque cuerpo a cuerpo")]
    [Tooltip("Daño que mete cada golpe.")]
    public float dañoMelee = 15f;

    [Tooltip("Distancia a la que puede empezar a atacar.")]
    public float attackRange = 1.5f;

    [Tooltip("Cooldown global del ataque (no se está usando directamente, pero lo dejo por si acaso).")]
    public float attackCooldown = 1.0f;

    [Tooltip("Capas que puede golpear (jugador, etc).")]
    public LayerMask targetLayers;

    [Tooltip("Tiempo que se queda quieto durante el ataque (parón inicial).")]
    public float attackStopDuration = 0.6f;

    [Header("Ritmo del ataque")]
    [Tooltip("Duración total aproximada de la animación de ataque (fallback si no llega el evento de fin).")]
    public float attackAnimDuration = 0.9f;

    [Tooltip("Pausa después del ataque antes de poder iniciar otro.")]
    public float attackRecoverTime = 0.25f;

    [Header("Hitbox (origen del golpe)")]
    [Tooltip("Punto desde donde se calcula el OverlapSphere del golpe.")]
    public Transform hitOrigin;

    [Tooltip("Radio de la esfera de golpe respecto a hitOrigin.")]
    public float hitRadius = 0.35f;

    [Header("Debug")]
    public bool debugLogs = false;
    public bool debugDrawHit = true;

    // Animator
    [Header("Animator")]
    public Animator anim;
    public string pSpeed = "Speed";
    public string pScream = "Scream"; // ahora mismo no lo uso, pero lo dejo por si lo necesito luego
    public string pDead = "Dead";
    public string pAttack = "Attack";

    // Eventos
    public event Action OnDeath;

    // Internos
    private Transform target;

    private bool isAttacking = false;
    private float attackEndTime = 0f;
    private float attackStartTime = 0f;
    private float nextAttackAllowedTime = 0f;

    private Rigidbody rb;
    private Vector3 desiredVelocity = Vector3.zero;   // solo plano XZ

    // Suavizado de la velocidad en el animator
    private float speedAnim;
    private float speedAnimVelRef;
    public float speedAnimSmooth = 0.08f;

    void Start()
    {
        // Aseguro que la vida inicial esté dentro de rango
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);
        muerto = vidaActual <= 0f;

        // Animator: intento coger el de este objeto o de hijos
        if (!anim) anim = GetComponent<Animator>();
        if (!anim) anim = GetComponentInChildren<Animator>();

        if (anim)
        {
            anim.enabled = true;
            anim.applyRootMotion = false; // muevo yo por código, no por root motion
        }

        // Rigidbody para que la física se encargue de la Y
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        BuscarTarget();
        SetSpeedParam(0f, true);
    }

    void Update()
    {
        if (IsDead())
        {
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        // Si está en fase de ataque y todavía dura el parón, no se mueve
        if (isAttacking)
        {
            if (Time.time < attackEndTime)
            {
                desiredVelocity = Vector3.zero;
                SetSpeedParam(0f);
                return;
            }

            // Si por lo que sea no llega el evento de fin, hago un corte de seguridad
            if (Time.time >= attackStartTime + attackAnimDuration)
            {
                ForceEndAttack();
            }
        }

        // Si por lo que sea se perdió el target, intento buscarlo otra vez
        if (!target)
        {
            BuscarTarget();
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);
            return;
        }

        float distancia = Vector3.Distance(transform.position, target.position);

        // Rotación suave hacia el jugador (solo en Y, no inclino el modelo)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion q = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
        }

        float distToStop = distancia - stoppingDistance;

        // Moverse o parar
        if (distToStop > 0.05f)
        {
            // Si está muy lejos, corre, si no, camina
            bool shouldRun = distToStop > 3f;
            float speed = shouldRun ? runSpeed : moveSpeed;

            Vector3 forwardFlat = transform.forward;
            forwardFlat.y = 0f;
            forwardFlat.Normalize();
            desiredVelocity = forwardFlat * speed;

            SetSpeedParam(speed);
        }
        else
        {
            // Está ya a distancia de parada
            desiredVelocity = Vector3.zero;
            SetSpeedParam(0f);

            // Y si está a rango de ataque y toca por tiempo, ataca
            if (!isAttacking && Time.time >= nextAttackAllowedTime && distancia <= attackRange)
            {
                DoAttack();
            }
        }
    }

    void FixedUpdate()
    {
        if (!rb || rb.isKinematic)
        {
            // Fallback sin rigidbody controlando XZ
            Vector3 delta = desiredVelocity * Time.fixedDeltaTime;
            transform.position += new Vector3(delta.x, 0f, delta.z);
            return;
        }

        // Mantengo la Y de la física y solo piso XZ
        Vector3 vel = rb.velocity;
        vel.x = desiredVelocity.x;
        vel.z = desiredVelocity.z;
        rb.velocity = vel;
    }

    // ------------ Lógica de ataque ------------
    void DoAttack()
    {
        if (IsDead()) return;

        isAttacking = true;
        attackStartTime = Time.time;
        attackEndTime = Time.time + attackStopDuration;

        desiredVelocity = Vector3.zero;

        if (rb && !rb.isKinematic)
        {
            // Paro la velocidad horizontal, dejo la Y en manos de la gravedad
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }

        // Lanzo la animación de ataque (nombre en el Animator)
        if (anim)
        {
            anim.CrossFadeInFixedTime("Ataque", 0.1f, 0);
        }

        // No se podrá volver a atacar hasta que pase toda la anim + recover
        nextAttackAllowedTime = Time.time + attackAnimDuration + attackRecoverTime;

        if (debugLogs)
            Debug.Log($"[Enemigo] Comienza ataque (Start={attackStartTime:F2})");
    }

    // Evento de animación para el frame del impacto
    public void OnAttackHitEvent()
    {
        if (debugLogs) Debug.Log("[Enemigo] OnAttackHitEvent");
        TryDealDamage();
    }

    // Evento de animación para marcar el final del ataque
    public void OnAttackEndEvent()
    {
        if (debugLogs) Debug.Log("[Enemigo] OnAttackEndEvent");
        ForceEndAttack();
    }

    // Corto la fase de ataque y ajusto el siguiente tiempo permitido
    private void ForceEndAttack()
    {
        isAttacking = false;

        float minNext = Time.time + attackRecoverTime;
        if (minNext > nextAttackAllowedTime)
            nextAttackAllowedTime = minNext;
    }

    // Hitbox / daño real del golpe
    void TryDealDamage()
    {
        if (IsDead()) return;

        Vector3 center;
        float radius;

        // Si tengo un origen definido, uso ese, si no, calculo uno delante del enemigo
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

        // Primero pruebo con las capas configuradas
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

        // Fallback: por si el jugador no entra en la capa o el collider
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

        if (debugLogs)
            Debug.Log($"[Enemigo] Impacto evaluado. hits={hits}");
    }

    //  Vida / daño 
    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f || IsDead()) return;

        vidaActual = Mathf.Clamp(vidaActual - cantidad, 0f, vidaMaxima);
        if (vidaActual <= 0f)
            Morir(atacante);
    }

    public void Curar(float cantidad)
    {
        if (cantidad <= 0f || IsDead()) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
    }

    public bool IsDead() => muerto;

    void Morir(GameObject atacante)
    {
        if (muerto) return;
        muerto = true;

        vidaActual = 0f;
        OnDeath?.Invoke();

        // Desactivo lógica de Update y movimiento
        enabled = false;
        desiredVelocity = Vector3.zero;

        if (anim && HasParameter(anim, pDead))
            anim.SetBool(pDead, true);

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        if (destruirAlMorir)
            Destroy(gameObject, tiempoDesaparecerAlMorir);
    }

    //  Utilidades internas 
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

    void BuscarTarget()
    {
        if (target) return;

        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go) target = go.transform;
    }

    //  Gizmos 
    void OnDrawGizmosSelected()
    {
        // Rango de detección general
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Rango de ataque aproximado
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * (attackRange * 0.5f);
        Gizmos.DrawWireSphere(center, attackRange);

        // Hitbox real si hay hitOrigin
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

    // Evento de animación para realizar el ataque (si no uso OnAttackHitEvent)
    void RealizarAtaque()
    {
        if (IsDead() || !target)
            return;

        if (debugLogs)
        {
            float distancia = Vector3.Distance(transform.position, target.position);
            Debug.Log($"[DañoVidaEnemigo] {name} RealizarAtaque (evento anim). Distancia={distancia:F2}");
        }

        TryDealDamage();
    }

    // Por si quiero escalar el daño en runtime (rondas, dificultad, etc.)
    public void MultiplyDamage(float factor)
    {
        dañoMelee *= factor;
    }
}
