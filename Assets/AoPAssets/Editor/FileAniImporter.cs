using UnityEditor.AssetImporters;
using UnityEngine;

namespace AoP.Editor
{
    [ScriptedImporter(1, "ani")]
    public class FileAniImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            FileAni fileAni = new(ctx.assetPath);
            TextAsset textAsset = new(fileAni.ToString());
            ctx.AddObjectToAsset("main", textAsset);
            ctx.SetMainObject(textAsset);
        }
    }
}
