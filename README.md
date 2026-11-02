# Camión de ruta

Videojuego 3D para teléfono de IH743 · Optativa Abierta III, unidad 4 (Universidad de Guadalajara, Licenciatura en
Desarrollo de Sistemas Web). Hiram Agustín Acevedo López.

Un camión de ruta avanza solo por una avenida mexicana: hay que cambiar de carril, brincar los topes y no chocar con los
autos parados. Guadalajara, Monterrey y Ciudad de México serán los niveles.

## Lo que hay en la entrega 4.1

- **Interfaz para teléfono vertical:** menú (Jugar, Cómo se juega, Salir) y escena de la avenida, con el lienzo escalado
  a 1080 × 1920, área segura para la muesca, botones táctiles (◄, BRINCAR, ►) y pausa.
- **Movimiento en los tres ejes con un Rigidbody:** el camión avanza en Z, cambia de carril en X y brinca en Y; los topes
  lo levantan y lo bajan.
- **Colisiones:** BoxCollider en el camión y en los autos, MeshCollider convexo en los topes. Al chocar con un auto, el
  camión se detiene y aparece el aviso; al llegar a la base termina el tramo de prueba.
- **Cámara:** sigue al camión sólo en X y Z; su altura no cambia y su distancia en Z es fija (−15 m).

## Controles

- Teléfono: ◄ y ► cambian de carril, BRINCAR salta y PAUSA está arriba a la izquierda.
- Computadora: flechas o A y D para el carril; barra, W o flecha arriba para brincar.

## Abrir el proyecto

Unity 6000.5.9f1 con la plantilla Universal 3D. Se abre la carpeta desde Unity Hub, después `Assets/Scenes/Menu.unity`,
y Play.

## Estructura

- `Assets/Scripts/`: `CamionRuta` (movimiento y colisiones), `CamaraSeguimiento`, `Navegacion` (escenas y paneles),
  `AreaSegura` y `Girar`.
- `Assets/Scenes/`: `Menu` y `Ruta`.
- `Assets/Prefabs/`: el camión, tres autos y el tope, armados con figuras básicas de Unity; el modelo definitivo del
  camión llega en las siguientes entregas.

Cada entrega lleva su etiqueta: `act-4.1`, `act-4.2`, etcétera.
