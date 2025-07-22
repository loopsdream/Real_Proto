// Unity Editor �޴� �߰�
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ChunkedPatchMenu
{
    [MenuItem("Tools/Unity MCP Chunked Patch/Test Large Message")]
    static void TestLargeMessage()
    {
        // 5KB ũ���� �׽�Ʈ �޽��� ����
        string largeMessage = new string('A', 5120);

        var chunks = UnityMCP.Chunked.ChunkedMessagePatch.SplitLargeMessage(largeMessage);

        Debug.Log($"Large message ({largeMessage.Length} bytes) split into {chunks.Count} chunks");

        // ûũ���� �ٽ� �����ؼ� �׽�Ʈ
        foreach (string chunk in chunks)
        {
            string reconstructed = UnityMCP.Chunked.ChunkedMessagePatch.ProcessChunkMessage(chunk);
            if (!string.IsNullOrEmpty(reconstructed))
            {
                Debug.Log($"Message reconstructed successfully: {reconstructed.Length} bytes");
                Debug.Log($"Original == Reconstructed: {largeMessage == reconstructed}");
            }
        }
    }

    [MenuItem("Tools/Unity MCP Chunked Patch/Show Session Status")]
    static void ShowSessionStatus()
    {
        UnityMCP.Chunked.ChunkedMessagePatch.LogSessionStatus();
    }

    [MenuItem("Tools/Unity MCP Chunked Patch/Force Cleanup")]
    static void ForceCleanup()
    {
        UnityMCP.Chunked.ChunkedMessagePatch.CleanupExpiredSessions();
        Debug.Log("Forced cleanup completed");
    }
}
#endif