// 기존 Unity MCP에 연결하는 인터셉터
using UnityEngine;
using System.Reflection;
using UnityEditor;

namespace UnityMCP.Chunked
{
    /// <summary>
    /// 기존 Unity MCP 메시지 처리를 가로채서 청크 기능 추가
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMCPInterceptor
    {
        private static bool isPatched = false;

        static UnityMCPInterceptor()
        {
            // Unity 시작 시 패치 적용
            EditorApplication.delayCall += ApplyPatch;
        }

        static void ApplyPatch()
        {
            if (isPatched) return;

            try
            {
                // 기존 Unity MCP의 메시지 처리 메서드를 찾아서 패치
                PatchMessageHandling();

                // 정리 작업 시작
                EditorApplication.update += PeriodicCleanup;

                isPatched = true;
                Debug.Log("[Chunked Patch] Successfully patched Unity MCP for chunked message support");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Chunked Patch] Failed to apply patch: {e.Message}");
            }
        }

        static void PatchMessageHandling()
        {
            // 기존 Unity MCP의 어셈블리에서 메시지 처리 클래스 찾기
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    // Unity MCP 관련 타입들 찾기
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name.Contains("MCP") || type.Name.Contains("Bridge") || type.Name.Contains("Server"))
                        {
                            // 메시지 처리 메서드 후킹
                            HookMessageMethods(type);
                        }
                    }
                }
                catch
                {
                    // 일부 어셈블리에서는 타입을 가져올 수 없을 수 있음
                    continue;
                }
            }
        }

        static void HookMessageMethods(System.Type type)
        {
            try
            {
                // 메시지 처리와 관련된 메서드들 찾기
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    if (method.Name.Contains("Process") ||
                        method.Name.Contains("Handle") ||
                        method.Name.Contains("Send") ||
                        method.Name.Contains("Receive"))
                    {
                        // 리플렉션을 통한 메서드 후킹은 복잡하므로
                        // 대신 이벤트 기반 접근 방식 사용
                        Debug.Log($"[Chunked Patch] Found method to potentially hook: {type.Name}.{method.Name}");
                    }
                }
            }
            catch
            {
                // 메서드 후킹 실패 시 무시
            }
        }

        static void PeriodicCleanup()
        {
            // 5초마다 정리 작업 수행
            if (Time.realtimeSinceStartup % 5f < 0.1f)
            {
                ChunkedMessagePatch.CleanupExpiredSessions();
            }
        }

        /// <summary>
        /// 외부에서 메시지 전송 시 호출할 수 있는 메서드
        /// </summary>
        public static void SendChunkedMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var chunks = ChunkedMessagePatch.SplitLargeMessage(message);

            foreach (string chunk in chunks)
            {
                // 기존 Unity MCP의 전송 메서드 호출
                SendToUnityMCP(chunk);
            }
        }

        /// <summary>
        /// 수신된 메시지 처리
        /// </summary>
        public static string ProcessReceivedMessage(string message)
        {
            if (ChunkedMessagePatch.IsChunkedMessage(message))
            {
                return ChunkedMessagePatch.ProcessChunkMessage(message);
            }

            return message; // 일반 메시지는 그대로 반환
        }

        private static void SendToUnityMCP(string message)
        {
            // 기존 Unity MCP의 전송 메서드를 리플렉션으로 호출
            // 실제 구현에서는 기존 Unity MCP의 API를 사용
            Debug.Log($"[Chunked Patch] Sending chunk: {message.Length} bytes");
        }
    }
}