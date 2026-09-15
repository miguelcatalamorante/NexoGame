using System;
using System.Collections;
using UnityEngine;

public class GestorRondas : MonoBehaviour
{
    [Header("Configuración rondas")]
    [Tooltip("Número actual de ronda (comienza en 0, StartNextRound incrementa)")]
    public int rondaActual = 0;
    public int enemigosBasePorRonda = 5;
    public int incrementoEnemigosPorRonda = 2;
    public float tiempoEntreRondas = 12f;     // intermedio entre rondas (segundos)
    public float tiempoEntreSpawns = 0.6f;     // segundos entre spawns
    public float multiplicadorVidaPorRonda = 1.12f;
    public float multiplicadorDañoPorRonda = 1.05f;

    [Header("Spawning")]
    public Transform[] puntosSpawn;          
    public GameObject[] prefabsEnemigos;       // prefabs de enemigos a spawnear

    [Header("Opciones")]
    public bool autoIniciarPrimeraRonda = true;

    // --- NUEVO: Audio ---
    [Header("Audio de Ronda")]
    public AudioSource audioSource;
    [Tooltip("Sonido al iniciar una nueva ronda.")]
    public AudioClip clipInicioRonda;
    [Tooltip("Sonido al completar la ronda y entrar en intermedio.")]
    public AudioClip clipRondaCompletada;

    // Eventos para que la UI u otros sistemas escuchen
    public event Action<int> OnRondaCambiada;                 // nuevo número de ronda
    public event Action<int,int> OnCuentaEnemigosCambiada;    // (vivos, totalEstaRonda)
    public event Action<float> OnIntermedioTick;              // tiempo restante del intermedio (segundos)
    public event Action OnRondaIniciada;
    public event Action OnRondaFinalizada;

    // Estado interno
    private int totalEstaRonda = 0;
    private int vivosEstaRonda = 0;
    private bool enIntermedio = false;
    private Coroutine spawnCoroutine;

    void Start()
    {
        // Intenta coger el AudioSource si no está asignado
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (puntosSpawn == null || puntosSpawn.Length == 0)
            Debug.LogWarning("GestorRondas: No hay puntos de spawn asignados.");

        if (prefabsEnemigos == null || prefabsEnemigos.Length == 0)
            Debug.LogWarning("GestorRondas: No hay prefabs de enemigos asignados.");

        if (autoIniciarPrimeraRonda)
            StartCoroutine(IniciarSiguienteRondaDespues(1f));
    }

    // calcula enemigos que tocarían en la ronda
    public int EnemigosParaRonda(int r)
    {
        if (r <= 1) return enemigosBasePorRonda;
        return enemigosBasePorRonda + (r - 1) * incrementoEnemigosPorRonda;
    }

    IEnumerator IniciarSiguienteRondaDespues(float delay)
    {
        yield return new WaitForSeconds(delay);
        IniciarSiguienteRonda();
    }

    public void IniciarSiguienteRonda()
    {
        if (enIntermedio) return;

        rondaActual++;
        OnRondaCambiada?.Invoke(rondaActual);

        totalEstaRonda = EnemigosParaRonda(rondaActual);
        vivosEstaRonda = totalEstaRonda;
        OnCuentaEnemigosCambiada?.Invoke(vivosEstaRonda, totalEstaRonda);

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnWave(totalEstaRonda));
        
        // --- SONIDO DE INICIO DE RONDA ---
        ReproducirSonido(clipInicioRonda);

        OnRondaIniciada?.Invoke();
    }

    IEnumerator SpawnWave(int cantidad)
    {
        int spawned = 0;
        while (spawned < cantidad)
        {
            SpawnUnEnemigo();
            spawned++;
            yield return new WaitForSeconds(Mathf.Max(0.01f, tiempoEntreSpawns));
        }
    }

    void SpawnUnEnemigo()
    {
        if (puntosSpawn == null || puntosSpawn.Length == 0) return;
        if (prefabsEnemigos == null || prefabsEnemigos.Length == 0) return;

        Transform sp = puntosSpawn[UnityEngine.Random.Range(0, puntosSpawn.Length)];
        GameObject prefab = prefabsEnemigos[UnityEngine.Random.Range(0, prefabsEnemigos.Length)];

        GameObject go = Instantiate(prefab, sp.position, sp.rotation);

        var enemigo = go.GetComponent<DañoVidaEnemigo>();
        if (enemigo != null)
        {
            // escalar vida y daño por la ronda
            enemigo.vidaMaxima *= Mathf.Pow(multiplicadorVidaPorRonda, Mathf.Max(0, rondaActual - 1));
            enemigo.vidaActual = enemigo.vidaMaxima;

            enemigo.MultiplyDamage(Mathf.Pow(multiplicadorDañoPorRonda, Mathf.Max(0, rondaActual - 1)));

            // contar muertes
            enemigo.OnDeath += () => EnemigoMuerto();
        }
        else
        {
            // intentamos buscar componente en hijos si el prefab lo tiene en child
            var child = go.GetComponentInChildren<DañoVidaEnemigo>();
            if (child != null)
            {
                child.vidaMaxima *= Mathf.Pow(multiplicadorVidaPorRonda, Mathf.Max(0, rondaActual - 1));
                child.vidaActual = child.vidaMaxima;
                child.MultiplyDamage(Mathf.Pow(multiplicadorDañoPorRonda, Mathf.Max(0, rondaActual - 1)));
                child.OnDeath += () => EnemigoMuerto();
            }
            else
            {
                Debug.LogWarning("[GestorRondas] Enemigo instanciado no tiene DañoVidaEnemigo.");
            }
        }
    }

    void EnemigoMuerto()
    {
        vivosEstaRonda = Mathf.Max(0, vivosEstaRonda - 1);
        OnCuentaEnemigosCambiada?.Invoke(vivosEstaRonda, totalEstaRonda);

        // Aquí contamos las kills de GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterKill();

        if (vivosEstaRonda <= 0)
            RondaCompletada();
    }

    void RondaCompletada()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        // --- SONIDO DE FIN DE RONDA ---
        ReproducirSonido(clipRondaCompletada);

        OnRondaFinalizada?.Invoke();
        StartCoroutine(IntermedioCoroutine(tiempoEntreRondas));
    }

    IEnumerator IntermedioCoroutine(float segundos)
    {
        enIntermedio = true;
        float t = segundos;
        while (t > 0f)
        {
            OnIntermedioTick?.Invoke(t);
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }
        OnIntermedioTick?.Invoke(0f);
        enIntermedio = false;
        IniciarSiguienteRonda();
    }
    
    // --- NUEVO: Método de reproducción unificado ---
    void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (clip != null)
        {
            // Advertencia solo si el clip está asignado pero el AudioSource falta.
            Debug.LogWarning("[GestorRondas] Clip asignado, pero falta AudioSource para reproducir.");
        }
    }

    public void ForzarFinRonda()
    {
        vivosEstaRonda = 0;
        OnCuentaEnemigosCambiada?.Invoke(vivosEstaRonda, totalEstaRonda);
        RondaCompletada();
    }

    public bool EstaEnIntermedio() => enIntermedio;
    public int GetRondaActual() => rondaActual;
    public int GetVivosEstaRonda() => vivosEstaRonda;
    public int GetTotalEstaRonda() => totalEstaRonda;
}