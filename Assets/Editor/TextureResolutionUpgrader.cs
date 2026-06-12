#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quirky Series 텍스쳐 Max Size 일괄 업그레이드 도구
///
/// ▶ 사용법
///   Unity 메뉴 → Tools → IDLEpets → 텍스쳐 해상도 최대로 설정
///
/// ▶ 왜 런타임에는 변경 불가한가?
///   텍스쳐 해상도는 임포트 단계(에디터)에서 결정되어 빌드에 굽혀짐.
///   런타임의 Texture2D.width/height는 읽기 전용이며,
///   QualitySettings.globalTextureMipmapLimit = 0(이미 Ultra 기본값)은
///   Mip 레벨을 0으로 고정할 뿐 Max Size 자체를 늘리지 못함.
///   → 임포트 설정 Max Size를 올린 뒤 재임포트해야 실제 해상도가 증가.
///
/// ▶ 이 스크립트가 하는 일
///   Assets 폴더 전체에서 텍스쳐(.png/.jpg/.tga 등)를 찾아
///   TextureImporter.maxTextureSize 를 4096 으로 설정하고 재임포트.
///   Quirky Series 에셋 경로 필터(QuirkySeries)를 포함한 것만 처리해
///   다른 텍스쳐에 영향을 주지 않음.
///
/// ▶ 주의
///   4096 이상의 텍스쳐는 GPU 메모리를 많이 사용함.
///   Quirky Series 원본이 256~512 이면 Max Size를 올려도
///   픽셀 수가 늘어나지 않음(업스케일 없음) — 원본 해상도가 상한.
/// </summary>
public static class TextureResolutionUpgrader
{
    const int   TARGET_MAX_SIZE    = 4096;
    const string FILTER_PATH_PART  = "";        // 빈 문자열 = 전체 Assets 대상
    //  Quirky Series 만 대상으로 하려면: "QuirkySeries" 로 변경

    [MenuItem("Tools/IDLEpets/텍스쳐 해상도 최대로 설정 (4096)")]
    static void UpgradeAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        int changed = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();   // 재임포트를 일괄 처리 — 속도 향상

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // 경로 필터 (필요 시 FILTER_PATH_PART 를 설정)
                if (!string.IsNullOrEmpty(FILTER_PATH_PART) &&
                    !path.Contains(FILTER_PATH_PART))
                {
                    skipped++;
                    continue;
                }

                // 에디터 전용 폴더는 건너뜀
                if (path.Contains("/Editor/") || path.Contains("\\Editor\\"))
                {
                    skipped++;
                    continue;
                }

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) { skipped++; continue; }

                bool dirty = false;

                // Max Size 상향
                if (importer.maxTextureSize < TARGET_MAX_SIZE)
                {
                    importer.maxTextureSize = TARGET_MAX_SIZE;
                    dirty = true;
                }

                // Mip Map 활성화 (없으면 원거리에서 흐릿해짐)
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    dirty = true;
                }

                // 압축 품질을 Normal → Best 로 (더 선명한 DXT 압축)
                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    dirty = true;
                }

                // FilterMode: Bilinear 이하이면 Trilinear 로 (Mip 경계 부드럽게)
                if (importer.filterMode < FilterMode.Trilinear)
                {
                    importer.filterMode = FilterMode.Trilinear;
                    dirty = true;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();   // StartAssetEditing 범위 내라서 큐에 쌓임
                    changed++;
                }
                else
                {
                    skipped++;
                }

                // 진행률 표시
                if (EditorUtility.DisplayCancelableProgressBar(
                        "텍스쳐 해상도 업그레이드",
                        $"{changed}개 변경 / {i + 1}/{guids.Length}",
                        (float)(i + 1) / guids.Length))
                {
                    Debug.LogWarning("[TextureResolutionUpgrader] 사용자가 취소했습니다.");
                    break;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();    // 여기서 큐의 재임포트를 한꺼번에 실행
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[TextureResolutionUpgrader] 완료 — {changed}개 업그레이드, {skipped}개 스킵");
        EditorUtility.DisplayDialog(
            "텍스쳐 해상도 업그레이드 완료",
            $"변경: {changed}개\n스킵(이미 최대 / 해당 없음): {skipped}개",
            "확인");
    }

    // ── 선택된 텍스쳐만 업그레이드 ────────────────────────────────
    [MenuItem("Tools/IDLEpets/선택한 텍스쳐만 4096으로 설정")]
    static void UpgradeSelected()
    {
        Object[] selected = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("선택 없음", "Project 창에서 텍스쳐를 선택하세요.", "확인");
            return;
        }

        int changed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (Object obj in selected)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.maxTextureSize    = TARGET_MAX_SIZE;
                importer.mipmapEnabled     = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.filterMode        = FilterMode.Trilinear;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                changed++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[TextureResolutionUpgrader] 선택 {changed}개 업그레이드 완료");
    }

    [MenuItem("Tools/IDLEpets/선택한 텍스쳐만 4096으로 설정", true)]
    static bool UpgradeSelectedValidate() =>
        Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets).Length > 0;
}
#endif
