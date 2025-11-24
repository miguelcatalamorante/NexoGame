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
public float cadenciaDisparo = 0.1f;
public float tiempoRecarga = 2f;

[Header("Punto de disparo")]
public Transform puntoDisparo;
public GameObject prefabBala;

[Header("Pickup al soltar")]
public GameObject prefabPickupSuelo;   

    private float siguienteTiempoDisparo = 0f;
    private bool recargando = false;

    void Update()
    {
        // Solo el arma activa puede disparar
        if (!gameObject.activeInHierarchy)
            return;

        if (recargando)
            return;

        // 📌 DISPARAR (Fire1 = click izquierdo)
        if (Input.GetButton("Fire1") && Time.time >= siguienteTiempoDisparo)
        {
            if (balasEnCargador > 0)
            {
                siguienteTiempoDisparo = Time.time + cadenciaDisparo;
                Disparar();
            }
            else
            {
                // si no hay balas pero sí reserva, recargar
                if (balasEnReserva > 0)
                    StartCoroutine(Recargar());
            }
        }

        // 📌 RECARGA MANUAL (tecla R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (balasEnCargador < tamañoCargador && balasEnReserva > 0)
                StartCoroutine(Recargar());
        }
    }

    void Disparar()
    {
        balasEnCargador--;

        // 🔥 Instanciar la bala física
        if (prefabBala != null && puntoDisparo != null)
        {
            GameObject balaGO = Instantiate(prefabBala, puntoDisparo.position, puntoDisparo.rotation);

            // asignar daño desde el arma
            Bala balaScript = balaGO.GetComponent<Bala>();
            if (balaScript != null)
            {
                balaScript.daño = dañoPorBala;
            }
        }

        // (Opcional) aquí puedes agregar sonido, retroceso, partículas...
    }

    IEnumerator Recargar()
    {
        recargando = true;

        // aquí puedes reproducir animación/sonido de recarga
        yield return new WaitForSeconds(tiempoRecarga);

        int necesarias = tamañoCargador - balasEnCargador;
        int aCargar = Mathf.Min(necesarias, balasEnReserva);

        balasEnReserva -= aCargar;
        balasEnCargador += aCargar;

        recargando = false;
    }
}
