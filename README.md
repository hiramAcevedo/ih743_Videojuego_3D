# Camión de ruta

Videojuego 3D para teléfono Android de IH743 · Optativa Abierta III, unidad 4 y producto integrador (Universidad de
Guadalajara, Licenciatura en Desarrollo de Sistemas Web). Hiram Agustín Acevedo López.

Un camión de ruta avanza solo por una avenida mexicana: hay que cambiar de carril, brincar los topes y no chocar con los
autos parados ni con los que lo rebasan. Son tres niveles: Guadalajara, Monterrey y Ciudad de México, cada uno con su música.
En algunos cruces se agarra la onda verde, y con 10 topes brincados se guarda el poder sin topes.

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
  <img src="docs/capturas/4.3-rebase.png" width="270" alt="Un auto rebasa al camión por el carril de al lado en la hora pico, en vertical">
  <img src="docs/capturas/4.3-vida-perdida.png" width="270" alt="A los 1,891 m, un tope sin brincar a 120 km/h quita una vida y frena el camión a 32 km/h, en vertical">
  <img src="docs/capturas/4.3-110-kmh.png" width="270" alt="El velocímetro en 110 km/h a los 1,500 m, en el viernes de quincena, en vertical">
</p>

- **Avenida sin fin por tramos:** `GeneradorAvenida` crea tramos de 40 m delante del camión mientras se juega y destruye los
  que quedan atrás de la cámara; siempre hay siete u ocho armados. Cada tramo es una porción (un prefab): cuadra de casas,
  cuadra de comercios o cruce con paso peatonal y semáforo, con sus autos parados y topes repartidos por código. Ya no hay
  base: la avenida no se acaba.
- **Velocidad y velocímetro:** el camión sale a 36 km/h, llega a 72 km/h a los 380 m, a 95 km/h a los 1,000 m y a 110 km/h
  a los 1,500 m; de ahí sigue subiendo parejo, unos 2.5 km/h cada 100 m, hasta su tope de 200 km/h a los 5,000 m. El
  velocímetro, en la esquina inferior derecha, dice a cuántos km/h va.
- **Tres vidas en los topes:** desde 120 km/h los topes se brincan. Pasar uno sin brincar quita una de las tres vidas (los
  corazones de arriba a la derecha) y frena el camión a 32 km/h, que luego vuelve a acelerar; sin vidas, se acaba la partida.
- **Cuatro etapas de tráfico:** tráfico ligero desde 0 m, tráfico medio desde 250 m, hora pico desde 600 m y viernes de
  quincena desde 1,000 m. Las filas de obstáculos se juntan (de 30 a 40 m entre una y otra hasta 18 a 26 m) y salen más
  seguido dos autos juntos, siempre con un carril libre; cuando entre dos filas no da tiempo de cruzar dos carriles, ese
  carril queda junto a uno de los libres de la fila anterior, y a más velocidad las filas se separan lo justo para que siempre
  dé tiempo de pasarse al carril de junto. El nombre de la etapa aparece debajo del récord.
- **Autos que rebasan:** desde el tráfico medio vienen autos por detrás, más rápido que el camión, por el carril de al lado.
  El tranquilo lo rebasa, busca su lugar y ahí se queda: si encuentra un auto parado, se pasa a un carril libre que no sea el
  del camión o se forma detrás. Desde los 2,500 m salen también autos locos, que pasan mucho más rápido y se pierden de
  vista. Ninguno se cambia al carril del camión cerca de él, y si el camión se le mete enfrente, frena detrás. Cada etapa
  trae más: 3 por kilómetro en el tráfico medio, 6 en la hora pico y 10 en el viernes de quincena.
- **Etapas en el Inspector:** cada etapa es un `ScriptableObject` (`EtapaAvenida`, en `Assets/Etapas/`) que se crea con
  clic derecho > Create > Camión de ruta > Etapa de la avenida. Ahí se ajusta desde qué metro rige, qué porciones la
  componen y con qué peso, la separación entre filas, la probabilidad de dos autos y de tope, y cuántos autos por
  kilómetro vienen a rebasar. En el componente `GeneradorAvenida` van el largo del tramo, los tramos por delante, los
  autos, el tope, los autos que rebasan, la salida libre, la semilla y la lista de etapas.
- **Bucle de juego:** al chocar se guarda el récord y el aviso dice a cuántos metros llegaste y qué pasó: un auto parado,
  uno que te rebasaba (y quién se cambió de carril) o que te quedaste sin vidas en los topes. JUGAR DE NUEVO (`ControlPartida.ReiniciarNivel`) vuelve a empezar en la misma
  escena: el contador regresa a 0 m, el camión a la salida y a 36 km/h, y el generador arma una avenida nueva.

## Lo que hay en la entrega 4.4

<p align="center">
  <img src="docs/capturas/4.4-pintura.png" width="270" alt="El menú con la pintura blanca R-182 en el camión que gira, en vertical">
  <img src="docs/capturas/4.4-atardecer.png" width="270" alt="La hora pico al atardecer, con cielo naranja y sol bajo, a los 720 m, en vertical">
  <img src="docs/capturas/4.4-anochecer.png" width="270" alt="El viernes de quincena al anochecer, con las calaveras encendidas, a los 1,100 m, en vertical">
</p>

- **La hora del día por etapa:** cada etapa trae su ambiente en el Inspector: el color del cielo (el fondo de la cámara y
  la niebla), el color, la fuerza, la altura y el giro del sol, y el color de la luz ambiental. `LuzDelDia` lee la etapa del
  metro donde va el camión y pasa de un ambiente al otro en 3 segundos: mañana en el tráfico ligero, mediodía en el tráfico
  medio (cielo más azul y sol alto), atardecer en la hora pico (cielo naranja, sol bajo y sombras largas) y anochecer en el
  viernes de quincena (cielo morado y sol casi en el horizonte). JUGAR DE NUEVO regresa a la mañana de golpe.
- **Pintura del camión:** el botón PINTURA del menú cambia la del camión que gira entre la verde de Mi Transporte, la roja
  de las R-400 y la blanca con techo y defensas azules del trompudo R-182, y la guarda con `PlayerPrefs`: en la avenida el
  camión sale con esa pintura, también la próxima vez que se abre el juego. Cada pintura es un `ScriptableObject`
  (`PinturaCamion`, en `Assets/Pinturas/`) que se crea con clic derecho > Create > Camión de ruta > Pintura del camión: el
  color de la carrocería, del techo, de los cuadros y de las defensas, su suavidad y qué tan metálica es. `PintarCamion` la
  aplica con materiales propios del camión, así no tiñe a los autos que comparten material con él.
- **Posprocesado:** un volumen global en cada escena (`Assets/Posproceso/Brillo suave`) con un bloom suave, que sólo hace
  brillar lo que brilla más que el blanco (faros, calaveras, letrero de ruta, semáforos y direccionales), y un viñeteado
  ligero.

## Lo que hay en la entrega 4.5

<p align="center">
  <img src="docs/capturas/4.5-niveles.png" width="270" alt="El panel NIVELES del menú: Guadalajara por terminar, Monterrey cerrado y la ruta libre con su récord, en vertical">
  <img src="docs/capturas/4.5-terminal.png" width="270" alt="El aviso de nivel completado en la terminal de Guadalajara, con el botón SIGUIENTE: MONTERREY, en vertical">
  <img src="docs/capturas/4.5-monterrey.png" width="270" alt="Monterrey en la hora pico, con el Cerro de la Silla detrás de la ciudad y espectaculares en las azoteas, a los 700 m, en vertical">
</p>

- **Dos niveles, cada uno en su escena:** `Guadalajara` (nivel 1) y `Monterrey` (nivel 2), en Build Settings después de
  `Menu` y antes de `Ruta`, que queda como la ruta libre del récord. Cada nivel es un `ScriptableObject` (`NivelRuta`, en
  `Assets/Niveles/`) que se crea con clic derecho > Create > Camión de ruta > Nivel: la ciudad, la escena, los metros hasta la
  terminal, la velocidad del camión con su curva, las etapas de su avenida y desde dónde salen los autos locos. `RutaNiveles`
  los guarda en orden.
- **La terminal:** cada nivel termina en un arco con el letrero de la ciudad y una raya de llegada a cuadros. Desde 45 m antes
  ya no hay obstáculos y desde 220 m antes no sale ningún auto a rebasar. Al cruzarla, el nivel queda terminado con
  `PlayerPrefs`, el camión frena hasta pararse y sale el aviso con SIGUIENTE NIVEL, que carga la escena de la otra ciudad con
  `SceneManager`. Después de Monterrey sale el fin de la ruta.
- **Guadalajara y Monterrey:** Guadalajara es la avenida de las entregas anteriores hasta los 1,200 m, donde va a unos
  100 km/h. Monterrey mide 2,000 m: sale a 60 km/h, pasa de 120 km/h a los 800 m (de ahí hay que brincar los topes) y llega a
  145 km/h. Trae sus propias etapas, más cargadas (tráfico medio desde la salida, hora pico desde los 400 m y viernes de
  quincena desde los 1,000 m), autos locos desde los 800 m, fachadas de colores de tierra, espectaculares en las azoteas y el
  Cerro de la Silla en el horizonte, que se tiñe con el cielo de cada hora.
- **Menú y navegación:** JUGAR lleva al primer nivel sin terminar y lo dice debajo. NIVELES muestra una tarjeta por ciudad
  con su estado (por terminar, terminado o cerrado: Monterrey se abre al terminar Guadalajara) y la de la ruta libre con el
  récord. En la avenida, PAUSA tiene CONTINUAR, REINICIAR y MENÚ PRINCIPAL, y en los niveles el HUD dice la ciudad, cuánto
  falta para la terminal y una barra de avance. Cada cambio de escena lleva un fundido a negro (`Transicion`, con
  `SceneManager.LoadSceneAsync`), también al salir desde la pausa.

## Lo que hay en la entrega 4.6

<p align="center">
  <img src="docs/capturas/4.6-onda-verde.png" width="270" alt="El camión con la estela verde de la onda verde, el velocímetro en verde y la barra del acelerón, en vertical">
  <img src="docs/capturas/4.6-controles.png" width="270" alt="El tablero nuevo con las guías encendidas: los arcos de las esquinas, Salto, y el freno iluminado en rojo, en vertical">
  <img src="docs/capturas/4.6-sin-topes.png" width="270" alt="El poder sin topes en Monterrey, en la hora pico: la estela, el aviso y el indicador con los segundos que quedan, en vertical">
</p>

- **La onda verde (el acelerón):** en algunos cruces, desde el tráfico medio, uno de los tres carriles trae una franja verde
  al pie del semáforo. Si el camión pasa por encima, va 35 km/h arriba de su curva durante 2.5 s y después vuelve a ella.
  La franja va en un carril que la fila anterior dejó libre y detrás de ella la avenida va libre lo que dura el acelerón, así
  que tomarla nunca lleva a un choque. Como la súper velocidad del cap. 11, es un estado que prende un evento y se apaga solo
  (`Aceleron`); la franja es `OndaVerde` y cada etapa dice qué tan seguido sale (`probabilidadOndaVerde`).
- **La estela:** dos `TrailRenderer` en las esquinas de atrás del camión, de blanco verdoso a transparente, que sólo emiten
  mientras dura el acelerón. Cada punto vive 0.4 s, así que del primero al último no pasan 3 s: `Aceleron` no deja que los
  segundos del acelerón y los de la estela sumen más. Con la estela, la cámara abre 7° su campo de visión, el velocímetro se
  pinta de verde, sale el aviso y una barra (un `Slider`, como la del cap. 12) se vacía mientras dura.
- **El poder sin topes:** cada tope que el camión brinca sin tocarlo suma uno; con 10, el poder queda guardado en el
  indicador de arriba a la izquierda. Lo brincado no se pierde hasta usar el poder: pasa de Guadalajara a Monterrey y se
  queda al reiniciar o al perder (`PlayerPrefs`), así que los topes de Guadalajara y los de cada intento ayudan a pasar
  Monterrey. Al tocarlo (o con la E), durante 10 s los topes que quedan a menos de 160 m se hunden en el asfalto y el camión
  da el mismo acelerón con su estela. Los brincos, los segundos y si se usa solo se ajustan en el Inspector de
  `PoderSinTopes`.
- **Controles sin botones:** cada esquina de abajo, dentro de un arco chico, cambia de carril; fuera de los arcos, hacia
  arriba, se brinca; abajo al centro, el freno a la izquierda y el acelerador a la derecha actúan mientras se mantiene el dedo
  y al soltarlos el camión vuelve a su curva. Al tocar sólo se ilumina la zona: rojo el freno, verde el acelerador y un
  destello en los carriles y el salto. Las guías (los arcos, las flechas y los letreros) se apagan desde la pausa o desde
  AJUSTES, el engrane, y queda guardado con `PlayerPrefs`. Son `ZonaTactil` y `ControlesTactiles`.
- **Con el teléfono acostado:** Freno y Acelerador quedan junto a sus arcos, cada zona llega al centro y la lectura de metros
  y km/h baja hasta abajo, entre los dos, para no tapar el camión; los avisos van entre la lectura y el camión. En vertical
  todo queda igual. Lo acomoda `TableroHorizontal` cada vez que cambia el tamaño de la pantalla.
- **El tablero nuevo:** la información salió del centro para no tapar la carretera. Arriba a la izquierda, PAUSA, el engrane
  y el indicador del poder; arriba a la derecha, los corazones y el panel con la ciudad, el tráfico con sus cuatro barras y lo
  que falta para la terminal; abajo al centro, los metros y los km/h. Los trazos de pincel son PNG con transparencia
  dibujados por un script, la letra es la manuscrita Kalam Bold y los avisos salen arriba de los metros.

## Lo que hay en el producto integrador

<p align="center">
  <img src="docs/capturas/pi-cdmx.png" width="270" alt="Ciudad de México por la mañana, en el tráfico medio, con fachadas de tezontle, cantera y rosa mexicano y los volcanes en la bruma, a los 160 m, en vertical">
  <img src="docs/capturas/pi-noche.png" width="270" alt="Ciudad de México en la noche de quincena, la cuarta etapa: la onda verde a 188 km/h y la silueta de los volcanes, a 550 m de la terminal, en vertical">
  <img src="docs/capturas/pi-niveles.png" width="270" alt="El panel NIVELES con Guadalajara, Monterrey y Ciudad de México terminados y la ruta libre con su récord, en vertical">
</p>

- **Ciudad de México, el tercer nivel:** 2,400 m; sale a 70 km/h, pasa de 120 km/h hacia los 600 m y llega a 160 km/h. Sube
  en cuatro etapas: tráfico medio desde la salida, hora pico desde los 400 m, viernes de quincena desde los 1,000 m y noche de
  quincena desde los 1,700 m, con filas más juntas y más autos que rebasan que en Monterrey; los autos locos salen desde los
  600 m. Sus cuadras son de tezontle, cantera y los colores de Coyoacán, con espectaculares, y en el horizonte están el
  Iztaccíhuatl y el Popocatépetl con nieve. La terminal es rosa mexicano y al cruzarla sale el fin de la ruta.
- **Tres niveles que suben de dificultad:** Guadalajara (1,200 m, hasta 100 km/h), Monterrey (2,000 m, hasta 145 km/h) y
  Ciudad de México (2,400 m, hasta 160 km/h). Cada uno se abre al terminar el anterior, y NIVELES los muestra con su estado.
- **Sonido:** cada ciudad tiene su música: dos piezas de mariachi en Guadalajara, tres de guitarra regional y corridos en
  Monterrey, y dos cumbias y una salsa en Ciudad de México; en el menú y en la ruta libre suena una al azar de las ocho. Cada
  partida empieza con una al azar de su lista y, cuando se acaba, sigue otra; en la pausa baja al 40 %. Suenan efectos al
  chocar, al brincar, al pasar un tope, cuando un auto rebasa (un zumbido al pasar junto al camión), con el turbo de la onda
  verde y con el arranque del poder sin topes, y un claxon avisa un segundo antes de que salga un auto a rebasar. AJUSTES
  trae interruptores para la música y los efectos, guardados con `PlayerPrefs`. Lo lleva `Sonido`. El audio va dentro del
  APK y no en el repositorio; la razón y los créditos están al final, en Audio.
- **Para Android:** en Player Settings, el nombre Camión de ruta, el paquete `com.hiramacevedo.camionderuta`, la versión
  1.1.0 (código 2: se instala encima de la 1.0.0, que no tenía sonido), el ícono (el camión de frente en la carretera, sobre
  el amarillo del tablero) con sus capas de ícono adaptable, Android 8.0 (API 26) como mínimo y Android 16 (API 36) como
  objetivo, IL2CPP con ARM64, y el teléfono se puede girar, parado o acostado. El APK va en el release `act-pi`.

## Controles

- Teléfono, parado o acostado: la esquina de abajo a la izquierda o a la derecha cambia de carril; más arriba se brinca; abajo,
  entre los dos arcos, se mantienen Freno o Acelerador; el indicador del poder lo usa y PAUSA y el engrane están arriba a la
  izquierda.
- Computadora: flechas o A y D para el carril; barra, W o flecha arriba para brincar; S o flecha abajo para frenar; Shift
  para acelerar; E para usar el poder.
- En el menú, PINTURA cambia la pintura del camión, NIVELES elige ciudad o la ruta libre y el engrane abre AJUSTES: las
  guías, la música y los efectos.

## Abrir el proyecto

Unity 6000.5.9f1 con la plantilla Universal 3D. Se abre la carpeta desde Unity Hub, después `Assets/Scenes/Menu.unity`,
y Play. Para compilar el APK hace falta el módulo Android Build Support, con OpenJDK y Android SDK & NDK Tools, que se
agrega desde Unity Hub; luego File > Build Profiles > Android > Build.

## Estructura

- `Assets/Scripts/`: `CamionRuta` (movimiento, curva de velocidad y colisiones), `GeneradorAvenida` (los tramos sin fin,
  sus obstáculos y los autos que rebasan), `EtapaAvenida` (las etapas de la curva de dificultad), `AutoQueRebasa` (el auto
  que viene por detrás y rebasa, tranquilo o loco), `ControlPartida` (el bucle de juego: choque, vidas y jugar de nuevo), `Marcador`
  (contador de metros, velocímetro, récord con `PlayerPrefs` y etapa en pantalla), `CamaraSeguimiento`,
  `EncuadreExhibicion` (en vertical, la cámara del menú se aleja hasta que el camión quepa completo), `Navegacion`
  (escenas y paneles), `AreaSegura`, `ContenidoVertical` (los paneles caben sin encimarse en cualquier proporción de
  pantalla), `Girar`, `TextoConProfundidad`, `LuzDelDia` (la hora del día de cada etapa), `PinturaCamion` (una pintura del
  camión), `PintarCamion` (aplica la pintura elegida), `BotonPintura` (el botón PINTURA del menú), `NivelRuta` (un nivel),
  `RutaNiveles` (los niveles en orden), `BotonNivel` (una tarjeta del panel NIVELES), `Transicion` (el fundido entre escenas),
  `Horizonte` (la sierra del fondo: el cerro o los volcanes), `Formato` (los números con coma de miles), `Aceleron` (el
  acelerón con sus estelas), `OndaVerde` (la franja de los cruces), `PoderSinTopes`, `ZonaTactil` y `ControlesTactiles` (los
  controles sin botones), `Guias` y `BotonGuias` (el interruptor de las guías), `Aviso` (los avisos del tablero),
  `TableroHorizontal` (el tablero con el teléfono acostado), `Sonido` (la música de cada escena y los efectos) y
  `BotonSonido` (los interruptores de la música y los efectos).
- `Assets/Audio/` (fuera del repositorio; ver Audio): en `Musica/`, las ocho canciones en OGG; en `Efectos/`, los siete
  efectos en WAV mono. Los importa `Assets/Editor/ImportarAudio.cs`: la música en Streaming y Vorbis con calidad 0.4, los
  efectos en Decompress On Load, ADPCM y mono, para que el APK crezca lo menos posible.
- `Assets/Scenes/`: `Menu`, `Guadalajara`, `Monterrey`, `CiudadDeMexico` y `Ruta` (la ruta libre).
- `Assets/Niveles/`: Guadalajara, Monterrey, Ciudad de México y la ruta de niveles.
- `Assets/Etapas/`: Tráfico ligero, Tráfico medio, Hora pico y Viernes de quincena, las tres de Monterrey (`MTY`) y las cuatro
  de Ciudad de México (`CDMX`).
- `Assets/Pinturas/`: Verde Mi Transporte, Roja R-400 y Blanca R-182.
- `Assets/Posproceso/`: el perfil del volumen, con el bloom y el viñeteado.
- `Assets/Interfaz/Trazos/`: los trazos de pincel, los íconos y los brillos del tablero, en PNG con transparencia.
- `Assets/Interfaz/Fuentes/`: la letra del tablero, Kalam Bold, de Indian Type Foundry, tomada de Google Fonts con licencia
  SIL Open Font License 1.1; el texto de la licencia va junto a la letra, en `Kalam-OFL.txt`.
- `Assets/Interfaz/Icono/`: el ícono (`icono.png`) y las dos capas del ícono adaptable de Android (el fondo y el camión).
- `Assets/Prefabs/`: el camión, tres autos, sus tres variantes que rebasan (con direccionales) y el tope, armados con
  figuras básicas de Unity. El camión lleva el número R-743 y la pintura elegida en el menú. La franja de la onda verde es
  `Onda verde`. En `Tramos/`, las cinco porciones de la avenida (dos cuadras de casas, dos de comercios y el cruce), las
  cuatro de Monterrey y las cuatro de Ciudad de México; además, la terminal.
- `Assets/Modelos/`: el tope, el Cerro de la Silla y los volcanes (el Iztaccíhuatl y el Popocatépetl) en low poly.
- `Assets/Shaders/`: el texto 3D del número de ruta, que respeta la profundidad, y el del cerro del horizonte, que se dibuja
  detrás de todo y se tiñe con la niebla.

Cada entrega lleva su etiqueta: `act-4.1`, `act-4.2`, etcétera, y el producto integrador, `act-pi`.

## Audio

Los términos de Pixabay no permiten subir sus audios a un repositorio tal como se descargan, así que la música y los efectos
no están en este repositorio. El APK del release es el juego completo y sí los incluye. Quien clone el repositorio tiene el
juego sin sonido: funciona igual, sin errores.

La música y los efectos son de Pixabay, bajo la [Licencia de contenido de Pixabay](https://pixabay.com/service/license-summary/),
que no exige atribución; aquí se acreditan de todos modos. Las canciones se pasaron a OGG y se normalizaron a -16 LUFS; los
efectos, a WAV mono sin silencio al inicio, y el arranque de motor del poder se recortó a 2.7 s.

| Canción o efecto | Dónde suena | Autor | Enlace |
|---|---|---|---|
| Corazón de Mariachi - Traditional Melancholic Mexican Folk Music | Guadalajara | JuliusH | [Pixabay](https://pixabay.com/music/urban-latin-coraz%C3%B3n-de-mariachi-traditional-melancholic-mexican-folk-music-524999/) |
| Mexican Mariachi instrumental - Plaza de Papel | Guadalajara | Michael-Vault | [Pixabay](https://pixabay.com/music/modern-jazz-mexican-mariachi-instrumental-plaza-de-papel-580777/) |
| Mexican Corridos acoustic | Monterrey | Drippeados | [Pixabay](https://pixabay.com/music/acoustic-group-mexican-corridos-acoustic-324282/) |
| Regional Mexican Guitar | Monterrey | Drippeados | [Pixabay](https://pixabay.com/music/acoustic-group-regional-mexican-guitar-249679/) |
| Soft Mexican Guitar | Monterrey | Drippeados | [Pixabay](https://pixabay.com/music/folk-soft-mexican-guitar-343137/) |
| CUMBIA B | Ciudad de México | KAIRONVOSS | [Pixabay](https://pixabay.com/music/latin-cumbia-b-446534/) |
| Cumbia de fuego | Ciudad de México | Jean-Paul-V | [Pixabay](https://pixabay.com/music/samba-latin-cumbia-de-fuego-276499/) |
| Salsa Estilo New York 03 | Ciudad de México | vjgalaxy | [Pixabay](https://pixabay.com/music/latin-salsa-estilo-new-york-03-522534/) |
| Car Crash Sound | el choque | DRAGON-STUDIO | [Pixabay](https://pixabay.com/sound-effects/film-special-effects-car-crash-sound-376882/) |
| Swing Whoosh | el brinco | Jofae | [Pixabay](https://pixabay.com/sound-effects/film-special-effects-swing-whoosh-110410/) |
| Thump | el tope | freesound_community | [Pixabay](https://pixabay.com/sound-effects/household-thump-105302/) |
| Car Drive By Whoosh | el auto que rebasa | DRAGON-STUDIO | [Pixabay](https://pixabay.com/sound-effects/city-car-drive-by-whoosh-450445/) |
| Nitro Activation | el turbo de la onda verde | freesound_community | [Pixabay](https://pixabay.com/sound-effects/film-special-effects-nitro-activation-48077/) |
| Car Engine Start (outside) | el poder sin topes | freesound_community | [Pixabay](https://pixabay.com/sound-effects/technology-car-engine-start-outside-72279/) |
| Car Horn 02 | el claxon antes del rebase | Universfield | [Pixabay](https://pixabay.com/sound-effects/city-car-horn-02-153260/) |

En el menú y en la ruta libre suenan las ocho canciones.
