using UnityEngine;

public class RecogerArma : MonoBehaviour
{
    [Header("Arma que se dará al jugador")]
    [Tooltip("Prefab del arma en mano (con ArmaDeFuego) que se equipará al recoger este objeto.")]
    public GameObject prefabArmaEnMano;

    [Header("Recogida")]
    public float distanciaRecogida = 3f;
    public KeyCode teclaRecoger = KeyCode.E;

    // Almacenamos el nombre del arma para la UI
    private string nombreArma;
    private bool estaEnRango = false;

    void Start()
    {
        // Intentamos obtener el nombre del prefab del arma.
        // Si tiene un componente de arma (por ejemplo, 'ArmaDeFuego'), úsalo.
        if (prefabArmaEnMano != null)
        {
            // Ejemplo: Suponiendo que tienes un script base de arma llamado 'BaseArma'
            // Puedes usar el nombre del objeto si no tienes un script específico de nombre:
            nombreArma = prefabArmaEnMano.name.Replace("(Clone)", "").Trim(); 
        }
        else
        {
            nombreArma = "Arma Desconocida";
        }
    }

    void Update()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) return;

        float dist = Vector3.Distance(jugador.transform.position, transform.position);

        if (dist <= distanciaRecogida)
        {
            // --- Lógica de UI: Mostrar el mensaje ---
            if (!estaEnRango && ControladorUIRecogida.Instancia != null)
            {
                ControladorUIRecogida.Instancia.MostrarMensaje(nombreArma, teclaRecoger);
                estaEnRango = true;
            }
            
            // --- Lógica de Recogida ---
            if (Input.GetKeyDown(teclaRecoger))
            {
                InventarioArmasJugador inventario = jugador.GetComponent<InventarioArmasJugador>();
                if (inventario == null)
                {
                    Debug.LogWarning("El jugador no tiene InventarioArmasJugador.");
                    return;
                }

                bool recogida = inventario.IntentarRecogerArma(
                    prefabArmaEnMano,
                    transform.position,
                    transform.rotation
                );

                if (recogida)
                {
                    // --- Ocultar UI antes de destruir ---
                    if (ControladorUIRecogida.Instancia != null)
                    {
                        ControladorUIRecogida.Instancia.OcultarMensaje();
                    }
                    Destroy(gameObject);
                }
            }
        }
        else
        {
            // --- Lógica de UI: Ocultar el mensaje ---
            if (estaEnRango && ControladorUIRecogida.Instancia != null)
            {
                ControladorUIRecogida.Instancia.OcultarMensaje();
                estaEnRango = false;
            }
        }
    }

    // Asegurarse de ocultar el mensaje si el jugador sale del rango justo antes de que el objeto sea destruido
    private void OnDestroy()
    {
        if (ControladorUIRecogida.Instancia != null && estaEnRango)
        {
            ControladorUIRecogida.Instancia.OcultarMensaje();
        }
    }
}