using UnityEditor;
using UnityEngine;

public class SetSpriteType
{
    [MenuItem("Tools/Set 44_meter to Sprite")]
    public static void SetToSprite()
    {
        string path = "Assets/UI/44_meter.png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
            Debug.Log("44_meter.png 已设置为 Sprite 类型！");
        }
        else
        {
            Debug.LogError("找不到图片：" + path);
        }
    }
}
