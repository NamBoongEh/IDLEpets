#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Quirky Series FREE 머티리얼의 셰이더를
/// Built-in SoftSurface → URP SoftSurfaceGraph 로 교체하는 Editor 유틸
/// 메뉴: Tools → Fix Quirky Series Materials
/// </summary>
public class FixQuirkyMaterials : EditorWindow
{
    [MenuItem("Tools/Fix Quirky Series Materials")]
    static void FixMaterials()
    {
        // URP용 ShaderGraph 찾기
        Shader urpShader = Shader.Find("Shader Graphs/SoftSurfaceGraph");
        if (urpShader == null)
        {
            EditorUtility.DisplayDialog("오류",
                "Shader Graphs/SoftSurfaceGraph 를 찾을 수 없습니다.\n" +
                "SoftSurfaceGraph.shadergraph 파일이 Assets 폴더 안에 있는지 확인하세요.",
                "확인");
            return;
        }

        // FREE/Materials 폴더의 모든 머티리얼 검색
        string[] guids = AssetDatabase.FindAssets("t:Material",
            new[] { "Assets/Quirky Series Ultimate/FREE/Materials" });

        int fixed_count = 0;
        foreach (string guid in guids)
        {
            string path  = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // 이미 URP 셰이더면 스킵
            if (mat.shader == urpShader) continue;

            // 기존 프로퍼티 저장
            Texture mainTex  = mat.GetTexture("_MainTex");
            Color   color    = mat.HasProperty("_Color")    ? mat.GetColor("_Color")       : Color.gray;
            float   emission = mat.HasProperty("_Emission") ? mat.GetFloat("_Emission")    : 0.5f;

            // 셰이더 교체
            mat.shader = urpShader;

            // 프로퍼티 복원
            if (mainTex != null) mat.SetTexture("_MainTex", mainTex);
            mat.SetColor("_Color",    color);
            mat.SetFloat("_Emission", emission);

            EditorUtility.SetDirty(mat);
            fixed_count++;
            Debug.Log($"[FixQuirkyMaterials] 교체 완료: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            $"{fixed_count}개 머티리얼 셰이더 교체 완료!\n" +
            "에디터 씬에서 동물 색이 정상으로 돌아왔는지 확인하세요.",
            "확인");
    }
}
#endif
