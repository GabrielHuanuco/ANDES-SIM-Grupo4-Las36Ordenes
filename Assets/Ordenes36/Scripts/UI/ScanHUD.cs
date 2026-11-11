using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AndesSim.Ordenes36
{
    /// <summary>
    /// Elimina cualquier interfaz 2D de pantalla completa que tape la cámara y deja
    /// únicamente un botón discreto en la esquina superior derecha para mostrar/ocultar
    /// las tarjetas 3D flotantes cuando se prueba sin marcador impreso.
    /// Toda la interfaz real (Ataque, Análisis, Defensa y simulación) vive en las tarjetas 3D flotantes.
    /// </summary>
    public class ScanHUD : MonoBehaviour
    {
        public TMP_Text estado;
        public TMP_Text contador;
        public Button btnSimularEscaneo;
        public TMP_Text txtBtnSimular;

        public Button tabAtaque;
        public Button tabAnalisis;
        public Button tabDefensa;
        public Image imgTabAtaque;
        public Image imgTabAnalisis;
        public Image imgTabDefensa;

        bool _simulandoEnEditor;
        OrderTarget _targetEscena;

        void Awake()
        {
            // Si la escena todavía tenía los paneles 2D antiguos (Panel_Inferior_Ciclo o Barra_Superior grande),
            // los destruimos de inmediato al arrancar para que NADA tape las tarjetas 3D flotantes.
            CleanLegacy2DOverlays();
        }

        void Start()
        {
            _targetEscena = FindFirstObjectByType<OrderTarget>();
            if (btnSimularEscaneo) btnSimularEscaneo.onClick.AddListener(ToggleSimulacionOrden);
            RefreshUI();
        }

        void CleanLegacy2DOverlays()
        {
            // Destruir paneles 2D antiguos si existen en la escena
            var oldBottom = transform.Find("Panel_Inferior_Ciclo");
            if (oldBottom != null) Destroy(oldBottom.gameObject);

            var oldTop = transform.Find("Barra_Superior");
            if (oldTop != null)
            {
                // Conservar únicamente el botón de simular en esquina y apagar el resto de la barra 2D
                var oldTabs = oldTop.Find("Tabs_Ciclo");
                if (oldTabs != null) Destroy(oldTabs.gameObject);
                var bg = oldTop.GetComponent<Image>();
                if (bg != null) bg.enabled = false;
            }

            if (estado != null) estado.gameObject.SetActive(false);
            if (contador != null) contador.gameObject.SetActive(false);
            if (tabAtaque != null) tabAtaque.gameObject.SetActive(false);
            if (tabAnalisis != null) tabAnalisis.gameObject.SetActive(false);
            if (tabDefensa != null) tabDefensa.gameObject.SetActive(false);
        }

        void ToggleSimulacionOrden()
        {
            if (_targetEscena == null) _targetEscena = FindFirstObjectByType<OrderTarget>();
            if (_targetEscena == null) return;

            _simulandoEnEditor = !_simulandoEnEditor;
            if (_simulandoEnEditor)
            {
                var cam = Camera.main;
                _targetEscena.transform.position = cam
                    ? cam.transform.position + cam.transform.forward * 0.62f
                    : new Vector3(0f, 0f, 0.62f);
                _targetEscena.transform.rotation = cam ? cam.transform.rotation : Quaternion.identity;
                if (_targetEscena.ficha)
                {
                    _targetEscena.ficha.transform.localPosition = Vector3.zero;
                    _targetEscena.ficha.transform.localRotation = Quaternion.identity;
                }
                _targetEscena.HandleTargetFound();
            }
            else
            {
                if (_targetEscena.ficha)
                {
                    _targetEscena.ficha.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                    _targetEscena.ficha.transform.localRotation = Quaternion.identity;
                }
                _targetEscena.HandleTargetLost();
            }
            RefreshUI();
        }

        public void RefreshUI()
        {
            if (txtBtnSimular)
                txtBtnSimular.text = _simulandoEnEditor ? "Ocultar Tarjetas 3D" : "Probar sin cámara (Tarjetas 3D)";
        }
    }
}
