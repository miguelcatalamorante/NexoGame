using UnityEngine;

public class RecogerArma : MonoBehaviour
{
    [Header("Arma que se dará al jugador")]
    [Tooltip("Prefab del arma en mano (con ArmaDeFuego) que se equipará al recoger este objeto.")]
    public GameObject prefabArmaEnMano;

    [Header("Recogida")]
    public float distanciaRecogida = 3f;
    public KeyCode teclaRecoger = KeyCode.E;

    void Update()
    {
        GameObject jugador = GameObject.FindGameObjectWithTag("Player");
        if (jugador == null) return;

        float dist = Vector3.Distance(jugador.transform.position, transform.position);
        if (dist > distanciaRecogida) return;

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
                Destroy(gameObject);
            }
        }
    }
}
