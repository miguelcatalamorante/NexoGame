using UnityEngine;
using TMPro;

public class CreditosPanelUI : MonoBehaviour
{
    public string nombreJuego = "NEXO";
    public string version = "v0.1";
    public string[] autores = { "Miguel – Programación y Arquitectura" };
    public string[] agradecimientos = { "Comunidad", "Familia y amigos" };

    public TextMeshProUGUI titulo;
    public TextMeshProUGUI contenido;

    void OnEnable()
    {
        if (titulo) titulo.text = "Créditos";

        if (contenido)
        {
            var year = System.DateTime.Now.Year;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine($"{nombreJuego} {version}");
            sb.AppendLine($"© {year} Todos los derechos reservados.");
            sb.AppendLine();
            sb.AppendLine("<b>Equipo</b>");
            foreach (var a in autores) sb.AppendLine($"• {a}");
            sb.AppendLine();
            if (agradecimientos != null && agradecimientos.Length > 0)
            {
                sb.AppendLine("<b>Agradecimientos</b>");
                foreach (var g in agradecimientos) sb.AppendLine($"• {g}");
                sb.AppendLine();
            }
            sb.AppendLine("Herramientas: Unity, Mixamo.com, GitHub, Visual Studio, Photoshop");
            sb.AppendLine("Música/Arte: ChatGPT-4, Recursos gratuitos");

            contenido.text = sb.ToString();
        }
    }

    // Por si quieres botones a redes/enlaces desde el panel
    public void AbrirURL(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
            Application.OpenURL(url);
    }
}
