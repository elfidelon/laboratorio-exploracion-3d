# Laboratorio de Exploración 3D

Proyecto 1 de 3 — **LAD537 Motores Gráficos Orientados a Videojuegos**
Licenciatura en Animación Digital · Quinto Semestre · Universidad De La Salle Bajío

Prototipo jugable de exploración/plataformas en 3D construido en **Unity 6** (6000.5.7f1).
El jugador controla una esfera que recorre un nivel vertical, cruza una puerta con
trigger, se apoya en plataformas móviles, recolecta botellas y llega a una meta.

## Controles

| Tecla | Acción |
|---|---|
| `WASD` / flechas | Moverse |
| `Espacio` | Saltar |

También responde a gamepad (stick izquierdo y botón inferior).

## Cómo abrirlo

1. Abre el proyecto con Unity 6 (6000.5.7f1 o superior).
2. Abre la escena `Assets/Scenes/SampleScene.unity`.
3. Menú **`Examen` → `Configurar escena automaticamente`**.
4. Play.

Ese menú arma la escena completa: agrega componentes, calcula los colliders de
las botellas, conecta las referencias de interfaz y de audio, y configura los
recorridos de las plataformas móviles. Se puede volver a ejecutar sin duplicar nada.

## Scripts

Todos en `Assets/Scripts/`.

| Script | Responsabilidad |
|---|---|
| `PlayerController` | Movimiento y salto con Rigidbody en `FixedUpdate`, Input System nuevo, detección de suelo |
| `Coleccionable` | Botellas recolectables mediante `OnTriggerEnter` |
| `GameManager` | Contador de botellas y mensajes en pantalla con TextMeshPro |
| `PuertaTrigger` | Puerta corrediza que se abre al acercarse el jugador |
| `ZonaPuerta` | Zona de detección invisible que avisa a la puerta |
| `PlataformaMovil` | Plataformas que van y vienen, y acarrean al jugador encima |
| `Meta` | Mensaje final, distinto según si se juntaron todas las botellas |
| `CamaraSeguidora` | Cámara en tercera persona con seguimiento suave |

`Assets/Scripts/Editor/ConfiguradorEscena.cs` es una herramienta del editor, no
forma parte del build.

Documentación de uso y ajustes en [`Assets/Scripts/LEEME.txt`](Assets/Scripts/LEEME.txt).

## Temario cubierto

- **Unidad I** — El motor de videojuegos
- **Unidad II** — El personaje y su interacción con el ambiente
- **Unidad III** — El código en el motor de videojuegos (3.1–3.4, 3.6, 3.9)

Variables, condicionales, bucles y eventos (`OnTriggerEnter`, `OnCollisionEnter`)
están repartidos entre los scripts; los bucles están en `PlayerController.HaySueloDebajo()`
y en `PlataformaMovil.FixedUpdate()`.

## Nota sobre los assets

El modelo `Beer_Bottle.fbx` y los audios `papoiiii.mp3`, `faaaa.mp3` y
`Happy Wheels victory green screen.mp3` son material de terceros usado con fines
educativos dentro de un trabajo escolar. Los archivos `recolectar.wav`, `meta.wav`
y `salto.wav` fueron generados sintéticamente para este proyecto.
