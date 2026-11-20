#if UNITY_EDITOR
using System;
using UnityEditor;

namespace RecolecEco.EditorTools
{
    /// <summary>Mantiene las mallas de Blender en metros y sin rotaciones de conversión en el objeto raíz.</summary>
    sealed class CoastalArtImporter : AssetPostprocessor
    {
        const string ModelFolder = "Assets/RECOLECECO/Resources/ArteCosta/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelFolder, StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
        }
    }
}
#endif
