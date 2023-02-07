using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace AoP.Editor
{
    [ScriptedImporter(1, "tga.tx")]
    public sealed class FileTgaTxImporter : ScriptedImporter
    {

        private const string FOUR_CC_DXT1 = "DXT1";
        private const string FOUR_CC_DXT3 = "DXT3";
        private const string FOUR_CC_DXT5 = "DXT5";

        [SerializeField] private bool isReadWrite = false;
        [SerializeField] private bool isSRGBTexture = true;
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField] private int anisoLevel = 1;
        [SerializeField] private TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        [SerializeField] private TextureWrapMode wrapModeU = TextureWrapMode.Repeat;
        [SerializeField] private TextureWrapMode wrapModeV = TextureWrapMode.Repeat;
        [SerializeField] private bool isFlipVertically = false;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            FileTgaTx fileTgaTx = new(ctx.assetPath);
            Texture2D texture2D = CreateTexture2D(fileTgaTx);
            ctx.AddObjectToAsset("main", texture2D);
            ctx.SetMainObject(texture2D);
        }

        private Texture2D CreateTexture2D(FileTgaTx fileTgaTx)
        {
            int width = fileTgaTx.headerData.width;
            int height = fileTgaTx.headerData.height;
            int mipMapCount = fileTgaTx.headerData.mipMapCount;
            GraphicsFormat graphicsFormat = GetGraphicsFormat(fileTgaTx.headerData.fourCC);
            Texture2D texture2D = new(width, height, graphicsFormat, mipMapCount, TextureCreationFlags.MipChain)
            {
                filterMode = filterMode,
                anisoLevel = anisoLevel,
            };
            if ((int)wrapMode > -1)
            {
                texture2D.wrapMode = wrapMode;
                wrapModeU = wrapMode;
                wrapModeV = wrapMode;
            }
            else
            {
                texture2D.wrapModeU = wrapModeU;
                texture2D.wrapModeV = wrapModeV;
                if (wrapModeU == wrapModeV)
                {
                    wrapMode = wrapModeU;
                    texture2D.wrapMode = wrapMode;
                }
            }
            if (isFlipVertically)
            {
                // TODO flip vertically texture data
            }
            texture2D.LoadRawTextureData(fileTgaTx.textureData.data);
            texture2D.Apply(false, !isReadWrite);
            return texture2D;
        }

        private GraphicsFormat GetGraphicsFormat(string fourCC)
        {
            switch (fourCC)
            {
                case FOUR_CC_DXT1:
                    {
                        return isSRGBTexture ? GraphicsFormat.RGBA_DXT1_SRGB : GraphicsFormat.RGBA_DXT1_UNorm;
                    }
                case FOUR_CC_DXT3:
                    {
                        return isSRGBTexture ? GraphicsFormat.RGBA_DXT3_SRGB : GraphicsFormat.RGBA_DXT3_UNorm;
                    }
                case FOUR_CC_DXT5:
                    {
                        return isSRGBTexture ? GraphicsFormat.RGBA_DXT5_SRGB : GraphicsFormat.RGBA_DXT5_UNorm;
                    }
                default:
                    {
                        throw new System.ArgumentException(string.Format("Unknown fourCC: {0}", fourCC), "fourCC");
                    }
            }
        }
    }
}
