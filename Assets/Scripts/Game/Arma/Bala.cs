using UnityEngine;

public class Bala : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 50f;
    public float tiempoVida = 3f;

    [Header("Daño")]
    [HideInInspector] 
    public float daño; 

    void Start()
    {
        // destruir la bala pasado un tiempo por si no choca con nada
        Destroy(gameObject, tiempoVida);
    }

    void Update()
    {
        
        transform.Translate(Vector3.forward * velocidad * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision other)
    {
        
        DañoVidaEnemigo enemigo = other.collider.GetComponentInParent<DañoVidaEnemigo>();

        if (enemigo != null)
        {
            // aplicar el daño que nos haya dado el arma
            enemigo.RecibirDaño(daño, gameObject);

            if (enemigo.IsDead())
            {
                Destroy(enemigo.gameObject);
            }
        }

        // destruir la bala al impactar
        Destroy(gameObject);
    }
}
