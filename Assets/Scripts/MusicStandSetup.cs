using UnityEngine;

public class MusicStandSetup : MonoBehaviour
{
    [Header("Sheet Music Display")]
    public Texture2D sheetTexture;
    public Renderer sheetRenderer;
    
    void Start()
    {
        SetupSheet();
    }
    
    void SetupSheet()
    {
        if (sheetRenderer == null)
        {
            // Try to find SheetHolder child
            Transform holder = transform.Find("SheetHolder");
            if (holder != null)
                sheetRenderer = holder.GetComponent<Renderer>();
        }
        
        if (sheetRenderer != null && sheetTexture != null)
        {
            // Create unlit material with texture
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.mainTexture = sheetTexture;
            sheetRenderer.material = mat;
        }
    }
}
