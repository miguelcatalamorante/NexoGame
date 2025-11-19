using System;
using UnityEngine;

public class VidaDañoPlayer : MonoBehaviour
{
    [Header("Vida")]
    public float vidaMaxima = 100f;
    public float vidaActual = 100f;

    [Header("Daño de Puñetazo")]
    public float dañoPuño = 10f;
    public float alcancePuño = 1.5f;
    public float tiempoRecargaPuño = 0.5f;
    public LayerMask capasObjetivo; // Asigna aquí las capas "Enemies", "Animals", "Players" en el Inspector
    public Transform origenPuño;    // Normalmente la cámara o un transform en la posición de la mano

    [Header("Opciones")]
    public bool destruirAlMorir = false; // si true destruye el GameObject al morir

    public event Action OnDeath; // suscribible desde otros scripts si se quiere
    public event Action<float, float> OnHealthChanged; // (vidaActual, vidaMaxima)

    private float ultimoGolpe = -999f;

    void Start()
    {
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);
        if (origenPuño == null && Camera.main != null)
            origenPuño = Camera.main.transform;

        // Notificar vida inicial a la UI
        OnHealthChanged?.Invoke(vidaActual, vidaMaxima);
    }

    void Update()
    {
        // Puñetazo con click izquierdo del ratón
        if (Input.GetMouseButtonDown(0))
        {
            IntentarPuñetazo();
        }
    }

    void IntentarPuñetazo()
    {
        if (Time.time - ultimoGolpe < tiempoRecargaPuño) return;
        ultimoGolpe = Time.time;

        Vector3 origen = origenPuño != null ? origenPuño.position : transform.position;
        Vector3 dir    = origenPuño != null ? origenPuño.forward  : transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origen, dir, out hit, alcancePuño, capasObjetivo))
        {
            // ✅ Dañar a otro jugador con VidaDañoPlayer
            VidaDañoPlayer vd = hit.collider.GetComponentInParent<VidaDañoPlayer>();
            if (vd != null)
            {
                vd.RecibirDaño(dañoPuño, gameObject);
            }

            // ✅ Dañar a un enemigo con DañoVidaEnemigo (si existe ese script)
            DañoVidaEnemigo de = hit.collider.GetComponentInParent<DañoVidaEnemigo>();
            if (de != null)
            {
                de.RecibirDaño(dañoPuño, gameObject);
            }
        }
    }

    // Método público para recibir daño
    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

        // Notificar a la UI
        OnHealthChanged?.Invoke(vidaActual, vidaMaxima);

        // Aquí puedes reproducir efectos, animaciones, feedback, etc.

        if (vidaActual <= 0f)
        {
            Morir(atacante);
        }
    }

    // Curar
    public void Curar(float cantidad)
    {
        if (cantidad <= 0f) return;

        vidaActual = Mathf.Clamp(vidaActual + cantidad, 0f, vidaMaxima);

        // Notificar a la UI
        OnHealthChanged?.Invoke(vidaActual, vidaMaxima);
    }

    void Morir(GameObject atacante)
    {
        OnDeath?.Invoke();
        if (destruirAlMorir)
            Destroy(gameObject);
        else
            enabled = false;
    }

    // Visualizar alcance en editor
    void OnDrawGizmosSelected()
    {
        if (origenPuño == null && Camera.main != null)
            origenPuño = Camera.main.transform;

        if (origenPuño != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origenPuño.position, origenPuño.position + origenPuño.forward * alcancePuño);
            Gizmos.DrawWireSphere(origenPuño.position + origenPuño.forward * alcancePuño, 0.05f);
        }
    }
}
