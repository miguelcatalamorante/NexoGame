using UnityEngine;
using TMPro;

public class ControlesPanelUI : MonoBehaviour
{
    [TextArea(6, 20)]
    public string textoPersonalizado;

    public TextMeshProUGUI titulo;
    public TextMeshProUGUI contenido;

    void OnEnable()
    {
        if (titulo) titulo.text = "Controles";
        if (contenido)
        {
            if (!string.IsNullOrEmpty(textoPersonalizado))
            {
                contenido.text = textoPersonalizado;
            }
            else
            {
                contenido.text =
@"Movimiento .................. W / A / S / D
Cámara ...................... Ratón
Correr ...................... Shift Izquierdo
Agacharse ................... Control Izquierdo
Saltar ...................... Espacio
Puñetazo .................... Click Izquierdo
Disparar arma ............... Click Izquierdo (con arma)
Recargar .................... R
Interacción ................. E

Consejo: si te quedas sin estamina al correr,
camina unos segundos para recuperarla.";
            }
        }
    }
}
