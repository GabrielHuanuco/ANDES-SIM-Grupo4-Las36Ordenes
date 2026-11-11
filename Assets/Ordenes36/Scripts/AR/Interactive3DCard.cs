using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AndesSim.Ordenes36
{
    /// <summary>
    /// Permite tocar con el dedo en el celular o hacer clic con el mouse en Unity
    /// sobre cualquier tarjeta 3D flotante. Incluye feedback de escala 3D animado al pulsar.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Interactive3DCard : MonoBehaviour, IPointerClickHandler
    {
        public enum CardAction
        {
            SetPhaseAtaque,
            SetPhaseAnalisis,
            SetPhaseDefensa,
            ToggleItem0,
            ToggleItem1,
            ToggleItem2,
            StepCycle
        }

        public CardAction action;
        public OrderCard parentStage;

        Vector3 _baseScale;
        float _pulseTimer;

        void Awake()
        {
            _baseScale = transform.localScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Execute();
        }

        public void Execute()
        {
            _pulseTimer = 0.25f;
            var gm = GameManager.I;
            if (gm == null) return;

            switch (action)
            {
                case CardAction.SetPhaseAtaque:
                    gm.SetPhase(SimPhase.Ataque);
                    break;
                case CardAction.SetPhaseAnalisis:
                    gm.SetPhase(SimPhase.Analisis);
                    break;
                case CardAction.SetPhaseDefensa:
                    gm.SetPhase(SimPhase.Defensa);
                    break;
                case CardAction.ToggleItem0:
                    if (gm.phase != SimPhase.Analisis && gm.phase != SimPhase.Defensa)
                        gm.SetPhase(SimPhase.Analisis);
                    if (gm.phase == SimPhase.Analisis) gm.ToggleVulnerability(GameManager.VULN_LIMITES);
                    else if (gm.phase == SimPhase.Defensa) gm.ToggleDefense(GameManager.DEF_LIMITES);
                    break;
                case CardAction.ToggleItem1:
                    if (gm.phase != SimPhase.Analisis && gm.phase != SimPhase.Defensa)
                        gm.SetPhase(SimPhase.Analisis);
                    if (gm.phase == SimPhase.Analisis) gm.ToggleVulnerability(GameManager.VULN_DOBLE_FIRMA);
                    else if (gm.phase == SimPhase.Defensa) gm.ToggleDefense(GameManager.DEF_DOBLE_FIRMA);
                    break;
                case CardAction.ToggleItem2:
                    if (gm.phase != SimPhase.Analisis && gm.phase != SimPhase.Defensa)
                        gm.SetPhase(SimPhase.Analisis);
                    if (gm.phase == SimPhase.Analisis) gm.ToggleVulnerability(GameManager.VULN_BENEFICIARIO);
                    else if (gm.phase == SimPhase.Defensa) gm.ToggleDefense(GameManager.DEF_LISTA_BLANCA);
                    break;
                case CardAction.StepCycle:
                    if (parentStage != null) parentStage.AdvanceCycleStep();
                    break;
            }
        }

        void Update()
        {
            if (_pulseTimer > 0f)
            {
                _pulseTimer -= Time.deltaTime;
                float s = 1f + 0.14f * Mathf.Sin((_pulseTimer / 0.25f) * Mathf.PI);
                transform.localScale = _baseScale * s;
            }
            else
            {
                transform.localScale = _baseScale;
            }
        }
    }
}
