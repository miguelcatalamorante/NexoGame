using System;
using UnityEngine;

public class InventarioArmasJugador : MonoBehaviour
{
    [Header("Punto donde se sujetan las armas en el jugador")]
    public Transform puntoSujecionArma;  // por ejemplo, un hijo llamado "WeaponHolder"

    [Header("Configuración")]
    public int maximoArmas = 2;

    private ArmaDeFuego[] armas;
    private int indiceArmaActiva = -1;

    // Evento para avisar a la UI cuando cambia el arma activa
    public event Action OnWeaponChanged;

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
        // Cambiar entre arma 1 y arma 2
        if (Input.GetKeyDown(KeyCode.Alpha1))
            CambiarArma(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            CambiarArma(1);

        // NUEVO: rueda del ratón
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int dir = scroll > 0 ? 1 : -1;
            int next = FindNextWeaponIndex(dir);
            if (next != indiceArmaActiva)
                CambiarArma(next);
        }
    }

    int FindNextWeaponIndex(int dir)
    {
        if (maximoArmas <= 0) return -1;
        int i = indiceArmaActiva;
        for (int step = 1; step <= maximoArmas; step++)
        {
            int idx = (i + dir * step) % maximoArmas;
            if (idx < 0) idx += maximoArmas;
            if (armas[idx] != null) return idx;
        }
        return indiceArmaActiva; // no change
    }

    public bool IntentarRecogerArma(GameObject prefabArmaEnMano, Vector3 posicionSuelo, Quaternion rotacionSuelo)
    {
        if (puntoSujecionArma == null)
        {
            Debug.LogWarning("InventarioArmasJugador: falta asignar 'puntoSujecionArma'.");
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

        // Si hay hueco libre AÑADE
        if (indiceLibre != -1)
        {
            ArmaDeFuego nuevaArma = InstanciarArmaEnMano(prefabArmaEnMano);
            if (nuevaArma == null) return false;

            armas[indiceLibre] = nuevaArma;
            CambiarArma(indiceLibre);

            return true;
        }

        // Si NO hay hueco libre, soltar arma actual y sustituir
        int indiceActual = indiceArmaActiva;
        if (indiceActual < 0 || indiceActual >= maximoArmas)
            indiceActual = 0;

        ArmaDeFuego armaActual = armas[indiceActual];

        if (armaActual != null && armaActual.prefabPickupSuelo != null)
        {
            // Instanciamos el pickup de la arma actual en el suelo
            Instantiate(armaActual.prefabPickupSuelo, posicionSuelo, rotacionSuelo);
        }

        if (armaActual != null)
        {
            Destroy(armaActual.gameObject);
            armas[indiceActual] = null;
        }

        // Instanciar la nueva arma en la mano
        ArmaDeFuego nueva = InstanciarArmaEnMano(prefabArmaEnMano);
        if (nueva == null) return false;

        armas[indiceActual] = nueva;
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
            Debug.LogWarning("El prefab de arma en mano no tiene ArmaDeFuego.");
        else
        {
            // inicialmente desuscribimos input hasta que se seleccione
            arma.SetEquipped(false);
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

        OnWeaponChanged?.Invoke();
    }

    private void ActualizarArmaActivaVisual()
    {
        for (int i = 0; i < maximoArmas; i++)
        {
            if (armas[i] != null)
            {
                bool activa = (i == indiceArmaActiva);
                armas[i].gameObject.SetActive(activa);

                // importante: sólo la arma activa responde a Input
                armas[i].SetEquipped(activa);
            }
        }
    }

    public ArmaDeFuego ObtenerArmaActiva()
    {
        if (indiceArmaActiva < 0 || indiceArmaActiva >= maximoArmas)
            return null;

        return armas[indiceArmaActiva];
    }
}
