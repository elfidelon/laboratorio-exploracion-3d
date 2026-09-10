using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

/// <summary>
/// Configura la escena completa con un clic: componentes, colliders,
/// referencias de interfaz y el cubo de meta.
/// Menu: Examen > Configurar escena automaticamente
/// </summary>
public static class ConfiguradorEscena
{
    [MenuItem("Examen/Configurar escena automaticamente")]
    public static void Configurar()
    {
        var escena = SceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();

        GameObject jugador = raices.FirstOrDefault(g => g.name == "Sphere");
        if (jugador == null)
        {
            EditorUtility.DisplayDialog("Configurador",
                "No encontre el objeto 'Sphere' en la escena.", "Ok");
            return;
        }

        ConfigurarJugador(jugador);
        var gm = ConfigurarGameManager();
        int botellas = ConfigurarBotellas(raices);
        ConfigurarPuerta(raices);
        int plataformas = ConfigurarPlataformas(raices);
        ConfigurarMeta(raices, jugador);
        ConfigurarCamara(raices, jugador);
        ConfigurarInterfaz(raices, gm);

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        Debug.Log($"[Configurador] Listo. Botellas: {botellas}. Plataformas moviles: {plataformas}.");
        EditorUtility.DisplayDialog("Configurador",
            $"Escena configurada.\n\nBotellas recolectables: {botellas}\nPlataformas moviles: {plataformas}\n\nYa puedes darle Play.", "Ok");
    }

    [MenuItem("Examen/Acomodar botellas flotantes")]
    public static void AcomodarBotellas()
    {
        var escena = SceneManager.GetActiveScene();
        var raices = escena.GetRootGameObjects();

        // Superficies sobre las que se puede parar la esfera.
        var plataformas = raices
            .Where(g =>
            {
                string n = g.name.ToLower();
                return n.StartsWith("piso") || n.Contains("movible") || n.Contains("movil") || n == "meta";
            })
            .Select(g => g.GetComponent<Renderer>())
            .Where(r => r != null)
            .ToArray();

        if (plataformas.Length == 0) return;

        int movidas = 0;
        const float alturaSobrePiso = 0.56f;

        foreach (var bot in Object.FindObjectsByType<Coleccionable>(FindObjectsInactive.Include))
        {
            Vector3 pos = bot.transform.position;

            Renderer mejor = null;
            Vector3 mejorPunto = Vector3.zero;
            float mejorDist = float.MaxValue;

            foreach (var plat in plataformas)
            {
                Bounds b = plat.bounds;

                // El punto de la superficie superior mas cercano a la botella.
                Vector3 punto = new Vector3(
                    Mathf.Clamp(pos.x, b.min.x, b.max.x),
                    b.max.y + alturaSobrePiso,
                    pos.z);

                float d = Vector3.Distance(pos, punto);
                if (d < mejorDist)
                {
                    mejorDist = d;
                    mejor = plat;
                    mejorPunto = punto;
                }
            }

            // Si ya esta bien parada sobre un piso, no la tocamos.
            if (mejor == null || mejorDist < 1.5f) continue;

            Undo.RecordObject(bot.transform, "Acomodar botella");
            bot.transform.position = mejorPunto;
            movidas++;

            Debug.Log($"[Configurador] {bot.name} movida de {pos} a {mejorPunto} (sobre {mejor.name})");
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        EditorUtility.DisplayDialog("Configurador",
            movidas == 0
                ? "Todas las botellas ya estaban sobre un piso."
                : $"Se acomodaron {movidas} botellas que estaban flotando en el aire.\n\nRevisa la consola para ver cuales.",
            "Ok");
    }

    static void ConfigurarJugador(GameObject jugador)
    {
        jugador.tag = "Player";

        var rb = Obtener<Rigidbody>(jugador);
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        // Sin esto la esfera rueda sola y la camara se marea.
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        var col = jugador.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        var pc = Obtener<PlayerController>(jugador);
        pc.velocidad = 6f;
        // El salto mas alto del nivel es de 4.2 m (de piso (10) a piso movible 2).
        // Con gravedad x2, una velocidad de 15 da unos 5.7 m de altura: alcanza con margen.
        pc.fuerzaSalto = 15f;
        pc.multiplicadorGravedad = 2f;
        pc.distanciaChequeoSuelo = 0.5f;
        pc.radioChequeoSuelo = 0.35f;

        var fuenteJugador = Obtener<AudioSource>(jugador);
        fuenteJugador.playOnAwake = false;
        // El de la esfera ya quedo bien, solo lo ponemos si esta vacio.
        if (pc.sonidoSalto == null) pc.sonidoSalto = CargarClip("salto.wav");
    }

    static GameManager ConfigurarGameManager()
    {
        var existente = Object.FindAnyObjectByType<GameManager>();
        GameObject go = existente != null ? existente.gameObject : new GameObject("GameManager");
        var gm = Obtener<GameManager>(go);

        var fuente = Obtener<AudioSource>(go);
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f; // 2D: se oye igual de fuerte en todo el nivel

        // El sonido de recolectar vive aqui, en un solo lugar, y no en cada
        // botella. Cada Coleccionable tiene su propio slot por si quieres que
        // alguna suene distinto.
        if (SePuedeReemplazar(gm.sonidoRecolectar))
        {
            gm.sonidoRecolectar = BuscarClip("papoi") ?? CargarClip("recolectar.wav");
            PrepararParaSonarSeguido(gm.sonidoRecolectar);
        }

        EditorUtility.SetDirty(gm);
        return gm;
    }

    static int ConfigurarBotellas(GameObject[] raices)
    {
        int contador = 0;

        foreach (var go in raices)
        {
            if (!go.name.StartsWith("corona")) continue;

            // El FBX se importo sin colliders, asi que le calculamos uno
            // a partir de lo que realmente ocupa la malla en pantalla.
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;

            Bounds mundo = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) mundo.Encapsulate(renderers[i].bounds);

            var box = Obtener<BoxCollider>(go);
            box.isTrigger = true;

            Vector3 escala = go.transform.lossyScale;
            Vector3 tamanoLocal = new Vector3(
                Mathf.Abs(escala.x) > 0.0001f ? mundo.size.x / Mathf.Abs(escala.x) : mundo.size.x,
                Mathf.Abs(escala.y) > 0.0001f ? mundo.size.y / Mathf.Abs(escala.y) : mundo.size.y,
                Mathf.Abs(escala.z) > 0.0001f ? mundo.size.z / Mathf.Abs(escala.z) : mundo.size.z);

            box.center = go.transform.InverseTransformPoint(mundo.center);
            // Un poco mas ancho para que sea facil recolectarla al pasar.
            box.size = tamanoLocal * 1.4f;

            var col = Obtener<Coleccionable>(go);

            // El sonido vive en el GameManager. Si ademas esta lleno el de la
            // botella, se oirian los dos encimados.
            if (col.sonidoRecoleccion != null)
            {
                Debug.LogWarning(
                    $"[Configurador] {go.name} tiene su propio Sonido Recoleccion " +
                    $"('{col.sonidoRecoleccion.name}'). Vacialo si no quieres que " +
                    "se oiga encimado con el del GameManager.");
            }

            contador++;
        }

        return contador;
    }

    static void ConfigurarPuerta(GameObject[] raices)
    {
        var puerta = raices.FirstOrDefault(g => g.name == "puerta");
        if (puerta == null)
        {
            Debug.LogWarning("[Configurador] No encontre el objeto 'puerta'.");
            return;
        }

        var solido = puerta.GetComponent<Collider>();
        if (solido == null) solido = puerta.AddComponent<BoxCollider>();
        solido.isTrigger = false;

        var pt = Obtener<PuertaTrigger>(puerta);
        // El nivel corre sobre el eje X, asi que la puerta se retira hacia el
        // fondo (eje Z). Cambia direccionApertura en el Inspector si la giras.
        pt.direccionApertura = Vector3.forward;
        pt.distanciaApertura = Mathf.Max(4f, puerta.transform.lossyScale.z * 1.2f);
        pt.velocidadApertura = 4f;
        pt.cerrarAlSalir = false;
        Obtener<AudioSource>(puerta).playOnAwake = false;

        // Zona de deteccion: un hijo con trigger mas grande que la puerta.
        Transform zona = puerta.transform.Find("ZonaDeteccion");
        if (zona == null)
        {
            var nuevo = new GameObject("ZonaDeteccion");
            nuevo.transform.SetParent(puerta.transform, false);
            zona = nuevo.transform;
        }

        zona.localPosition = Vector3.zero;
        zona.localRotation = Quaternion.identity;
        zona.localScale = Vector3.one;

        var zonaBox = Obtener<BoxCollider>(zona.gameObject);
        zonaBox.isTrigger = true;

        Vector3 escala = puerta.transform.lossyScale;
        // 5 metros de radio de deteccion, convertidos a espacio local.
        zonaBox.size = new Vector3(
            1f + (escala.x > 0.0001f ? 10f / escala.x : 10f),
            1f,
            1f + (escala.z > 0.0001f ? 10f / escala.z : 10f));
        zonaBox.center = Vector3.zero;

        var zp = Obtener<ZonaPuerta>(zona.gameObject);
        zp.puerta = pt;
    }

    static void ConfigurarMeta(GameObject[] raices, GameObject jugador)
    {
        GameObject meta = raices.FirstOrDefault(g => g.name.ToLower() == "meta");

        if (meta == null)
        {
            var previa = Object.FindAnyObjectByType<Meta>();
            if (previa != null) meta = previa.gameObject;
        }

        if (meta == null)
        {
            Debug.LogWarning("[Configurador] No encontre el cubo llamado 'meta'.");
            return;
        }

        // Version vieja: la meta era una zona invisible flotando encima del cubo.
        // Ahora la meta es el cubo mismo, asi que esa zona sobra. La buscamos
        // en toda la escena porque pudo quedar suelta fuera del cubo.
        foreach (var sobrante in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (sobrante != null && sobrante.name == "ZonaMeta")
            {
                Debug.Log("[Configurador] Borrando ZonaMeta sobrante de la version anterior.");
                Object.DestroyImmediate(sobrante.gameObject);
            }
        }

        var col = meta.GetComponent<Collider>();
        if (col == null) col = meta.AddComponent<BoxCollider>();

        // Solido, no trigger: la esfera lo toca y se para encima.
        // Meta.cs responde tanto a colision como a trigger.
        col.isTrigger = false;

        LiberarPisosQueTapanLaMeta(raices, meta);

        var m = Obtener<Meta>(meta);
        m.mensajeIncompleto = "Llegaste a la meta!";
        m.mensajeCompleto = "Llegaste a la meta... eres un alcoholico!";
        m.congelarJugador = true;
        m.soloUnaVez = true;

        if (SePuedeReemplazar(m.sonidoMeta))
            m.sonidoMeta = BuscarClip("happy wheels", "victory", "happy") ?? CargarClip("meta.wav");

        var fuenteMeta = Obtener<AudioSource>(meta);
        fuenteMeta.playOnAwake = false;
        fuenteMeta.spatialBlend = 0f;

        EditorUtility.SetDirty(m);
    }

    /// <summary>
    /// El cubo de meta esta encajado dentro de una plataforma, asi que apenas
    /// sobresale y la esfera se queda parada en la plataforma sin llegar a
    /// pararse encima del cubo. Le apagamos el collider a esa plataforma: se
    /// sigue viendo, pero deja pasar, y el unico piso ahi arriba es la meta.
    /// </summary>
    static void LiberarPisosQueTapanLaMeta(GameObject[] raices, GameObject meta)
    {
        var colMeta = meta.GetComponent<Collider>();
        if (colMeta == null) return;

        Bounds zonaMeta = colMeta.bounds;

        foreach (var go in raices)
        {
            if (go == meta) continue;

            string n = go.name.ToLower();
            bool esPiso = n.StartsWith("piso") || n.Contains("movible") || n.Contains("movil");
            if (!esPiso) continue;

            var col = go.GetComponent<Collider>();
            if (col == null || !col.enabled) continue;

            if (!col.bounds.Intersects(zonaMeta)) continue;

            col.enabled = false;
            EditorUtility.SetDirty(col);

            Debug.Log($"[Configurador] Le apague el collider a '{go.name}' porque " +
                      "estaba tapando el cubo de meta. La esfera ahora lo atraviesa " +
                      "y aterriza sobre la meta.");
        }
    }

    /// <summary>
    /// Plataformas moviles. Los recorridos estan calculados para que la esfera
    /// pueda pasar de una plataforma fija a la siguiente sin quedarse atorada.
    /// Todo es editable despues en el Inspector.
    /// </summary>
    static int ConfigurarPlataformas(GameObject[] raices)
    {
        var moviles = raices
            .Where(g => g.name.ToLower().Contains("movible") || g.name.ToLower().Contains("movil"))
            .ToArray();

        foreach (var go in moviles)
        {
            var pm = Obtener<PlataformaMovil>(go);
            string nombre = go.name.ToLower();

            if (nombre.Contains("2"))
            {
                // Puente de piso (10) [y 17.3] hasta piso (12) [y 25], pasando por y 21.5.
                // Va del centro -23.2 al -11.8, o sea punto medio -17.5 y 5.7 de cada lado.
                ColocarRecorrido(go, pm, centroX: -17.5f, distancia: 5.7f, velocidad: 2f);
                pm.empezarHaciaLaIzquierda = true;
            }
            else
            {
                // Puente de piso (7) [x 4.69] hasta piso (9) [x -17.87], todo a y 13.9.
                // Va del centro -15 al -1: punto medio -8 y 7 de cada lado.
                ColocarRecorrido(go, pm, centroX: -8f, distancia: 7f, velocidad: 2.5f);
                pm.empezarHaciaLaIzquierda = false;
            }
        }

        return moviles.Length;
    }

    static void ColocarRecorrido(GameObject go, PlataformaMovil pm, float centroX, float distancia, float velocidad)
    {
        // La plataforma arranca en el centro de su recorrido.
        Vector3 pos = go.transform.position;
        go.transform.position = new Vector3(centroX, pos.y, pos.z);

        pm.distancia = distancia;
        pm.velocidad = velocidad;
        pm.esperaEnExtremos = 0.4f;

        var col = go.GetComponent<Collider>();
        if (col == null) col = go.AddComponent<BoxCollider>();
        col.isTrigger = false;
    }

    static void ConfigurarCamara(GameObject[] raices, GameObject jugador)
    {
        var cam = raices.FirstOrDefault(g => g.name == "Main Camera");
        if (cam == null) return;

        var seguidora = Obtener<CamaraSeguidora>(cam);
        seguidora.objetivo = jugador.transform;
        seguidora.desplazamiento = new Vector3(0f, 7f, -10f);
        seguidora.suavizado = 5f;
    }

    static void ConfigurarInterfaz(GameObject[] raices, GameManager gm)
    {
        var canvas = raices.FirstOrDefault(g => g.name == "Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("[Configurador] No encontre el Canvas.");
            return;
        }

        // Contador: el Text (TMP) que ya existe, anclado arriba a la izquierda.
        var contador = canvas.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(t => t.gameObject.name != "MensajeFinal");

        if (contador != null)
        {
            var rt = contador.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -30f);
            rt.sizeDelta = new Vector2(400f, 60f);
            contador.fontSize = 36f;
            contador.alignment = TextAlignmentOptions.TopLeft;
            contador.text = "Botellas: 0/0";
            gm.textoContador = contador;
        }

        // Mensaje final: texto grande al centro, apagado hasta llegar a la meta.
        Transform mf = canvas.transform.Find("MensajeFinal");
        GameObject mensajeGO;

        if (mf == null)
        {
            mensajeGO = new GameObject("MensajeFinal", typeof(RectTransform));
            mensajeGO.transform.SetParent(canvas.transform, false);
        }
        else
        {
            mensajeGO = mf.gameObject;
        }

        var texto = Obtener<TextMeshProUGUI>(mensajeGO);
        var rtm = texto.rectTransform;
        rtm.anchorMin = new Vector2(0.5f, 0.5f);
        rtm.anchorMax = new Vector2(0.5f, 0.5f);
        rtm.pivot = new Vector2(0.5f, 0.5f);
        rtm.anchoredPosition = Vector2.zero;
        rtm.sizeDelta = new Vector2(900f, 300f);
        texto.fontSize = 64f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.yellow;
        texto.text = "";
        mensajeGO.SetActive(false);

        gm.textoMensajeFinal = texto;
        EditorUtility.SetDirty(gm);
    }

    /// <summary>Carga un sonido de Assets/Audio si existe.</summary>
    static AudioClip CargarClip(string nombreArchivo)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + nombreArchivo);
    }

    /// <summary>
    /// Busca un AudioClip por un pedazo de su nombre, este en la carpeta que
    /// este. Asi no importa si despues mueves los archivos de lugar.
    /// </summary>
    static AudioClip BuscarClip(params string[] pedazosDelNombre)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            string archivo = System.IO.Path.GetFileNameWithoutExtension(ruta).ToLower();

            foreach (string pedazo in pedazosDelNombre)
            {
                if (archivo.Contains(pedazo.ToLower()))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
            }
        }
        return null;
    }

    /// <summary>
    /// Decide si podemos cambiar un sonido: solo si esta vacio o si lo que
    /// tiene es uno de los provisionales que generamos nosotros. Si tu pusiste
    /// otro a mano, no se toca.
    /// </summary>
    static bool SePuedeReemplazar(AudioClip actual)
    {
        if (actual == null) return true;
        string n = actual.name.ToLower();
        return n == "recolectar" || n == "meta" || n == "salto";
    }

    /// <summary>
    /// Deja el mp3 descomprimido en memoria. Para sonidos cortos que se
    /// repiten mucho (como el de recolectar) evita el tironcito al sonar.
    /// </summary>
    static void PrepararParaSonarSeguido(AudioClip clip)
    {
        if (clip == null) return;

        string ruta = AssetDatabase.GetAssetPath(clip);
        var importer = AssetImporter.GetAtPath(ruta) as AudioImporter;
        if (importer == null) return;

        var ajustes = importer.defaultSampleSettings;
        if (ajustes.loadType == AudioClipLoadType.DecompressOnLoad) return;

        ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
        ajustes.preloadAudioData = true;
        importer.defaultSampleSettings = ajustes;
        importer.SaveAndReimport();
    }

    static T Obtener<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }
}
