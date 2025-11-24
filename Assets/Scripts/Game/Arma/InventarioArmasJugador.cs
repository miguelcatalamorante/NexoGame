using UnityEngine;

public class InventarioArmasJugador : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("Objeto hijo donde se sujetan las armas (por ejemplo un empty llamado 'WeaponHolder').")]
    public Transform puntoSujecionArma;

    [Header("Configuración")]
    public int maximoArmas = 2;

    private ArmaDeFuego[] armas;
    private int indiceArmaActiva = -1;

    void Awake()
    {
        armas = new ArmaDeFuego[maximoArmas];
    }

    void Start()
    {
        ActualizarArmaActivaVisual();
    }

    void Update()
    {
        // Cambiar entre arma 1 y 2
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CambiarArma(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CambiarArma(1);
        }
    }

    /// <summary>
    /// Intenta recoger un arma nueva.
    /// Si hay hueco libre, la añade.
    /// Si no hay hueco, suelta el arma actual al suelo y la sustituye.
    /// </summary>
    public bool IntentarRecogerArma(GameObject prefabArmaEnMano, Vector3 posicionDrop, Quaternion rotacionDrop)
    {
        if (puntoSujecionArma == null)
        {
            Debug.LogWarning("InventarioArmasJugador: no hay puntoSujecionArma asignado.");
            return false;
        }

        // 1) Buscar hueco libre
        int indiceLibre = -1;
        for (int i = 0; i < maximoArmas; i++)
        {
            if (armas[i] == null)
            {
                indiceLibre = i;
                break;
            }
        }

        // 2) Si hay hueco libre, simplemente la añadimos ahí
        if (indiceLibre != -1)
        {
            ArmaDeFuego armaNueva = InstanciarArmaEnMano(prefabArmaEnMano);
            if (armaNueva == null) return false;

            armas[indiceLibre] = armaNueva;
            CambiarArma(indiceLibre);

            return true;
        }

        // 3) Si NO hay hueco libre -> soltar arma actual y sustituir
        int indiceActual = indiceArmaActiva;
        if (indiceActual < 0 || indiceActual >= maximoArmas)
            indiceActual = 0;

        ArmaDeFuego armaActual = armas[indiceActual];

        if (armaActual != null && armaActual.prefabPickupSuelo != null)
        {
            // Instanciamos el pickup de la arma actual en el suelo
            Instantiate(armaActual.prefabPickupSuelo, posicionDrop, rotacionDrop);
        }

        if (armaActual != null)
        {
            Destroy(armaActual.gameObject);
            armas[indiceActual] = null;
        }

        // Ahora instanciamos la nueva en esa misma posición del inventario
        ArmaDeFuego nuevaArma = InstanciarArmaEnMano(prefabArmaEnMano);
        if (nuevaArma == null) return false;

        armas[indiceActual] = nuevaArma;
        CambiarArma(indiceActual);

        return true;
    }

    private ArmaDeFuego InstanciarArmaEnMano(GameObject prefabArmaEnMano)
    {
        GameObject armaGO = Instantiate(prefabArmaEnMano, puntoSujecionArma);
        armaGO.transform.localPosition = Vector3.zero;
        armaGO.transform.localRotation = Quaternion.identity;

        ArmaDeFuego arma = armaGO.GetComponent<ArmaDeFuego>();
        if (arma == null)
        {
            Debug.LogWarning("El prefab de arma no tiene componente ArmaDeFuego.");
        }
        return arma;
    }

    public void CambiarArma(int indice)
    {
        if (indice < 0 || indice >= maximoArmas)
            return;

        if (armas[indice] == null)
            return;

        indiceArmaActiva = indice;
        ActualizarArmaActivaVisual();
    }

    private void ActualizarArmaActivaVisual()
    {
        for (int i = 0; i < maximoArmas; i++)
        {
            if (armas[i] != null)
            {
                armas[i].gameObject.SetActive(i == indiceArmaActiva);
            }
        }
    }
}
