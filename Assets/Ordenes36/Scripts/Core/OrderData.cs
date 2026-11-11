using System;
using UnityEngine;

namespace AndesSim.Ordenes36
{
    /// <summary>Estado final real de una orden SWIFT (caso Bangladesh 2016 adaptado al BANCO ANDINO).</summary>
    public enum OrderStatus
    {
        Bloqueada,      // detenida por el Banco Corresponsal Internacional (lista de sanciones)
        Pagada,         // liberada y acreditada al beneficiario (ataque exitoso)
        Retenida,       // retenida por un control transaccional (modo defensa)
        Detenida,       // detenida por el banco intermediario (caso «Fandation»)
        NoReenviada     // rechazada por formato y nunca reenviada
    }

    /// <summary>Fase del ciclo pedagógico de ciberseguridad (Revivir -> Analizar -> Defender).</summary>
    public enum SimPhase
    {
        Ataque,         // 1. REVIVIR: cómo sucedió el ataque en 2016
        Analisis,       // 2. ANALIZAR: vulnerabilidades técnicas aprovechadas por Lazarus
        Defensa         // 3. DEFENDER: contramedidas técnicas que detienen la orden
    }

    /// <summary>Datos de una de las 36 órdenes. Se guarda dentro de <see cref="ScenarioData"/>.</summary>
    [Serializable]
    public class OrderData
    {
        [Tooltip("Identificador único; debe coincidir con el nombre del Image Target (ej. ORD-01).")]
        public string id = "ORD-01";
        [Tooltip("Número de la orden dentro de las 36.")]
        public int numero = 1;
        public string referenciaSwift = "BAND160204001";
        public string fechaHora = "04 feb 2016 · 20:58";
        public int hora24 = 20;
        public double montoUSD = 30000000;
        public string beneficiario = "";
        public string beneficiarioRealEsperado = "";
        public string cuentaBeneficiario = "";
        public string destino = "";
        public string pais = "";
        public bool beneficiarioEnListaBlanca = false;
        public bool tieneDobleFirma = false;
        public bool tieneTypo = false;

        public OrderStatus estadoReal = OrderStatus.Pagada;
        [TextArea(2, 4)] public string notaEstado = "";

        [Header("Explicaciones técnicas de Ciberseguridad")]
        [TextArea(3, 5)] public string explicacionAtaque = "";
        [TextArea(3, 5)] public string vulnerabilidad1 = "";
        [TextArea(3, 5)] public string vulnerabilidad2 = "";
        [TextArea(3, 5)] public string vulnerabilidad3 = "";
        [TextArea(3, 5)] public string explicacionDefensa = "";

        public string MontoFormateado => "US$ " + montoUSD.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        public static string EstadoTexto(OrderStatus s)
        {
            switch (s)
            {
                case OrderStatus.Bloqueada: return "BLOQUEADA (SANCIONES)";
                case OrderStatus.Pagada: return "PAGADA · FONDOS ROBADOS";
                case OrderStatus.Retenida: return "RETENIDA POR DEFENSA";
                case OrderStatus.Detenida: return "DETENIDA («FANDATION»)";
                case OrderStatus.NoReenviada: return "NO REENVIADA";
            }
            return s.ToString().ToUpperInvariant();
        }

        public static Color EstadoColor(OrderStatus s)
        {
            switch (s)
            {
                case OrderStatus.Bloqueada: return new Color(0.30f, 0.65f, 1.00f);   // azul
                case OrderStatus.Pagada: return new Color(1.00f, 0.32f, 0.28f);      // rojo (ataque exitoso)
                case OrderStatus.Retenida: return new Color(0.22f, 0.95f, 0.54f);    // verde (defensa activa)
                case OrderStatus.Detenida: return new Color(1.00f, 0.76f, 0.29f);    // ámbar
                case OrderStatus.NoReenviada: return new Color(0.55f, 0.60f, 0.58f); // gris
            }
            return Color.white;
        }
    }
}
