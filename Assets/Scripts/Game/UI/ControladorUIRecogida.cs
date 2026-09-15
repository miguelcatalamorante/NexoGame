using UnityEngine;
using TMPro; // Asegúrate de incluir esta línea para TextMeshPro

public class ControladorUIRecogida : MonoBehaviour
{
    // Asigna este objeto TextMeshPro en el Inspector
    public TextMeshProUGUI textoRecogida;

    // Patrón Singleton (Opcional, pero muy útil para acceder a él fácilmente)
    public static ControladorUIRecogida Instancia;

    private void Awake()
    {
        // Implementación simple de Singleton
        if (Instancia != null && Instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instancia = this;
        }

        // Asegurarse de que el texto está oculto al inicio
        if (textoRecogida != null)
        {
            textoRecogida.gameObject.SetActive(false);
        }
    }

    // Método que llamaremos desde el arma para mostrar el mensaje
    public void MostrarMensaje(string nombreArma, KeyCode tecla)
    {
        if (textoRecogida != null)
        {
            // El formato será: "Pulsa E para recoger (Nombre del Arma)"
            textoRecogida.text = $"Pulsa **{tecla.ToString()}** para recoger **{nombreArma}**";
            textoRecogida.gameObject.SetActive(true);
        }
    }

    // Método que llamaremos para ocultar el mensaje
    public void OcultarMensaje()
    {
        if (textoRecogida != null)
        {
            textoRecogida.gameObject.SetActive(false);
        }
    }
}