using System;
using System.Collections;
using UnityEngine;

public class ArmaDeFuego : MonoBehaviour
{
    [Header("Datos del arma")]
    public string nombreArma = "Arma";
    public float dañoPorBala = 20f;
    public int tamañoCargador = 30;
    public int balasEnCargador = 30;
    public int balasEnReserva = 90;
    public float cadenciaDisparo = 0.1f; // tiempo entre disparos
    public float tiempoRecarga = 2f;

    [Header("Punto de disparo / bala física")]
    public Transform puntoDisparo;        // dónde aparece la bala
    public GameObject prefabBala;         // prefab de la bala física

    [Header("Pickup al soltar")]
    [Tooltip("Prefab del arma en el suelo que se instancia cuando el jugador suelta esta arma.")]
    public GameObject prefabPickupSuelo;

    [Header("Sonido")]
    public AudioSource audioSource; // Nuevo: El componente AudioSource
    public AudioClip clipDisparo;   // Nuevo: Sonido al disparar
    public AudioClip clipRecarga;   // Opcional: Sonido al iniciar la recarga
    public AudioClip clipVacio;     // Opcional: Sonido de 'click' cuando está vacío

    // Eventos para la UI
    public event Action<int, int, int> OnAmmoChanged; // balasEnCargador, tamañoCargador, balasEnReserva
    public event Action<float> OnReloadProgress;      // 0–1, progreso de recarga

    private float siguienteTiempoDisparo = 0f;
    private bool recargando = false;

    // NUEVO: solo las armas equipadas responden al Input
    private bool isEquipped = false;

    void Start()
    {
        // Si no se ha asignado un AudioSource, intentamos encontrar uno en el objeto
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning($"El arma {nombreArma} no tiene un AudioSource asignado ni lo pudo encontrar.");
            }
        }

        // Notificar estado inicial a la UI
        OnAmmoChanged?.Invoke(balasEnCargador, tamañoCargador, balasEnReserva);
        OnReloadProgress?.Invoke(0f);
    }

    void Update()
    {
        // Si no estamos equipados (por ejemplo estamos como prefab en el suelo), no procesamos Input
        if (!isEquipped) return;

        // Solo el arma activa puede disparar
        if (!gameObject.activeInHierarchy)
            return;

        if (recargando)
            return;

        // DISPARAR (click izquierdo / botón Fire1)
        if (Input.GetButton("Fire1") && Time.time >= siguienteTiempoDisparo)
        {
            if (balasEnCargador > 0)
            {
                siguienteTiempoDisparo = Time.time + cadenciaDisparo;
                Disparar();
            }
            else
            {
                // Si no quedan balas en el cargador pero hay en reserva, intentamos recargar
                if (balasEnReserva > 0)
                {
                    StartCoroutine(Recargar());
                }
                else
                {
                    // Nuevo: Sonido de 'click' si no hay balas ni en cargador ni en reserva
                    ReproducirSonido(clipVacio); 
                }
            }
        }

        // RECARGA MANUAL (tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (balasEnCargador < tamañoCargador && balasEnReserva > 0)
            {
                StartCoroutine(Recargar());
            }
        }
    }

    // Nuevo: Método unificado para reproducir cualquier sonido
    void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void Disparar()
    {
        balasEnCargador--;

        // Nuevo: Reproducir el sonido de disparo
        ReproducirSonido(clipDisparo);

        // Instanciar bala física
        if (prefabBala != null && puntoDisparo != null)
        {
            GameObject balaGO = Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);

            Bala balaScript = balaGO.GetComponent<Bala>();
            if (balaScript != null)
            {
                // El arma decide el daño de la bala
                balaScript.daño = dañoPorBala;
            }
        }

        // Avisar a la UI de la nueva munición
        OnAmmoChanged?.Invoke(balasEnCargador, tamañoCargador, balasEnReserva);
    }

    IEnumerator Recargar()
    {
        recargando = true;
        
        ReproducirSonido(clipRecarga); // Nuevo: Sonido al iniciar la recarga

        // Barra de recarga a 0 al empezar
        OnReloadProgress?.Invoke(0f);

        float t = 0f;
        while (t < tiempoRecarga)
        {
            t += Time.deltaTime;
            float progreso = Mathf.Clamp01(t / tiempoRecarga);
            OnReloadProgress?.Invoke(progreso);
            yield return null;
        }

        // Recarga efectiva
        int necesarias = tamañoCargador - balasEnCargador;
        int aCargar = Mathf.Min(necesarias, balasEnReserva);

        balasEnReserva -= aCargar;
        balasEnCargador += aCargar;

        recargando = false;

        // Notificamos a la UI
        OnAmmoChanged?.Invoke(balasEnCargador, tamañoCargador, balasEnReserva);
        OnReloadProgress?.Invoke(1f);
    }

    //controlar equipar / desequipar desde el inventario
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
    }

    public bool IsEquipped() => isEquipped;
}