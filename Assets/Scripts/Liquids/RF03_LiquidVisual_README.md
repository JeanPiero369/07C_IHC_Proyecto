# RF-03 - Sistema visual de líquidos

Implementación visual liviana para recipientes de laboratorio en VR.

## Archivos

- `LiquidVisualController.cs`: componente principal para nivel, transparencia, menisco y color por pH/concentración.
- `Editor/RF03LiquidMaterialCreator.cs`: menú de editor para crear un material transparente reutilizable.

## Uso en Unity

Opción rápida:

1. Selecciona el recipiente en la jerarquía, por ejemplo `FlaskGrabbable`, `Tube01Grabbable` o `BottleGrabbable`.
2. Ejecuta `Tools/RF-03/Add Liquid Visual To Selected Container`.
3. Ajusta el hijo creado `Liquid_RF03` desde el Inspector.

Opción manual:

1. Crea un GameObject hijo dentro del recipiente, por ejemplo `Liquid_RF03`.
2. Posiciónalo en el centro interno del vaso/matraz/tubo.
3. Agrega `LiquidVisualController` al GameObject hijo.
4. Ajusta desde Inspector:
   - `Bottom Offset`: altura local donde empieza el líquido.
   - `Max Liquid Height`: altura máxima del recipiente.
   - `Liquid Radius`: radio interno aproximado del recipiente.
   - `Liquid Level`: nivel 0..1.
   - `Transparency`: transparencia del líquido.
   - `Meniscus Intensity`: curvatura visual del menisco.
   - `Color Mode`: `Manual`, `ByPH` o `ByConcentration`.
   - `Test PH` o `Test Concentration`: valores visuales de prueba.
5. Opcional: usa `Tools/RF-03/Create Transparent Liquid Material` para generar `Assets/Materials/RF03_Liquids/RF03_TransparentLiquid_URP.mat` y asígnalo en `Liquid Material`. La opción rápida lo genera y asigna automáticamente.

## Vertido visual entre envases

Para que un envase pueda echar líquido:

1. Asegúrate de que el envase tenga un hijo `Liquid_RF03` con `LiquidVisualController`.
2. En el GameObject principal del envase, por ejemplo `FlaskGrabbable`, agrega `LiquidPourController`.
3. Crea un hijo vacío llamado `PourOrigin_RF03` y colócalo en la boca/labio del envase.
4. En `LiquidPourController`, asigna:
   - `Source Liquid`: el `Liquid_RF03` del mismo envase.
   - `Pour Origin`: el punto `PourOrigin_RF03`.
   - `Target Liquid`: opcional. Si lo dejas vacío, intentará detectar el líquido receptor debajo del chorro.
5. Ajusta:
   - `Pour Start Angle`: inclinación mínima para empezar a verter.
   - `Transfer Rate`: velocidad con la que baja/sube el nivel.
   - `Target Detection Radius`: tolerancia horizontal para encontrar el recipiente receptor.

El chorro usa `LineRenderer`, no partículas pesadas, para mantener buen rendimiento en Meta Quest 2/3.

## Colores independientes y mezcla visual

Cada `LiquidVisualController` guarda su propio `Base Color`; ya no depende de cambiar el material compartido.

Para configurar líquidos diferentes:

1. Selecciona el `Liquid_RF03` del envase.
2. En `Color Mode`, usa `Manual`.
3. Cambia `Base Color` al color de ese envase, por ejemplo naranja o celeste.
4. Puedes reutilizar el mismo material `RF03_TransparentLiquid_URP` en todos; el color ahora se aplica por objeto.

Para que al vaciar en un envase vacío adopte el color entrante:

- Activa `Use Incoming Color When Empty`.

Para mezclar dos líquidos:

- Activa `Mix Incoming Color Automatically` si quieres mezcla promedio automática.
- Si quieres decidir tú el resultado, por ejemplo naranja + celeste = verde:
  1. En el envase receptor activa `Use Custom Mixed Color`.
  2. En `Custom Mixed Color`, elige verde.

Esto es visual, no química real todavía; RF-04 podrá reemplazar esta decisión con cálculo de pH/concentración.

## Movimiento visual del líquido

`LiquidVisualController` incluye `Movement / Slosh`:

- `Simulate Surface Movement`: activa que la superficie intente mantenerse horizontal al inclinar el envase.
- `Slosh Amount`: cuánto se inclina visualmente el líquido.
- `Slosh Responsiveness`: rapidez de respuesta.
- `Max Surface Tilt`: límite de inclinación visual.

## Conexión futura con RF-04

RF-04 podrá calcular pH/concentración real y llamar:

```csharp
liquidVisual.SetPH(phCalculado);
liquidVisual.SetConcentration(concentracionNormalizada);
liquidVisual.SetLiquidLevel(nivelNormalizado);
liquidVisual.SetLiquidColor(colorPersonalizado);
```

## Notas de rendimiento VR

- No usa simulación física de fluidos.
- Genera solo dos mallas simples: un cilindro y un disco de menisco de 48 segmentos.
- No agrega colliders al líquido.
- No modifica scripts de agarre, soltado ni operación bimanual.
