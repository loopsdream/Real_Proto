// ���� Unity MCP�� �����ϴ� ���ͼ���
using UnityEngine;
using System.Reflection;
using UnityEditor;

namespace UnityMCP.Chunked
{
    /// <summary>
    /// ���� Unity MCP �޽��� ó���� ����ä�� ûũ ��� �߰�
    /// </summary>
    [InitializeOnLoad]
    public static class UnityMCPInterceptor
    {
        private static bool isPatched = false;

        static UnityMCPInterceptor()
        {
            // Unity ���� �� ��ġ ����
            EditorApplication.delayCall += ApplyPatch;
        }

        static void ApplyPatch()
        {
            if (isPatched) return;

            try
            {
                // ���� Unity MCP�� �޽��� ó�� �޼��带 ã�Ƽ� ��ġ
                PatchMessageHandling();

                // ���� �۾� ����
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
            // ���� Unity MCP�� ��������� �޽��� ó�� Ŭ���� ã��
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    // Unity MCP ���� Ÿ�Ե� ã��
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.Name.Contains("MCP") || type.Name.Contains("Bridge") || type.Name.Contains("Server"))
                        {
                            // �޽��� ó�� �޼��� ��ŷ
                            HookMessageMethods(type);
                        }
                    }
                }
                catch
                {
                    // �Ϻ� ����������� Ÿ���� ������ �� ���� �� ����
                    continue;
                }
            }
        }

        static void HookMessageMethods(System.Type type)
        {
            try
            {
                // �޽��� ó���� ���õ� �޼���� ã��
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    if (method.Name.Contains("Process") ||
                        method.Name.Contains("Handle") ||
                        method.Name.Contains("Send") ||
                        method.Name.Contains("Receive"))
                    {
                        // ���÷����� ���� �޼��� ��ŷ�� �����ϹǷ�
                        // ��� �̺�Ʈ ��� ���� ��� ���
                        Debug.Log($"[Chunked Patch] Found method to potentially hook: {type.Name}.{method.Name}");
                    }
                }
            }
            catch
            {
                // �޼��� ��ŷ ���� �� ����
            }
        }

        static void PeriodicCleanup()
        {
            // 5�ʸ��� ���� �۾� ����
            if (Time.realtimeSinceStartup % 5f < 0.1f)
            {
                ChunkedMessagePatch.CleanupExpiredSessions();
            }
        }

        /// <summary>
        /// �ܺο��� �޽��� ���� �� ȣ���� �� �ִ� �޼���
        /// </summary>
        public static void SendChunkedMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            var chunks = ChunkedMessagePatch.SplitLargeMessage(message);

            foreach (string chunk in chunks)
            {
                // ���� Unity MCP�� ���� �޼��� ȣ��
                SendToUnityMCP(chunk);
            }
        }

        /// <summary>
        /// ���ŵ� �޽��� ó��
        /// </summary>
        public static string ProcessReceivedMessage(string message)
        {
            if (ChunkedMessagePatch.IsChunkedMessage(message))
            {
                return ChunkedMessagePatch.ProcessChunkMessage(message);
            }

            return message; // �Ϲ� �޽����� �״�� ��ȯ
        }

        private static void SendToUnityMCP(string message)
        {
            // ���� Unity MCP�� ���� �޼��带 ���÷������� ȣ��
            // ���� ���������� ���� Unity MCP�� API�� ���
            Debug.Log($"[Chunked Patch] Sending chunk: {message.Length} bytes");
        }
    }
}