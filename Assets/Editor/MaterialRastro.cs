using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MaterialRastro
{
    [MenuItem("Tools/Material Rastro")]
    public static void Crear()
    {
        Asegurar("Assets/Entities");
        Asegurar("Assets/Entities/VFX");
        Asegurar("Assets/Entities/VFX/Materiales");
        Asegurar("Assets/Entities/VFX/Texturas");

        // Textura: brillante/opaca al centro de la cinta, se desvanece hacia los bordes
        string rutaTex = "Assets/Entities/VFX/Texturas/TexRastro.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex) == null)
        {
            Texture2D tmp = new Texture2D(4, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++)
            {
                float v = (y + 0.5f) / 32f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(2f * v - 1f)), 1.2f);
                for (int x = 0; x < 4; x++) tmp.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tmp.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetParent(Application.dataPath).FullName, rutaTex), tmp.EncodeToPNG());
            Object.DestroyImmediate(tmp);
            AssetDatabase.ImportAsset(rutaTex);
            var imp = (TextureImporter)AssetImporter.GetAtPath(rutaTex);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex);

        string rutaMat = "Assets/Entities/VFX/Materiales/MatRastro.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        bool nuevo = m == null;
        if (nuevo)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            m = new Material(s);
        }

        if (m.HasProperty("_Surface"))
        {
            var src = UnityEngine.Rendering.BlendMode.SrcAlpha;
            var dst = UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f); // Alpha Blend (no Additive)
            m.SetFloat("_SrcBlend", (float)src);
            m.SetFloat("_DstBlend", (float)dst);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)dst);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
        }
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;

        if (nuevo) AssetDatabase.CreateAsset(m, rutaMat); else EditorUtility.SetDirty(m);

        Rastro rastro = Object.FindAnyObjectByType<Rastro>();
        if (rastro != null)
        {
            rastro.material = m;
            EditorUtility.SetDirty(rastro);
            EditorSceneManager.MarkSceneDirty(rastro.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = m;
        Debug.Log("Material del rastro creado en " + rutaMat);
    }

    static void Asegurar(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        AssetDatabase.CreateFolder(Path.GetDirectoryName(ruta).Replace("\\", "/"), Path.GetFileName(ruta));
    }
}