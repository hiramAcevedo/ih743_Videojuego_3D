# Camión de ruta

Videojuego 3D para teléfono de IH743 · Optativa Abierta III, unidad 4 (Universidad de Guadalajara, Licenciatura en
Desarrollo de Sistemas Web). Hiram Agustín Acevedo López.

Un camión de ruta avanza solo por una avenida mexicana: hay que cambiar de carril, brincar los topes y no chocar con los
autos parados. Guadalajara, Monterrey y Ciudad de México serán los niveles.

## Lo que hay en la entrega 4.1

<p align="center">
  <img src="docs/capturas/4.1-menu.png" width="270" alt="Menú del juego en un teléfono vertical">
  <img src="docs/capturas/4.1-tope.png" width="270" alt="El camión pasa un tope en la avenida, en vertical">
</p>

- **Interfaz para teléfono vertical:** menú (Jugar, Cómo se juega, Salir) y escena de la avenida, con el lienzo escalado
  a 1080 × 1920, área segura para la muesca, botones táctiles (◄, BRINCAR, ►) y pausa.
- **Movimiento en los tres ejes con un Rigidbody:** el camión avanza en Z, cambia de carril en X y brinca en Y; los topes
  lo levantan y lo bajan.
- **Colisiones:** BoxCollider en el camión y en los autos, MeshCollider convexo en los topes. Al chocar con un auto, el
  camión se detiene y aparece el aviso; al llegar a la base termina el tramo de prueba.
- **Cámara:** sigue al camión sólo en X y Z; su altura no cambia y su distancia en Z es fija (−15 m).

## Lo que hay en la entrega 4.2

<p align="center">
  <img src="docs/capturas/4.2-contador.png" width="270" alt="El contador marca 95 m en la avenida, en vertical">
  <img src="docs/capturas/4.2-menu-record.png" width="270" alt="El menú con el récord de 103 m y el camión completo">
</p>

- **Contador en pantalla:** arriba, al centro, un texto de UI con los metros que lleva el camión; sube parejo con su avance
  en Z.
- **Curva de dificultad:** el camión sale a 36 km/h y acelera con los metros recorridos hasta 72 km/h a los 380 m. Las tres
  cifras y la forma de la curva (`AnimationCurve`) se ajustan en el Inspector, en el componente `CamionRuta`.
- **Récord guardado:** la mejor distancia se guarda con `PlayerPrefs` al chocar, al llegar a la base o al volver al menú
  desde la pausa, y aparece en el menú y debajo del contador.
- **Menú en vertical:** la cámara se aleja según la proporción de la pantalla para que el camión de exhibición quepa
  completo mientras gira, en 9:16 y en 20:9; en horizontal se queda donde estaba.

## Lo que hay en la entrega 4.3

<p align="center">
  <img src="docs/capturas/4.3-hora-pico.png" width="270" alt="La avenida en la hora pico a los 700 m, en vertical">
  <img src="docs/capturas/4.3-choque.png" width="270" alt="El aviso de choque con JUGAR DE NUEVO, en vertical">
</p>

- **Avenida sin fin por tramos:** `GeneradorAvenida` crea tramos de 40 m delante del camión mientras se juega y destruye los
  que quedan atrás de la cámara; siempre hay siete u ocho armados. Cada tramo es una porción (un prefab): cuadra de casas,
  cuadra de comercios o cruce con paso peatonal y semáforo, con sus autos parados y topes repartidos por código. Ya no hay
  base: la avenida no se acaba.
- **Curva de dificultad por etapas:** además de la velocidad (36 a 72 km/h), el tráfico crece con la distancia. Tráfico
  ligero desde 0 m, tráfico medio desde 250 m y hora pico desde 600 m: las filas de obstáculos se juntan (de 30 a 40 m
  entre una y otra hasta 18 a 26 m) y salen más seguido dos autos juntos, siempre con un carril libre. El nombre de la
  etapa aparece debajo del récord.
- **Etapas en el Inspector:** cada etapa es un `ScriptableObject` (`EtapaAvenida`, en `Assets/Etapas/`) que se crea con
  clic derecho > Create > Camión de ruta > Etapa de la avenida. Ahí se ajusta desde qué metro rige, qué porciones la
  componen y con qué peso, la separación entre filas y la probabilidad de dos autos y de tope. En el componente
  `GeneradorAvenida` van el largo del tramo, los tramos por delante, los autos, el tope, la salida libre, la semilla y la
  lista de etapas.
- **Bucle de juego:** al chocar se guarda el récord y el aviso dice a cuántos metros llegaste. JUGAR DE NUEVO
  (`ControlPartida.ReiniciarNivel`) vuelve a empezar en la misma escena: el contador regresa a 0 m, el camión a la salida
  y a 36 km/h, y el generador arma una avenida nueva.

## Controles

- Teléfono: ◄ y ► cambian de carril, BRINCAR salta y PAUSA está arriba a la izquierda.
- Computadora: flechas o A y D para el carril; barra, W o flecha arriba para brincar.

## Abrir el proyecto

Unity 6000.5.9f1 con la plantilla Universal 3D. Se abre la carpeta desde Unity Hub, después `Assets/Scenes/Menu.unity`,
y Play.

## Estructura

- `Assets/Scripts/`: `CamionRuta` (movimiento, curva de velocidad y colisiones), `GeneradorAvenida` (los tramos sin fin y
  sus obstáculos), `EtapaAvenida` (las etapas de la curva de dificultad), `ControlPartida` (el bucle de juego: choque y
  jugar de nuevo), `Marcador` (contador de metros, récord con `PlayerPrefs` y etapa en pantalla), `CamaraSeguimiento`,
  `EncuadreExhibicion` (en vertical, la cámara del menú se aleja hasta que el camión quepa completo), `Navegacion`
  (escenas y paneles), `AreaSegura`, `ContenidoVertical` (los paneles caben sin encimarse en cualquier proporción de
  pantalla), `Girar` y `TextoConProfundidad`.
- `Assets/Scenes/`: `Menu` y `Ruta`.
- `Assets/Etapas/`: Tráfico ligero, Tráfico medio y Hora pico.
- `Assets/Prefabs/`: el camión, tres autos y el tope, armados con figuras básicas de Unity. El camión lleva el verde de
  las unidades actuales de Guadalajara y el número R-743; el modelo definitivo y las otras pinturas llegan en las
  siguientes entregas. En `Tramos/`, las cinco porciones de la avenida: dos cuadras de casas, dos de comercios y el cruce.
- `Assets/Shaders/`: el texto 3D del número de ruta, que respeta la profundidad.

Cada entrega lleva su etiqueta: `act-4.1`, `act-4.2`, etcétera.
