using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class FundidoEscena : MonoBehaviour
{
    [Header("Configuración del fundido")]
    public Image imagenFundido;       // Una imagen negra a pantalla completa
    public float duracionFundido = 0.5f;

    private void Start()
    {
        if (imagenFundido)
        {
            // Fundido de entrada (negro → transparente)
            StartCoroutine(Fundir(1f, 0f));
        }
    }

    public void FundirYCargarEscena(string nombreEscena)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FundirYEntrar(nombreEscena));
    }

    private IEnumerator FundirYEntrar(string nombreEscena)
    {
        yield return StartCoroutine(Fundir(0f, 1f));
        SceneManager.LoadScene(nombreEscena);
    }

    private IEnumerator Fundir(float desde, float hasta)
    {
        if (!imagenFundido) yield break;

        float t = 0f;
        var c = imagenFundido.color;
        c.a = desde;
        imagenFundido.color = c;

        while (t < duracionFundido)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(desde, hasta, t / duracionFundido);
            c.a = a;
            imagenFundido.color = c;
            yield return null;
        }
        c.a = hasta;
        imagenFundido.color = c;
    }
}
