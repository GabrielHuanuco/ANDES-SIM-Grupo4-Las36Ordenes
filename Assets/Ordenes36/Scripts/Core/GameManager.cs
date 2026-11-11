using System;
using System.Collections.Generic;
using UnityEngine;

namespace AndesSim.Ordenes36
{
    public enum SimMode { Ataque, Defensa }

    /// <summary>
    /// Contrato mínimo común a los 11 escenarios de ANDES-SIM + gestión del ciclo
    /// interactivo: 1. ATAQUE (Revivir) -> 2. ANÁLISIS (Vulnerabilidades) -> 3. DEFENSA (Contramedidas).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I;                 // instancia única

        public ScenarioData data;                    // textos, fechas, cifras del escenario
        public SimMode mode = SimMode.Ataque;
        public SimPhase phase = SimPhase.Ataque;

        // IDs de vulnerabilidades (Análisis) y controles (Defensa)
        public const string VULN_LIMITES = "VULN_SIN_LIMITES";
        public const string VULN_DOBLE_FIRMA = "VULN_SIN_DOBLE_FIRMA";
        public const string VULN_BENEFICIARIO = "VULN_BENEFICIARIO_NUEVO";

        public const string DEF_LIMITES = "DEF_LIMITES_MONTO_HORARIO";
        public const string DEF_DOBLE_FIRMA = "DEF_DOBLE_APROBACION";
        public const string DEF_LISTA_BLANCA = "DEF_LISTA_BENEFICIARIOS";

        public event Action<string> OnEvidenceRegistered;
        public event Action<string> OnDefenseApplied;
        public event Action<SimPhase> OnPhaseChanged;
        public event Action OnStateChanged;
        public event Action OnScenarioEnded;

        readonly HashSet<string> _evidencias = new HashSet<string>();
        readonly HashSet<string> _vulnerabilidadesAnalizadas = new HashSet<string>();
        readonly HashSet<string> _defensas = new HashSet<string>();

        public IReadOnlyCollection<string> Evidencias => _evidencias;
        public IReadOnlyCollection<string> VulnerabilidadesAnalizadas => _vulnerabilidadesAnalizadas;
        public IReadOnlyCollection<string> Defensas => _defensas;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        public void SetPhase(SimPhase nuevaFase)
        {
            phase = nuevaFase;
            mode = (nuevaFase == SimPhase.Defensa && _defensas.Count > 0) ? SimMode.Defensa : SimMode.Ataque;
            Debug.Log($"[GameManager] Fase cambiada a: {phase} (Modo: {mode})");
            OnPhaseChanged?.Invoke(phase);
            OnStateChanged?.Invoke();
        }

        /// <summary>Al encontrar una pista (escanear una orden o auditar una vulnerabilidad).</summary>
        public void RegisterEvidence(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            bool nueva = _evidencias.Add(id);
            Debug.Log($"[GameManager] Evidencia {(nueva ? "nueva" : "repetida")}: {id} ({_evidencias.Count} registradas)");
            OnEvidenceRegistered?.Invoke(id);
            OnStateChanged?.Invoke();
        }

        /// <summary>Marca o desmarca una vulnerabilidad técnica auditada en la fase de Análisis.</summary>
        public void ToggleVulnerability(string vulnId)
        {
            if (string.IsNullOrEmpty(vulnId)) return;
            if (!_vulnerabilidadesAnalizadas.Add(vulnId))
                _vulnerabilidadesAnalizadas.Remove(vulnId);
            else
                RegisterEvidence(vulnId);

            Debug.Log($"[GameManager] Vulnerabilidad auditada: {vulnId} (Total: {_vulnerabilidadesAnalizadas.Count}/3)");
            OnStateChanged?.Invoke();
        }

        public bool IsVulnerabilityAnalyzed(string vulnId) => _vulnerabilidadesAnalizadas.Contains(vulnId);

        /// <summary>Contrato mínimo: activa una contramedida en el modo defensa.</summary>
        public void ApplyDefense(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _defensas.Add(id);
            mode = SimMode.Defensa;
            Debug.Log($"[GameManager] Defensa aplicada: {id} ({_defensas.Count} activas)");
            OnDefenseApplied?.Invoke(id);
            OnStateChanged?.Invoke();
        }

        /// <summary>Activa o desactiva un control técnico de defensa desde la interfaz interactiva.</summary>
        public void ToggleDefense(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (_defensas.Contains(id))
            {
                _defensas.Remove(id);
                if (_defensas.Count == 0) mode = SimMode.Ataque;
                Debug.Log($"[GameManager] Defensa desactivada: {id}");
                OnStateChanged?.Invoke();
            }
            else
            {
                ApplyDefense(id);
            }
        }

        public bool IsDefenseActive(string id) => _defensas.Contains(id);

        /// <summary>Evalúa qué estado tendría la orden según el modo y los controles de defensa activos.</summary>
        public OrderStatus EvaluateOrderStatus(OrderData o, out string motivoTecnico)
        {
            if (o == null) { motivoTecnico = ""; return OrderStatus.Pagada; }

            // Si estamos en modo Defensa y hay al menos un control activo, evaluamos reglas técnicas
            if (mode == SimMode.Defensa && _defensas.Count > 0)
            {
                var motivos = new List<string>();

                if (_defensas.Contains(DEF_LIMITES))
                {
                    double max = data != null ? data.limiteMontoMaximoUSD : 5000000;
                    if (o.montoUSD > max)
                        motivos.Add($"Monto ({o.MontoFormateado}) supera límite automático (US$ 5M)");
                    if (o.hora24 < 8 || o.hora24 >= 18)
                        motivos.Add($"Emitida fuera de ventana operativa ({o.fechaHora})");
                }

                if (_defensas.Contains(DEF_DOBLE_FIRMA) && !o.tieneDobleFirma)
                {
                    motivos.Add("Sin firma de Autorizador 2 (principio Maker-Checker / 4 ojos)");
                }

                if (_defensas.Contains(DEF_LISTA_BLANCA) && (!o.beneficiarioEnListaBlanca || o.tieneTypo))
                {
                    motivos.Add($"Beneficiario '{o.beneficiario}' no figura en lista blanca KYC/AML");
                }

                if (motivos.Count > 0)
                {
                    motivoTecnico = "RETENIDA POR REGLAS:\n• " + string.Join("\n• ", motivos);
                    return OrderStatus.Retenida;
                }
            }

            motivoTecnico = o.notaEstado;
            return o.estadoReal;
        }

        /// <summary>Muestra el resultado del modo actual.</summary>
        public void EndScenario()
        {
            Debug.Log($"[GameManager] Fin del escenario en modo {mode}. Evidencias: {_evidencias.Count}, defensas: {_defensas.Count}");
            OnScenarioEnded?.Invoke();
        }

        public OrderData GetOrder(string id) => data != null ? data.GetOrder(id) : null;
    }
}
