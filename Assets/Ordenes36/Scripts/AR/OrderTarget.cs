using UnityEngine;
using Vuforia;

namespace AndesSim.Ordenes36
{
    /// <summary>
    /// Va en cada ImageTarget de orden. Al reconocerse el marcador (OnTargetFound de
    /// DefaultObserverEventHandler) carga los datos de la orden desde ScenarioData,
    /// registra la evidencia en el GameManager y muestra la ficha 3D.
    /// </summary>
    [RequireComponent(typeof(ObserverBehaviour))]
    public class OrderTarget : MonoBehaviour
    {
        [Tooltip("Id de la orden en ScenarioData. Si está vacío se usa el nombre del target de Vuforia.")]
        public string orderId = "ORD-01";
        public OrderCard ficha;

        public static event System.Action<OrderTarget, OrderData> OnOrderFound;
        public static event System.Action<OrderTarget> OnOrderLost;

        ObserverBehaviour _observer;
        OrderData _data;

        public OrderData Data => _data;

        void Start()
        {
            _observer = GetComponent<ObserverBehaviour>();
            if (string.IsNullOrEmpty(orderId) && _observer) orderId = _observer.TargetName;

            var gm = GameManager.I;
            _data = gm ? gm.GetOrder(orderId) : null;
            if (_data == null)
            {
                Debug.LogWarning($"[OrderTarget] No hay datos para '{orderId}' en ScenarioData.");
                return;
            }
            if (ficha) ficha.Bind(_data, gm.data.totalOrdenes);
        }

        /// <summary>Conectado a DefaultObserverEventHandler.OnTargetFound.</summary>
        public void HandleTargetFound()
        {
            Debug.Log($"[OrderTarget] Orden reconocida: {orderId}");
            if (GameManager.I) GameManager.I.RegisterEvidence(orderId);
            if (ficha)
            {
                ForceEnableRenderers(ficha.gameObject);
                ficha.Show();
            }
            OnOrderFound?.Invoke(this, _data);
        }

        /// <summary>Conectado a DefaultObserverEventHandler.OnTargetLost.</summary>
        public void HandleTargetLost()
        {
            Debug.Log($"[OrderTarget] Orden perdida: {orderId}");
            if (ficha) ficha.Hide();
            OnOrderLost?.Invoke(this);
        }

        static void ForceEnableRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
            foreach (var c in root.GetComponentsInChildren<Canvas>(true))
                c.enabled = true;
        }
    }
}
