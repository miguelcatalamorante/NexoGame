using System;
using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 50f;
    public float vidaActual = 50f;
    public bool destruirAlMorir = true;

    [Header("Detección y movimiento")]
    public string targetTag = "Player";
    public float detectionRange = 10f;
    public float moveSpeed = 3f;
    public float stoppingDistance = 1.2f; // distancia a la que deja de acercarse para atacar

    [Header("Ataque cuerpo a cuerpo")]
    public float dañoMelee = 15f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.0f;
    public LayerMask targetLayers; // opcional: capas a considerar como objetivos

    public event Action OnDeath;

    private Transform target;
    private float lastAttackTime = -999f;

    void Start()
    {
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);
        // intenta asignar target por tag
        GameObject go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null) target = go.transform;
    }

    void Update()
    {
        if (target == null)
        {
            // intenta buscar cada frame si no hay target asignado (útil en escenas pequeñas)
            GameObject go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) target = go.transform;
            return;
        }

        float d = Vector3.Distance(transform.position, target.position);
        if (d <= detectionRange)
        {
            //Mover hacia el target si está lejos del stoppingDistance
            if (d > stoppingDistance)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                dir.y = 0f;
                transform.forward = Vector3.Lerp(transform.forward, dir, 10f * Time.deltaTime);
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
            }

            // Intentar atacar si en rango de ataque
            if (d <= attackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                RealizarAtaque();
            }
        }
    }

    void RealizarAtaque()
    {
        // comprueba si el objetivo tiene componente de vida
        if (target == null) return;

        // opcional: usa OverlapSphere para permitir golpear varios objetivos cercanos
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, targetLayers);
        bool hitPlayer = false;

        if (hits.Length == 0)
        {
            // si no hay targetLayers configuradas, intenta dañar directamente al target por tag
            VidaDañoPlayer vdDirect = target.GetComponentInParent<VidaDañoPlayer>();
            if (vdDirect != null)
            {
                vdDirect.RecibirDaño(dañoMelee, gameObject);
                hitPlayer = true;
            }
        }
        else
        {
            foreach (var c in hits)
            {
                VidaDañoPlayer vd = c.GetComponentInParent<VidaDañoPlayer>();
                if (vd != null)
                {
                    vd.RecibirDaño(dañoMelee, gameObject);
                    hitPlayer = true;
                }
            }
        }

        // aquí puedes reproducir animación/sonido si quieres
        if (hitPlayer)
        {
            // efecto opcional
        }
    }

    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f) return;
        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

        if (vidaActual <= 0f)
        {
            Morir(atacante);
        }
    }

    public void Curar(float cantidad)
    {
        if (cantidad <= 0f) return;
        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);
    }

    void Morir(GameObject atacante)
    {
        OnDeath?.Invoke();
        if (destruirAlMorir)
            Destroy(gameObject);
        else
            enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}