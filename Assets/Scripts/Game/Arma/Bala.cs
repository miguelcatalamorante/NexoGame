using UnityEngine;

public class Bala : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 50f;
    public float tiempoVida = 3f;

    [Header("Daño")]
    [HideInInspector] 
    public float daño; // <-- lo pone el arma al instanciar la bala

    void Start()
    {
        // destruir la bala pasado un tiempo por si no choca con nada
        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        // avanzar siempre hacia adelante (eje Z local)
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision other)
    {
        // buscar enemigo en el objeto golpeado
        DañoVidaEnemigo enemigo = other.collider.GetComponentInParent<DañoVidaEnemigo>();

        if (enemigo != null)
        {
            // aplicar el daño que nos haya dado el arma
            enemigo.RecibirDaño(daño, gameObject);
        }

        // destruir la bala al impactar
        Destroy(gameObject);
    }
}
