using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Ordenes36
{
    /// <summary>
    /// Textos, fechas y cifras del escenario 4 «Las 36 órdenes» (contrato común ANDES-SIM).
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioData_Ordenes36", menuName = "ANDES-SIM/Scenario Data")]
    public class ScenarioData : ScriptableObject
    {
        public int numeroEscenario = 4;
        public string titulo = "Las 36 órdenes";
        public string fecha = "4 y 5 feb 2016";
        [TextArea(3, 6)]
        public string historia =
            "Las 36 órdenes viajan al Banco Corresponsal Internacional. Treinta y cinco son rechazadas por formato " +
            "y reenviadas corregidas esa noche (una nunca se reenvía). Al final se bloquean 30 y pasan 5: " +
            "cuatro a Manila y una, por 20 millones, a la «Fandation».";

        public int totalOrdenes = 36;
        public double montoTotalUSD = 951000000;

        [Header("Umbrales de Seguridad (Motor de Reglas)")]
        public double limiteMontoMaximoUSD = 5000000;
        public int horaInicioLaboral = 8;
        public int horaFinLaboral = 18;

        [Tooltip("Órdenes que tienen marcador impreso. El id debe coincidir con el nombre del Image Target.")]
        public List<OrderData> ordenes = new List<OrderData>();

        public OrderData GetOrder(string id)
        {
            foreach (var o in ordenes)
                if (o != null && o.id == id) return o;
            return null;
        }
    }
}
