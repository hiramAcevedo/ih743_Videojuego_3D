using UnityEditor;
using UnityEngine;

// Cómo se importa el audio del juego, para que el APK crezca lo menos posible:
//   Assets/Audio/Musica: Streaming (se lee del disco mientras suena, no ocupa memoria entera) y Vorbis con calidad 0.4.
//   Assets/Audio/Efectos: Decompress On Load (listos para sonar sin retraso), ADPCM y en mono.
// Se aplica al importar cada archivo; si cambian estos ajustes, se sube la versión y Unity los vuelve a importar.
public class ImportarAudio : AssetPostprocessor
{
    const string CarpetaMusica = "Assets/Audio/Musica/";
    const string CarpetaEfectos = "Assets/Audio/Efectos/";
    const float CalidadMusica = 0.4f;

    public override uint GetVersion() => 1;

    void OnPreprocessAudio()
    {
        var importador = (AudioImporter)assetImporter;
        var ajustes = importador.defaultSampleSettings;
        if (assetPath.StartsWith(CarpetaMusica))
        {
            importador.forceToMono = false;
            ajustes.loadType = AudioClipLoadType.Streaming;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = CalidadMusica;
            ajustes.preloadAudioData = false;
        }
        else if (assetPath.StartsWith(CarpetaEfectos))
        {
            importador.forceToMono = true;
            ajustes.loadType = AudioClipLoadType.DecompressOnLoad;
            ajustes.compressionFormat = AudioCompressionFormat.ADPCM;
            ajustes.preloadAudioData = true;
        }
        else
        {
            return;
        }
        ajustes.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importador.defaultSampleSettings = ajustes;
        importador.ClearSampleSettingOverride("Android");
    }
}
