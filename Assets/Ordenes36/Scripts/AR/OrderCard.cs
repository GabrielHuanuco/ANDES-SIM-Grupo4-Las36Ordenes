using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AndesSim.Ordenes36
{
    /// <summary>
    /// Tablero Holográfico 3D de Tarjetas Flotantes con estética SOC Ciberseguridad (ANDES-SIM · Banco Andino):
    /// - Materiales URP persistidos en disco (cero rosa/magenta) y auto-reparados en Awake.
    /// - Paleta de consola SOC nocturna: paneles verde-obsidiana (#04100B), azul marino corporativo (#0A2540),
    ///   bordes neón SWIFT (#38F28A), alerta rojo Lazarus (#FF4B42), ámbar forense (#FFC24A) y cian (#38C8FF).
    /// - Siempre DERECHO frente a la cámara (billboard limpio).
    /// - Incluye:
    ///   1. Tres tarjetas-pestaña 3D superiores: [1. ATAQUE (2016)] · [2. ANÁLISIS TÉCNICO] · [3. DEFENSA TÉCNICA]
    ///   2. Tarjeta 3D Superior de Explicación Técnica con franja de cabecera y acento neón.
    ///   3. Tarjeta 3D Central de la Orden Bancaria SWIFT MT103 con separadores, chips de datos y sello de estado.
    ///   4. Tres tarjetas 3D Izquierdas (Análisis de Vulnerabilidades) con barra de acento ámbar e indicadores.
    ///   5. Tres tarjetas 3D Derechas (Controles Técnicos de Defensa) con barra de acento esmeralda e indicadores.
    ///   6. Tarjeta 3D Inferior con la Ruta SWIFT animada (Banco Andino -> Corresponsal NY -> Manila),
    ///      paquete MT103 proporcional, barrera láser de defensa y matriz 6x6 de las 36 órdenes ($951M).
    /// </summary>
    public class OrderCard : MonoBehaviour
    {
        [Header("Material Base (URP Unlit)")]
        public Material materialBase;

        [Header("Tarjeta Central (Orden SWIFT MT103)")]
        public TMP_Text titulo;
        public TMP_Text referencia;
        public TMP_Text monto;
        public TMP_Text beneficiario;
        public TMP_Text destino;
        public TMP_Text estado;
        public TMP_Text nota;
        public Renderer marco;
        public Renderer insigniaEstado;
        public Renderer franjaSuperiorCentral;

        [Header("Pestañas 3D Superiores (Ciclo)")]
        public Renderer tab3DAtaque;
        public Renderer tab3DAnalisis;
        public Renderer tab3DDefensa;
        public Renderer fondoTabAtaque;
        public Renderer fondoTabAnalisis;
        public Renderer fondoTabDefensa;

        [Header("Tarjeta 3D de Explicación Técnica del Ciclo (Ataque / Análisis / Defensa)")]
        public Renderer marcoExplicacion3D;
        public Renderer cabExplicacion3D;
        public TMP_Text tituloExplicacion3D;
        public TMP_Text cuerpoExplicacion3D;

        [Header("Tarjetas 3D Izquierdas (Análisis: 3 Vulnerabilidades)")]
        public Transform grupoAnalisis3D;
        public Renderer[] marcosVuln = new Renderer[3];
        public Renderer[] acentosVuln = new Renderer[3];
        public Renderer[] fondosVuln = new Renderer[3];
        public TMP_Text[] textosVuln = new TMP_Text[3];

        [Header("Tarjetas 3D Derechas (Defensa: 3 Controles)")]
        public Transform grupoDefensa3D;
        public Renderer[] marcosDef = new Renderer[3];
        public Renderer[] acentosDef = new Renderer[3];
        public Renderer[] fondosDef = new Renderer[3];
        public TMP_Text[] textosDef = new TMP_Text[3];

        [Header("Animación 3D de Ruta SWIFT + 36 Órdenes (Tarjeta Inferior)")]
        public Renderer marcoFlujo3D;
        public TMP_Text tituloFlujo;
        public TMP_Text explicacionFlujo;
        public Transform paqueteViajero;
        public Renderer paqueteRenderer;
        public Transform escudoBarrera;
        public Renderer escudoRenderer;
        public Renderer[] nodosRuta = new Renderer[3]; // 0: Banco Andino, 1: Corresponsal, 2: Manila
        public Renderer[] cuadros36 = new Renderer[36];

        [Header("Animación y Orientación")]
        public bool mantenerDerechoFrenteACamara = true;
        public float duracionAparicion = 0.45f;
        public float duracionDesaparicion = 0.2f;
        public float amplitudFlotado = 0.003f;   // metros
        public float velocidadFlotado = 1.6f;

        Vector3 _escalaBase;
        Vector3 _posBase;
        Coroutine _anim;
        bool _visible;
        MaterialPropertyBlock _mpb;
        Color _colorEstado = Color.white;
        OrderData _data;
        int _totalOrdenes = 36;
        float _recorridoT;

        // --- Paleta estética SOC Ciberseguridad (ANDES-SIM · Banco Andino)
        static readonly Color ColFondoOscuro      = new Color(0.024f, 0.055f, 0.075f); // #060E13 obsidiana
        static readonly Color ColFondoTarjeta     = new Color(0.035f, 0.082f, 0.114f); // #09151D azul noche
        static readonly Color ColFondoSubpanel    = new Color(0.055f, 0.125f, 0.168f); // #0E202B caja interna
        static readonly Color ColCabBanco         = new Color(0.047f, 0.196f, 0.365f); // #0C325D azul Banco Andino
        static readonly Color ColCabVuln          = new Color(0.290f, 0.175f, 0.035f); // cabecera ámbar oscuro
        static readonly Color ColCabDef           = new Color(0.035f, 0.235f, 0.145f); // cabecera esmeralda oscuro
        static readonly Color ColRojo             = new Color(1.000f, 0.275f, 0.235f); // #FF463C rojo alerta Lazarus
        static readonly Color ColRojoFondoActivo  = new Color(0.240f, 0.055f, 0.065f);
        static readonly Color ColAmbar            = new Color(1.000f, 0.760f, 0.220f); // #FFC238 ámbar forense
        static readonly Color ColAmbarFondoActivo = new Color(0.220f, 0.150f, 0.035f);
        static readonly Color ColVerde            = new Color(0.220f, 0.950f, 0.540f); // #38F28A verde neón SWIFT
        static readonly Color ColVerdeFondoActivo = new Color(0.045f, 0.210f, 0.130f);
        static readonly Color ColCian             = new Color(0.260f, 0.780f, 1.000f); // #42C7FF cian nodo
        static readonly Color ColOro              = new Color(1.000f, 0.840f, 0.310f); // #FFD64F dorado monto
        static readonly Color ColGris             = new Color(0.260f, 0.330f, 0.380f);
        static readonly Color ColBordeInactivo    = new Color(0.140f, 0.250f, 0.320f);

        public bool Visible => _visible;
        public OrderData Data => _data;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();

            EnsureValidMaterialsAndHierarchy();

            _escalaBase = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            _posBase = transform.localPosition;
            transform.localScale = Vector3.zero;
        }

        void OnEnable()
        {
            if (GameManager.I != null)
                GameManager.I.OnStateChanged += RefreshVisuals;
        }

        void OnDisable()
        {
            if (GameManager.I != null)
                GameManager.I.OnStateChanged -= RefreshVisuals;
        }

        public void Bind(OrderData o, int totalOrdenes)
        {
            _data = o;
            _totalOrdenes = totalOrdenes;
            if (GameManager.I != null)
            {
                GameManager.I.OnStateChanged -= RefreshVisuals;
                GameManager.I.OnStateChanged += RefreshVisuals;
            }
            RefreshVisuals();
        }

        /// <summary>
        /// Garantiza que ningún cubo tenga material nulo (que en URP se ve fucsia/magenta)
        /// y construye el tablero completo si faltan referencias.
        /// </summary>
        public void EnsureValidMaterialsAndHierarchy()
        {
            if (materialBase == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color")
                      ?? Shader.Find("Sprites/Default");
                materialBase = new Material(sh);
            }

            if (tab3DAtaque == null || paqueteViajero == null || marcoExplicacion3D == null)
            {
                BuildAestheticRig(materialBase,
                    CreateColoredMat(materialBase, ColFondoTarjeta),
                    CreateColoredMat(materialBase, ColFondoSubpanel),
                    CreateColoredMat(materialBase, ColCabBanco),
                    CreateColoredMat(materialBase, ColCabVuln),
                    CreateColoredMat(materialBase, ColCabDef),
                    CreateColoredMat(materialBase, ColBordeInactivo));
            }
            else
            {
                // Reparar cualquier MeshRenderer que haya quedado sin material serializado
                foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.GetComponent<TMP_Text>() != null) continue;
                    if (mr.sharedMaterial == null)
                        mr.sharedMaterial = materialBase;
                }
            }
        }

        static Material CreateColoredMat(Material template, Color c)
        {
            var m = new Material(template);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        /// <summary>
        /// Construye el tablero de 11 tarjetas 3D con estética SOC Ciberseguridad.
        /// Recibe materiales de disco cuando se llama desde el Editor para que el prefab quede 100% serializado.
        /// </summary>
        public void BuildAestheticRig(Material matEstado, Material matFondo, Material matSubpanel,
                                      Material matCabBanco, Material matCabVuln, Material matCabDef, Material matLinea)
        {
            materialBase = matEstado;

            var mm = transform.Find("Contenido_mm");
            if (mm == null)
            {
                mm = new GameObject("Contenido_mm").transform;
                mm.SetParent(transform, false);
                mm.localScale = Vector3.one * 0.001f;
            }

            for (int i = mm.childCount - 1; i >= 0; i--)
                DestroyImmediate(mm.GetChild(i).gameObject);

            var font = TMP_Settings.defaultFontAsset;
            var gris = new Color(0.68f, 0.80f, 0.76f);

            // ==================================================================
            // 1. PESTAÑAS 3D SUPERIORES (Clicables / Tocables en 3D)
            // ==================================================================
            var tabs3D = new GameObject("Tabs_3D_Ciclo").transform;
            tabs3D.SetParent(mm, false);
            tabs3D.localPosition = new Vector3(0, 130, 0);

            Make3DTab("Tab3D_Ataque", tabs3D, new Vector3(-182, 0, 0), new Vector3(170, 24, 3),
                matEstado, matFondo, font, "1 · ATAQUE (REVIVIR 2016)",
                Interactive3DCard.CardAction.SetPhaseAtaque, out tab3DAtaque, out fondoTabAtaque);

            Make3DTab("Tab3D_Analisis", tabs3D, new Vector3(0, 0, 0), new Vector3(170, 24, 3),
                matEstado, matFondo, font, "2 · ANÁLISIS TÉCNICO",
                Interactive3DCard.CardAction.SetPhaseAnalisis, out tab3DAnalisis, out fondoTabAnalisis);

            Make3DTab("Tab3D_Defensa", tabs3D, new Vector3(182, 0, 0), new Vector3(170, 24, 3),
                matEstado, matFondo, font, "3 · DEFENSA TÉCNICA",
                Interactive3DCard.CardAction.SetPhaseDefensa, out tab3DDefensa, out fondoTabDefensa);

            // ==================================================================
            // 2. TARJETA 3D SUPERIOR: EXPLICACIÓN TÉCNICA (Puntos 4, 5 y 6)
            // ==================================================================
            var expCard = new GameObject("Tarjeta_3D_Explicacion").transform;
            expCard.SetParent(mm, false);
            expCard.localPosition = new Vector3(0, 88, 0);

            marcoExplicacion3D = MakeBox("Marco_Exp", expCard, new Vector3(0, 0, 1.4f), new Vector3(540, 44, 2), matEstado, false).GetComponent<Renderer>();
            var fondoExp = MakeBox("Fondo_Exp", expCard, Vector3.zero, new Vector3(534, 39, 3), matFondo, true);
            var hitExp = fondoExp.AddComponent<Interactive3DCard>();
            hitExp.parentStage = this;
            hitExp.action = Interactive3DCard.CardAction.StepCycle;

            cabExplicacion3D = MakeBox("Franja_Exp", expCard, new Vector3(0, 12.5f, -1.7f), new Vector3(534, 14, 1), matCabBanco, false).GetComponent<Renderer>();

            tituloExplicacion3D = MakeText("Titulo_Exp", expCard, font, new Vector2(0, 12.5f), new Vector2(516, 12), 36, TextAlignmentOptions.MidlineLeft, Color.white, true);
            cuerpoExplicacion3D = MakeText("Cuerpo_Exp", expCard, font, new Vector2(0, -6.5f), new Vector2(516, 22), 30, TextAlignmentOptions.MidlineLeft, new Color(0.90f, 0.96f, 0.94f), false);

            // ==================================================================
            // 3. TARJETA 3D CENTRAL (ORDEN BANCARIA SWIFT MT103)
            // ==================================================================
            var centerCard = new GameObject("Tarjeta_Central_Orden").transform;
            centerCard.SetParent(mm, false);
            centerCard.localPosition = new Vector3(0, 0, 0);

            marco = MakeBox("Marco", centerCard, new Vector3(0, 0, 1.5f), new Vector3(200, 126, 2), matEstado, false).GetComponent<Renderer>();
            var fondoCentral = MakeBox("Fondo", centerCard, Vector3.zero, new Vector3(194, 120, 3), matFondo, true);
            var hitCentral = fondoCentral.AddComponent<Interactive3DCard>();
            hitCentral.parentStage = this;
            hitCentral.action = Interactive3DCard.CardAction.StepCycle;

            franjaSuperiorCentral = MakeBox("Cabecera", centerCard, new Vector3(0, 48, -1.7f), new Vector3(194, 24, 1), matCabBanco, false).GetComponent<Renderer>();
            // Cajas internas para dar profundidad estética a cada campo
            MakeBox("Chip_Monto", centerCard, new Vector3(0, 15, -1.6f), new Vector3(182, 20, 0.8f), matSubpanel, false);
            MakeBox("Chip_Benef", centerCard, new Vector3(0, -7, -1.6f), new Vector3(182, 20, 0.8f), matSubpanel, false);
            MakeBox("Chip_Dest", centerCard, new Vector3(0, -29, -1.6f), new Vector3(182, 20, 0.8f), matSubpanel, false);

            insigniaEstado = MakeBox("Insignia_Estado", centerCard, new Vector3(-50, -49, -1.8f), new Vector3(84, 15, 1.2f), matEstado, false).GetComponent<Renderer>();

            titulo = MakeText("Titulo", centerCard, font, new Vector2(-2, 48), new Vector2(182, 20), 62, TextAlignmentOptions.MidlineLeft, Color.white, true);
            MakeText("Banco", centerCard, font, new Vector2(-2, 48), new Vector2(182, 20), 40, TextAlignmentOptions.MidlineRight, new Color(0.45f, 0.88f, 1f), true).text = "BANCO ANDINO";
            referencia = MakeText("Referencia", centerCard, font, new Vector2(0, 31), new Vector2(182, 10), 38, TextAlignmentOptions.MidlineLeft, gris, false);

            MakeText("L_Monto", centerCard, font, new Vector2(-60, 15), new Vector2(54, 14), 40, TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.85f, 0.98f), true).text = "MONTO (:32A:)";
            monto = MakeText("Monto", centerCard, font, new Vector2(28, 15), new Vector2(120, 18), 86, TextAlignmentOptions.MidlineLeft, ColOro, true);

            MakeText("L_Beneficiario", centerCard, font, new Vector2(-60, -7), new Vector2(54, 14), 40, TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.85f, 0.98f), true).text = "BENEF. (:59:)";
            beneficiario = MakeText("Beneficiario", centerCard, font, new Vector2(28, -7), new Vector2(120, 18), 52, TextAlignmentOptions.MidlineLeft, Color.white, true);

            MakeText("L_Destino", centerCard, font, new Vector2(-60, -29), new Vector2(54, 14), 40, TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.85f, 0.98f), true).text = "DESTINO (:57A:)";
            destino = MakeText("Destino", centerCard, font, new Vector2(28, -29), new Vector2(120, 18), 48, TextAlignmentOptions.MidlineLeft, Color.white, true);

            estado = MakeText("Estado", centerCard, font, new Vector2(-50, -49), new Vector2(80, 14), 46, TextAlignmentOptions.Center, new Color(0.03f, 0.05f, 0.07f), true);
            estado.transform.localPosition += new Vector3(0, 0, -0.6f);
            nota = MakeText("Nota", centerCard, font, new Vector2(42, -49), new Vector2(96, 16), 33, TextAlignmentOptions.MidlineLeft, gris, false);

            // ==================================================================
            // 4. COLUMNA IZQUIERDA: 3 TARJETAS 3D DE ANÁLISIS (VULNERABILIDADES)
            // ==================================================================
            var leftCol = new GameObject("Columna_Izquierda_Analisis").transform;
            leftCol.SetParent(mm, false);
            leftCol.localPosition = new Vector3(-188, 0, 0);
            grupoAnalisis3D = leftCol;

            MakeBox("Marco_Cab_Analisis", leftCol, new Vector3(0, 54, 0.8f), new Vector3(166, 18, 2), matEstado, false);
            MakeBox("Cab_Analisis", leftCol, new Vector3(0, 54, 0), new Vector3(162, 15, 3), matCabVuln, false);
            MakeText("Txt_Cab_Analisis", leftCol, font, new Vector2(0, 54), new Vector2(154, 13), 36,
                TextAlignmentOptions.Center, ColAmbar, true).text = "5 · ANÁLISIS (3 FALLAS MT103)";

            Interactive3DCard.CardAction[] accionesItems = {
                Interactive3DCard.CardAction.ToggleItem0,
                Interactive3DCard.CardAction.ToggleItem1,
                Interactive3DCard.CardAction.ToggleItem2
            };

            marcosVuln = new Renderer[3];
            acentosVuln = new Renderer[3];
            fondosVuln = new Renderer[3];
            textosVuln = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 24f - i * 39f;
                MakeSideCard($"Tarjeta_Vuln_{i + 1}", leftCol, new Vector3(0, y, 0), new Vector3(162, 33, 3),
                    matEstado, matFondo, font, accionesItems[i],
                    out marcosVuln[i], out acentosVuln[i], out fondosVuln[i], out textosVuln[i]);
            }

            // ==================================================================
            // 5. COLUMNA DERECHA: 3 TARJETAS 3D DE DEFENSA (CONTROLES TÉCNICOS)
            // ==================================================================
            var rightCol = new GameObject("Columna_Derecha_Defensa").transform;
            rightCol.SetParent(mm, false);
            rightCol.localPosition = new Vector3(188, 0, 0);
            grupoDefensa3D = rightCol;

            MakeBox("Marco_Cab_Defensa", rightCol, new Vector3(0, 54, 0.8f), new Vector3(166, 18, 2), matEstado, false);
            MakeBox("Cab_Defensa", rightCol, new Vector3(0, 54, 0), new Vector3(162, 15, 3), matCabDef, false);
            MakeText("Txt_Cab_Defensa", rightCol, font, new Vector2(0, 54), new Vector2(154, 13), 36,
                TextAlignmentOptions.Center, ColVerde, true).text = "6 · DEFENSA (3 CONTROLES)";

            marcosDef = new Renderer[3];
            acentosDef = new Renderer[3];
            fondosDef = new Renderer[3];
            textosDef = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 24f - i * 39f;
                MakeSideCard($"Tarjeta_Def_{i + 1}", rightCol, new Vector3(0, y, 0), new Vector3(162, 33, 3),
                    matEstado, matFondo, font, accionesItems[i],
                    out marcosDef[i], out acentosDef[i], out fondosDef[i], out textosDef[i]);
            }

            // ==================================================================
            // 6. TARJETA 3D INFERIOR: RUTA SWIFT ANIMADA + LAS 36 ÓRDENES
            // ==================================================================
            var bottomCard = new GameObject("Tarjeta_Inferior_Simulacion").transform;
            bottomCard.SetParent(mm, false);
            bottomCard.localPosition = new Vector3(0, -96, 0);

            marcoFlujo3D = MakeBox("Marco_Flujo", bottomCard, new Vector3(0, 0, 1.5f), new Vector3(540, 58, 2), matEstado, false).GetComponent<Renderer>();
            var fondoFlujo = MakeBox("Fondo_Flujo", bottomCard, Vector3.zero, new Vector3(534, 52, 3), matFondo, true);
            var hitFlujo = fondoFlujo.AddComponent<Interactive3DCard>();
            hitFlujo.parentStage = this;
            hitFlujo.action = Interactive3DCard.CardAction.StepCycle;

            // Subpanel oscuro para la pista de red SWIFT
            MakeBox("Pista_Fondo", bottomCard, new Vector3(-72, -1, -1.6f), new Vector3(356, 30, 0.8f), matSubpanel, false);

            tituloFlujo = MakeText("Titulo_Flujo", bottomCard, font, new Vector2(-72, 19), new Vector2(356, 10),
                34, TextAlignmentOptions.MidlineLeft, Color.white, true);
            explicacionFlujo = MakeText("Explicacion_Flujo", bottomCard, font, new Vector2(-72, -20), new Vector2(356, 10),
                29, TextAlignmentOptions.MidlineLeft, gris, false);

            var rutaRoot = new GameObject("Ruta_SWIFT_Animada").transform;
            rutaRoot.SetParent(bottomCard, false);
            rutaRoot.localPosition = new Vector3(-72, 2, -2.2f);

            MakeBox("Cable_Ruta", rutaRoot, Vector3.zero, new Vector3(260, 2.2f, 0.8f), matLinea, false);

            string[] nombresNodos = { "BANCO ANDINO\n(Mesa SWIFT)", "CORRESPONSAL NY\n(Filtro Sanciones)", "MANILA · RCBC\n(Ag. Las Palmeras)" };
            float[] xNodos = { -130f, 0f, 130f };
            nodosRuta = new Renderer[3];
            for (int n = 0; n < 3; n++)
            {
                var nodo = MakeBox($"Nodo_{n}", rutaRoot, new Vector3(xNodos[n], 0, -0.6f), new Vector3(14, 9, 1.8f), matEstado, false);
                nodosRuta[n] = nodo.GetComponent<Renderer>();
                MakeText($"Lbl_Nodo_{n}", rutaRoot, font, new Vector2(xNodos[n], -10.5f), new Vector2(84, 9),
                    24, TextAlignmentOptions.Center, new Color(0.85f, 0.95f, 1f), true).text = nombresNodos[n];
            }

            // Paquete SWIFT viajero proporcionado y nítido
            var pkt = MakeBox("Paquete_SWIFT_MT103", rutaRoot, new Vector3(-130, 0, -1.6f), new Vector3(22, 8.5f, 2.2f), matEstado, false);
            paqueteViajero = pkt.transform;
            paqueteRenderer = pkt.GetComponent<Renderer>();
            var lblPkt = MakeText("Lbl_Pkt", pkt.transform, font, Vector2.zero, new Vector2(0.88f, 0.80f),
                3.2f, TextAlignmentOptions.Center, Color.white, true);
            lblPkt.enableAutoSizing = false;
            lblPkt.fontSize = 3.2f;
            lblPkt.transform.localPosition = new Vector3(0, 0, -0.6f);
            lblPkt.text = "MT103";

            // Escudo barrera láser en modo Defensa
            var esc = MakeBox("Escudo_Barrera_Defensa", rutaRoot, new Vector3(-68, 0, -1.8f), new Vector3(4.5f, 22, 2.6f), matEstado, false);
            escudoBarrera = esc.transform;
            escudoRenderer = esc.GetComponent<Renderer>();

            // Grilla 6x6 de las 36 órdenes a la derecha de la tarjeta inferior
            var gridRoot = new GameObject("Matriz_36_Ordenes").transform;
            gridRoot.SetParent(bottomCard, false);
            gridRoot.localPosition = new Vector3(195, -2, -2.0f);

            MakeBox("Fondo_Matriz", gridRoot, new Vector3(0, -1, 0.5f), new Vector3(124, 44, 0.8f), matSubpanel, false);
            MakeText("Lbl_36", gridRoot, font, new Vector2(0, 19.5f), new Vector2(122, 8),
                25, TextAlignmentOptions.Center, ColOro, true).text = "36 ÓRDENES ($951M)";

            cuadros36 = new Renderer[36];
            int k = 0;
            for (int r = 0; r < 6; r++)
            {
                for (int c = 0; c < 6; c++)
                {
                    float gx = (c - 2.5f) * 16.5f;
                    float gy = (2.5f - r) * 5.4f - 2.5f;
                    var sq = MakeBox($"OrdSq_{k + 1:00}", gridRoot, new Vector3(gx, gy, 0), new Vector3(13.5f, 4.0f, 1.4f), matEstado, false);
                    cuadros36[k] = sq.GetComponent<Renderer>();
                    k++;
                }
            }
        }

        void Make3DTab(string name, Transform parent, Vector3 pos, Vector3 size,
                       Material matMarco, Material matFondo, TMP_FontAsset font, string label,
                       Interactive3DCard.CardAction action, out Renderer marcoOut, out Renderer fondoOut)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;

            marcoOut = MakeBox("Marco", root, new Vector3(0, 0, 1.0f), size + new Vector3(3.5f, 3.5f, 0), matMarco, false).GetComponent<Renderer>();
            var fondo = MakeBox("Fondo", root, Vector3.zero, size, matFondo, true);
            fondoOut = fondo.GetComponent<Renderer>();
            var hit = fondo.AddComponent<Interactive3DCard>();
            hit.parentStage = this;
            hit.action = action;

            MakeText("Label", root, font, Vector2.zero, new Vector2(size.x - 10, size.y - 4), 38,
                TextAlignmentOptions.Center, Color.white, true).text = label;
        }

        void MakeSideCard(string name, Transform parent, Vector3 pos, Vector3 size,
                          Material matMarco, Material matFondo, TMP_FontAsset font,
                          Interactive3DCard.CardAction action,
                          out Renderer marcoRenderer, out Renderer acentoRenderer, out Renderer fondoRenderer, out TMP_Text bodyText)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;

            marcoRenderer = MakeBox("Marco", root, new Vector3(0, 0, 1.2f), size + new Vector3(3.5f, 3.5f, 0), matMarco, false).GetComponent<Renderer>();
            var fondo = MakeBox("Fondo", root, Vector3.zero, size, matFondo, true);
            fondoRenderer = fondo.GetComponent<Renderer>();
            var hit = fondo.AddComponent<Interactive3DCard>();
            hit.parentStage = this;
            hit.action = action;

            // Barra vertical de acento neón a la izquierda de cada tarjeta
            acentoRenderer = MakeBox("Barra_Acento", root, new Vector3(-size.x * 0.5f + 3f, 0, -1.7f), new Vector3(4.5f, size.y - 2f, 1f), matMarco, false).GetComponent<Renderer>();

            bodyText = MakeText("Texto", root, font, new Vector2(3f, 0), new Vector2(size.x - 16, size.y - 5), 32,
                TextAlignmentOptions.MidlineLeft, Color.white, false);
        }

        static GameObject MakeBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!keepCollider) DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, Vector2 pos, Vector2 size,
                                 float fontSize, TextAlignmentOptions align, Color color, bool bold)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshPro>();
            if (font) t.font = font;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = size;
            rt.localPosition = new Vector3(pos.x, pos.y, -2.2f);
            t.fontSize = fontSize;
            t.enableAutoSizing = true;
            t.fontSizeMin = fontSize * 0.45f;
            t.fontSizeMax = fontSize;
            t.alignment = align;
            t.color = color;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.richText = true;
            t.text = name;
            return t;
        }

        public void AdvanceCycleStep()
        {
            var gm = GameManager.I;
            if (gm == null) return;

            if (gm.phase == SimPhase.Ataque)
            {
                gm.SetPhase(SimPhase.Analisis);
            }
            else if (gm.phase == SimPhase.Analisis)
            {
                if (!gm.IsVulnerabilityAnalyzed(GameManager.VULN_LIMITES)) gm.ToggleVulnerability(GameManager.VULN_LIMITES);
                else if (!gm.IsVulnerabilityAnalyzed(GameManager.VULN_DOBLE_FIRMA)) gm.ToggleVulnerability(GameManager.VULN_DOBLE_FIRMA);
                else if (!gm.IsVulnerabilityAnalyzed(GameManager.VULN_BENEFICIARIO)) gm.ToggleVulnerability(GameManager.VULN_BENEFICIARIO);
                else gm.SetPhase(SimPhase.Defensa);
            }
            else
            {
                if (!gm.IsDefenseActive(GameManager.DEF_LIMITES)) gm.ToggleDefense(GameManager.DEF_LIMITES);
                else if (!gm.IsDefenseActive(GameManager.DEF_DOBLE_FIRMA)) gm.ToggleDefense(GameManager.DEF_DOBLE_FIRMA);
                else if (!gm.IsDefenseActive(GameManager.DEF_LISTA_BLANCA)) gm.ToggleDefense(GameManager.DEF_LISTA_BLANCA);
                else
                {
                    gm.ToggleDefense(GameManager.DEF_LIMITES);
                    gm.ToggleDefense(GameManager.DEF_DOBLE_FIRMA);
                    gm.ToggleDefense(GameManager.DEF_LISTA_BLANCA);
                    gm.SetPhase(SimPhase.Ataque);
                }
            }
        }

        public void RefreshVisuals()
        {
            if (_data == null) return;
            var gm = GameManager.I;
            var phase = gm != null ? gm.phase : SimPhase.Ataque;
            bool defensaActiva = gm != null && gm.mode == SimMode.Defensa && gm.Defensas.Count > 0;

            bool v1 = gm != null && gm.IsVulnerabilityAnalyzed(GameManager.VULN_LIMITES);
            bool v2 = gm != null && gm.IsVulnerabilityAnalyzed(GameManager.VULN_DOBLE_FIRMA);
            bool v3 = gm != null && gm.IsVulnerabilityAnalyzed(GameManager.VULN_BENEFICIARIO);

            bool c1 = gm != null && gm.IsDefenseActive(GameManager.DEF_LIMITES);
            bool c2 = gm != null && gm.IsDefenseActive(GameManager.DEF_DOBLE_FIRMA);
            bool c3 = gm != null && gm.IsDefenseActive(GameManager.DEF_LISTA_BLANCA);

            // --- 1. Pestañas 3D superiores con alto contraste
            SetColor(tab3DAtaque, phase == SimPhase.Ataque ? ColRojo : ColBordeInactivo);
            SetColor(fondoTabAtaque, phase == SimPhase.Ataque ? ColRojoFondoActivo : ColFondoOscuro);

            SetColor(tab3DAnalisis, phase == SimPhase.Analisis ? ColAmbar : ColBordeInactivo);
            SetColor(fondoTabAnalisis, phase == SimPhase.Analisis ? ColAmbarFondoActivo : ColFondoOscuro);

            SetColor(tab3DDefensa, phase == SimPhase.Defensa ? ColVerde : ColBordeInactivo);
            SetColor(fondoTabDefensa, phase == SimPhase.Defensa ? ColVerdeFondoActivo : ColFondoOscuro);

            // --- 2. Tarjeta 3D de Explicación Técnica (Puntos 4, 5 y 6)
            if (phase == SimPhase.Ataque)
            {
                SetColor(marcoExplicacion3D, ColRojo);
                SetColor(cabExplicacion3D, ColRojoFondoActivo);
                if (tituloExplicacion3D)
                    tituloExplicacion3D.text = "<color=#ff645a>● 4. ATAQUE DE CIBERSEGURIDAD · ASÍ SUCEDIÓ EL ROBO SWIFT (4-5 FEB 2016)</color>";
                if (cuerpoExplicacion3D)
                    cuerpoExplicacion3D.text =
                        "Lazarus inició sesión a las 20:36 h, parcheó 2 bytes de <color=#ffc24a>gp.ocx</color> para apagar la impresora y emitió <b>36 órdenes MT103 por US$ 951M</b>.\n" +
                        "Resultado real: <color=#42c7ff>30 bloqueadas</color> por alerta «JUPITER», <color=#ff5a4e>4 PAGADAS a Manila (US$ 81M robados)</color> y <color=#ffc24a>1 detenida por «Fandation» ($20M)</color>.";
            }
            else if (phase == SimPhase.Analisis)
            {
                int nV = (v1 ? 1 : 0) + (v2 ? 1 : 0) + (v3 ? 1 : 0);
                SetColor(marcoExplicacion3D, ColAmbar);
                SetColor(cabExplicacion3D, ColAmbarFondoActivo);
                if (tituloExplicacion3D)
                    tituloExplicacion3D.text = $"<color=#ffd25a>● 5. ANÁLISIS TÉCNICO DE CIBERSEGURIDAD · AUDITORÍA FORENSE MT103 ({nV}/3 FALLAS)</color>";
                if (cuerpoExplicacion3D)
                    cuerpoExplicacion3D.text =
                        "¿Por qué salió la Orden 01? <b>(1) Campo :32A:</b> sin tope de monto ni corte nocturno (20:58 h), <b>(2) Sin Maker-Checker</b> (1 sola credencial\n" +
                        "comprometida aprobó el envío), y <b>(3) Campo :59:</b> sin lista blanca KYC/AML ni filtro léxico. <color=#ffd25a>Toca las 3 tarjetas izquierdas.</color>";
            }
            else
            {
                int nC = (c1 ? 1 : 0) + (c2 ? 1 : 0) + (c3 ? 1 : 0);
                SetColor(marcoExplicacion3D, defensaActiva ? ColVerde : ColAmbar);
                SetColor(cabExplicacion3D, defensaActiva ? ColVerdeFondoActivo : ColAmbarFondoActivo);
                if (tituloExplicacion3D)
                    tituloExplicacion3D.text = $"<color=#38f28a>● 6. DEFENSA TÉCNICA DE CIBERSEGURIDAD · MOTOR PRE-TRANSMISIÓN ({nC}/3 ACTIVOS)</color>";
                if (cuerpoExplicacion3D)
                    cuerpoExplicacion3D.text =
                        "Contramedidas técnicas: <b>(1) Regla de monto/horario</b> (bloquea > US$ 5M o fuera de 08–18 h), <b>(2) Doble Aprobación Mancomunada</b> (4 ojos + MFA),\n" +
                        "y <b>(3) Lista blanca KYC + distancia Levenshtein</b>. <color=#38f28a>Toca las 3 tarjetas derechas para retener las 36 órdenes (US$ 0 robados).</color>";
            }

            // --- 3. Tarjeta Central (Orden 01)
            SetColor(franjaSuperiorCentral, ColCabBanco);
            if (titulo)
            {
                string tagFase = phase == SimPhase.Ataque ? "<color=#ff5a4e>[ATAQUE]</color>"
                               : phase == SimPhase.Analisis ? "<color=#ffc24a>[ANÁLISIS]</color>"
                               : "<color=#38f28a>[DEFENSA]</color>";
                titulo.text = $"ORDEN {_data.numero:00}/{_totalOrdenes} {tagFase}";
            }

            if (referencia) referencia.text = $"SWIFT MT103 · Ref. {_data.referenciaSwift} · {_data.fechaHora}";

            if (monto)
            {
                if (phase == SimPhase.Analisis && v1)
                    monto.text = $"<color=#ff5a4e>{_data.MontoFormateado} (!SIN TOPE)</color>";
                else if (defensaActiva && c1)
                    monto.text = $"<color=#38f28a>{_data.MontoFormateado} (RETENIDA)</color>";
                else
                    monto.text = _data.MontoFormateado;
            }

            if (beneficiario)
            {
                string benTxt = (phase == SimPhase.Analisis && v3)
                    ? $"<color=#ffc24a>{_data.beneficiario} (!SIN KYC)</color>"
                    : _data.beneficiario;
                beneficiario.text = $"{benTxt}\n<size=72%><color=#88c0d0>Cta. {_data.cuentaBeneficiario}</color></size>";
            }

            if (destino)
            {
                string firmaTag = (phase == SimPhase.Analisis && v2) ? " <color=#ff5a4e>[!1 FIRMA]</color>" : "";
                destino.text = $"{_data.destino}{firmaTag}\n<size=72%><color=#88c0d0>{_data.pais}</color></size>";
            }

            OrderStatus st = _data.estadoReal;
            string motivo = _data.notaEstado;
            if (gm != null)
                st = gm.EvaluateOrderStatus(_data, out motivo);

            if (phase == SimPhase.Analisis)
            {
                int nVuln = gm != null ? gm.VulnerabilidadesAnalizadas.Count : 0;
                if (estado) estado.text = $"AUDITORÍA ({nVuln}/3)";
                if (nota) nota.text = "Toca las tarjetas izquierdas para\nmarcar cada vulnerabilidad técnica.";
                _colorEstado = ColAmbar;
            }
            else
            {
                if (estado) estado.text = OrderData.EstadoTexto(st);
                if (nota) nota.text = motivo;
                _colorEstado = OrderData.EstadoColor(st);
            }

            SetColor(marco, _colorEstado);
            SetColor(insigniaEstado, _colorEstado);

            // --- 4. Tarjetas 3D Izquierdas (Vulnerabilidades técnicas)
            UpdateVulnCard(0, v1, phase == SimPhase.Analisis,
                "FALLA 1 · CAMPO :32A: Y HORA",
                "Sin límite de monto ni bloqueo nocturno:\nUS$ 30M emitidos a las 20:58 h.");
            UpdateVulnCard(1, v2, phase == SimPhase.Analisis,
                "FALLA 2 · SIN DOBLE FIRMA",
                "1 sola credencial comprometida envió el\nMT103 (sin control Maker-Checker / 4 ojos).");
            UpdateVulnCard(2, v3, phase == SimPhase.Analisis,
                "FALLA 3 · CAMPO :59: SIN LISTA BLANCA",
                "Beneficiario inédito en Manila y nulo filtro\nléxico («Shalika Fandation»).");

            // --- 5. Tarjetas 3D Derechas (Controles técnicos de Defensa)
            UpdateDefCard(0, c1, phase == SimPhase.Defensa,
                "CONTROL 1 · LÍMITES Y HORARIO",
                "Retiene pagos > US$ 5M o fuera de\nventana operativa (08:00 a 18:00 h).");
            UpdateDefCard(1, c2, phase == SimPhase.Defensa,
                "CONTROL 2 · DOBLE APROBACIÓN + MFA",
                "Exige Operador 1 (crea) + Autorizador 2\ncon token físico antes de transmitir.");
            UpdateDefCard(2, c3, phase == SimPhase.Defensa,
                "CONTROL 3 · LISTA BLANCA + LÉXICO",
                "Solo cuentas pre-autorizadas KYC/AML y\ndistancia Levenshtein en nombres.");

            // --- 6. Tarjeta 3D Inferior: Flujo animado + 36 órdenes
            SetColor(marcoFlujo3D, defensaActiva ? ColVerde : ColCian);
            if (escudoBarrera) escudoBarrera.gameObject.SetActive(defensaActiva);
            if (escudoRenderer) SetColor(escudoRenderer, ColVerde);

            if (tituloFlujo)
            {
                if (defensaActiva)
                    tituloFlujo.text = "<color=#38f28a>● SIMULACIÓN EN VIVO (DEFENSA ACTIVA):</color> Barrera SWIFT retiene las 36 órdenes · <b>US$ 0 ROBADOS</b>";
                else
                    tituloFlujo.text = "<color=#ff5a4e>● SIMULACIÓN EN VIVO (ATAQUE 2016):</color> <color=#42c7ff>30 bloqueadas</color> · <color=#ff5a4e>4 pagadas ($81M)</color> · <color=#ffc24a>1 «Fandation» ($20M)</color>";
            }

            if (explicacionFlujo)
            {
                explicacionFlujo.text = defensaActiva
                    ? "El mensaje MT103 choca contra el escudo de reglas en el BANCO ANDINO y no sale a la red SWIFT."
                    : "El mensaje MT103 viaja de Banco Andino a Corresponsal NY y acredita US$ 30M en Manila.";
            }

            for (int i = 0; i < cuadros36.Length; i++)
            {
                if (!cuadros36[i]) continue;
                Color col;
                if (defensaActiva) col = ColVerde;
                else if (i < 4) col = ColRojo;
                else if (i == 4) col = ColAmbar;
                else if (i == 5) col = ColGris;
                else col = ColCian;
                SetColor(cuadros36[i], col);
            }
        }

        void UpdateVulnCard(int idx, bool auditada, bool enFaseAnalisis, string cab, string det)
        {
            if (idx < marcosVuln.Length && marcosVuln[idx])
                SetColor(marcosVuln[idx], auditada ? ColAmbar : (enFaseAnalisis ? new Color(0.65f, 0.45f, 0.14f) : ColBordeInactivo));
            if (idx < acentosVuln.Length && acentosVuln[idx])
                SetColor(acentosVuln[idx], auditada ? ColAmbar : new Color(0.55f, 0.35f, 0.10f));
            if (idx < fondosVuln.Length && fondosVuln[idx])
                SetColor(fondosVuln[idx], auditada ? ColAmbarFondoActivo : ColFondoTarjeta);
            if (idx < textosVuln.Length && textosVuln[idx])
            {
                string estadoTag = auditada ? "<color=#ffd24a>[AUDITADA ✓]</color>" : "<color=#7fa6b8>[TOCAR]</color>";
                textosVuln[idx].text = $"<color=#ffd24a><b>{cab}</b></color> {estadoTag}\n<color=#dceae4>{det}</color>";
            }
        }

        void UpdateDefCard(int idx, bool activo, bool enFaseDefensa, string cab, string det)
        {
            if (idx < marcosDef.Length && marcosDef[idx])
                SetColor(marcosDef[idx], activo ? ColVerde : (enFaseDefensa ? new Color(0.16f, 0.55f, 0.36f) : ColBordeInactivo));
            if (idx < acentosDef.Length && acentosDef[idx])
                SetColor(acentosDef[idx], activo ? ColVerde : new Color(0.12f, 0.42f, 0.28f));
            if (idx < fondosDef.Length && fondosDef[idx])
                SetColor(fondosDef[idx], activo ? ColVerdeFondoActivo : ColFondoTarjeta);
            if (idx < textosDef.Length && textosDef[idx])
            {
                string estadoTag = activo ? "<color=#38f28a>[ACTIVO ✓]</color>" : "<color=#7fa6b8>[TOCAR]</color>";
                textosDef[idx].text = $"<color=#38f28a><b>{cab}</b></color> {estadoTag}\n<color=#dceae4>{det}</color>";
            }
        }

        void SetColor(Renderer r, Color c)
        {
            if (!r) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);
            r.SetPropertyBlock(_mpb);
        }

        public void Show()
        {
            _visible = true;
            RefreshVisuals();
            Play(AnimarEscala(transform.localScale, _escalaBase, duracionAparicion, true));
        }

        public void Hide()
        {
            _visible = false;
            Play(AnimarEscala(transform.localScale, Vector3.zero, duracionDesaparicion, false));
        }

        void Play(IEnumerator rutina)
        {
            if (!isActiveAndEnabled) return;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(rutina);
        }

        IEnumerator AnimarEscala(Vector3 desde, Vector3 hasta, float duracion, bool rebote)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duracion);
                float k = rebote ? EaseOutBack(Mathf.Clamp01(t)) : Mathf.Clamp01(t) * Mathf.Clamp01(t);
                transform.localScale = Vector3.LerpUnclamped(desde, hasta, k);
                yield return null;
            }
            transform.localScale = hasta;
            _anim = null;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        void Update()
        {
            if (!_visible) return;

            if (PointerPressedThisFrame(out Vector2 screenPos) && Camera.main != null)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(screenPos);
                    if (Physics.Raycast(ray, out RaycastHit hit, 20f))
                    {
                        var card3D = hit.collider.GetComponent<Interactive3DCard>();
                        if (card3D != null) card3D.Execute();
                    }
                }
            }
        }

        static bool PointerPressedThisFrame(out Vector2 pos)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pos = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pos = Mouse.current.position.ReadValue();
                return true;
            }
            pos = Vector2.zero;
            return false;
        }

        void LateUpdate()
        {
            if (!_visible) return;

            // 1. Siempre derecho frente a la cámara
            if (mantenerDerechoFrenteACamara && Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            // 2. Flotado suave
            float s = Mathf.Sin(Time.time * velocidadFlotado);
            transform.localPosition = _posBase + Vector3.up * (s * amplitudFlotado);

            // 3. Pulso de la insignia de estado
            if (insigniaEstado)
            {
                float p = 0.80f + 0.20f * (0.5f + 0.5f * Mathf.Sin(Time.time * 4f));
                SetColor(insigniaEstado, _colorEstado * p + new Color(0, 0, 0, 1));
            }

            // 4. Animación del paquete MT103 y onda luminosa en las 36 órdenes
            var gm = GameManager.I;
            bool defensaActiva = gm != null && gm.mode == SimMode.Defensa && gm.Defensas.Count > 0;
            _recorridoT = (_recorridoT + Time.deltaTime * 0.50f) % 1f;

            if (paqueteViajero)
            {
                float x;
                if (defensaActiva)
                {
                    float k = Mathf.PingPong(_recorridoT * 2.2f, 1f);
                    x = Mathf.Lerp(-130f, -76f, k);
                    if (paqueteRenderer) SetColor(paqueteRenderer, ColVerde);
                }
                else
                {
                    x = Mathf.Lerp(-130f, 130f, _recorridoT);
                    if (paqueteRenderer) SetColor(paqueteRenderer, ColRojo);
                }
                var lp = paqueteViajero.localPosition;
                paqueteViajero.localPosition = new Vector3(x, lp.y, lp.z);
            }

            if (nodosRuta != null && nodosRuta.Length >= 3)
            {
                SetColor(nodosRuta[0], ColCian);
                SetColor(nodosRuta[1], defensaActiva ? ColVerde : ColAmbar);
                SetColor(nodosRuta[2], defensaActiva ? ColGris : ColRojo);
            }
        }
    }
}
