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
    public Transform origenPuño; // Normalmente la cámara o un transform en la posición de la mano

    [Header("Opciones")]
    public bool destruirAlMorir = false; // si true destruye el GameObject al morir

    public event Action OnDeath; // suscribible desde otros scripts si se quiere

    private float ultimoGolpe = -999f;

    void Start()
    {
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);
        if (origenPuño == null && Camera.main != null)
            origenPuño = Camera.main.transform;
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
        Vector3 dir = origenPuño != null ? origenPuño.forward : transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origen, dir, out hit, alcancePuño, capasObjetivo))
        {
            // Intentar aplicar daño a un componente VidaDañoPlayer en el objeto golpeado o en sus padres
            VidaDañoPlayer vd = hit.collider.GetComponentInParent<VidaDañoPlayer>();
            if (vd != null)
            {
                vd.RecibirDaño(dañoPuño, gameObject);
            }
            else
            {
                // Si el objetivo no tiene VidaDañoPlayer pero quieres soportar otras interfaces,
                // puedes comprobar aquí y llamar a métodos alternativos.
            }
        }
    }

    // Método público para recibir daño
    public void RecibirDaño(float cantidad, GameObject atacante = null)
    {
        if (cantidad <= 0f) return;

        vidaActual -= cantidad;
        vidaActual = Mathf.Clamp(vidaActual, 0f, vidaMaxima);

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