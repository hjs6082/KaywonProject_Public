// =============================================================================
// OutlineTest.cs
// =============================================================================
// QuickOutline 동작 테스트용 스크립트
// 사용법: 단서 오브젝트에 추가하고 Play 모드 진입
// =============================================================================

using UnityEngine;

namespace GameDatabase.Player
{
    public class OutlineTest : MonoBehaviour
    {
        private Component _outline;

        private void Start()
        {
            // Outline 컴포넌트 찾기
            var allComponents = GetComponents<Component>();
            foreach (var comp in allComponents)
            {
                if (comp != null && comp.GetType().Name == "Outline")
                {
                    _outline = comp;
                    Debug.Log($"[OutlineTest] Outline 컴포넌트 발견: {comp.GetType().FullName}");
                    break;
                }
            }

            if (_outline == null)
            {
                Debug.LogError("[OutlineTest] Outline 컴포넌트를 찾을 수 없습니다!");
                return;
            }

            // Outline 프로퍼티 확인
            var type = _outline.GetType();
            Debug.Log($"[OutlineTest] Outline 타입: {type.AssemblyQualifiedName}");

            // OutlineWidth 프로퍼티 확인
            var widthProp = type.GetProperty("OutlineWidth");
            if (widthProp != null)
            {
                var currentWidth = widthProp.GetValue(_outline);
                Debug.Log($"[OutlineTest] 현재 OutlineWidth: {currentWidth}");

                // Width를 5로 설정
                widthProp.SetValue(_outline, 5f);
                Debug.Log($"[OutlineTest] OutlineWidth를 5로 설정함");

                var newWidth = widthProp.GetValue(_outline);
                Debug.Log($"[OutlineTest] 새 OutlineWidth: {newWidth}");
            }
            else
            {
                Debug.LogError("[OutlineTest] OutlineWidth 프로퍼티를 찾을 수 없습니다!");
            }

            // OutlineColor 프로퍼티 확인
            var colorProp = type.GetProperty("OutlineColor");
            if (colorProp != null)
            {
                colorProp.SetValue(_outline, Color.yellow);
                Debug.Log($"[OutlineTest] OutlineColor를 Yellow로 설정함");
            }

            // OutlineMode 프로퍼티 확인
            var modeProp = type.GetProperty("OutlineMode");
            if (modeProp != null)
            {
                var currentMode = modeProp.GetValue(_outline);
                Debug.Log($"[OutlineTest] OutlineMode: {currentMode}");
            }

            // Renderer 확인
            var renderers = GetComponentsInChildren<Renderer>();
            Debug.Log($"[OutlineTest] Renderer 개수: {renderers.Length}");
            foreach (var renderer in renderers)
            {
                Debug.Log($"[OutlineTest] Renderer: {renderer.name}, Materials: {renderer.sharedMaterials.Length}");
                foreach (var mat in renderer.sharedMaterials)
                {
                    Debug.Log($"[OutlineTest]   - Material: {(mat != null ? mat.name : "null")}");
                }
            }
        }
    }
}
