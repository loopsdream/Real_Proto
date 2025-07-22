// ChunkedMessagePatch.cs - ���� Unity MCP�� �߰��� ��ġ
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityMCP.Chunked
{
    /// <summary>
    /// ���� Unity MCP �ý��ۿ� ûũ ����� �߰��ϴ� ��ġ
    /// </summary>
    public static class ChunkedMessagePatch
    {
        private const int MAX_CHUNK_SIZE = 1500; // 2KB���� �۰� ����
        private const string CHUNK_MARKER = "__CHUNKED_MESSAGE__";

        private static Dictionary<string, ChunkedReceiveSession> activeSessions =
            new Dictionary<string, ChunkedReceiveSession>();

        /// <summary>
        /// ū �޽����� ûũ�� ���� (���� Unity MCP ȣȯ)
        /// </summary>
        public static List<string> SplitLargeMessage(string originalMessage)
        {
            List<string> chunks = new List<string>();

            // �޽����� ������ �״�� ��ȯ
            if (originalMessage.Length <= MAX_CHUNK_SIZE)
            {
                chunks.Add(originalMessage);
                return chunks;
            }

            // ���� ID ����
            string sessionId = Guid.NewGuid().ToString("N")[..8]; // 8�ڸ��� ����
            byte[] messageBytes = Encoding.UTF8.GetBytes(originalMessage);
            int totalChunks = Mathf.CeilToInt((float)messageBytes.Length / MAX_CHUNK_SIZE);

            Debug.Log($"[Chunked Patch] Splitting message: {messageBytes.Length} bytes into {totalChunks} chunks");

            // �� ûũ ����
            for (int i = 0; i < totalChunks; i++)
            {
                int startIndex = i * MAX_CHUNK_SIZE;
                int length = Mathf.Min(MAX_CHUNK_SIZE, messageBytes.Length - startIndex);

                byte[] chunkData = new byte[length];
                Array.Copy(messageBytes, startIndex, chunkData, 0, length);

                // ûũ �޽��� ���� (JSON ���·� ����)
                var chunkMessage = new
                {
                    __chunked = true,
                    sessionId = sessionId,
                    chunkIndex = i,
                    totalChunks = totalChunks,
                    data = Convert.ToBase64String(chunkData),
                    isLastChunk = (i == totalChunks - 1)
                };

                string chunkJson = JsonUtility.ToJson(chunkMessage);
                chunks.Add(chunkJson);
            }

            return chunks;
        }

        /// <summary>
        /// ûũ �޽������� Ȯ��
        /// </summary>
        public static bool IsChunkedMessage(string message)
        {
            try
            {
                // ������ ûũ �ĺ��� üũ
                return message.Contains("\"__chunked\":true") ||
                       message.Contains("__chunked") ||
                       message.Contains("sessionId");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ûũ �޽����� ó���ϰ� ������ �޽��� ��ȯ (�ִ� ���)
        /// </summary>
        public static string ProcessChunkMessage(string chunkMessage)
        {
            try
            {
                var chunkData = JsonUtility.FromJson<ChunkData>(chunkMessage);

                if (!chunkData.__chunked)
                {
                    return null; // ûũ �޽����� �ƴ�
                }

                string sessionId = chunkData.sessionId;

                // ������ ������ ����
                if (!activeSessions.ContainsKey(sessionId))
                {
                    activeSessions[sessionId] = new ChunkedReceiveSession
                    {
                        SessionId = sessionId,
                        TotalChunks = chunkData.totalChunks,
                        ReceivedChunks = new Dictionary<int, byte[]>(),
                        StartTime = DateTime.Now
                    };

                    Debug.Log($"[Chunked Patch] Started new session: {sessionId} ({chunkData.totalChunks} chunks)");
                }

                var session = activeSessions[sessionId];

                // ûũ ������ ����
                byte[] chunkBytes = Convert.FromBase64String(chunkData.data);
                session.ReceivedChunks[chunkData.chunkIndex] = chunkBytes;

                Debug.Log($"[Chunked Patch] Received chunk {chunkData.chunkIndex + 1}/{chunkData.totalChunks}");

                // ��� ûũ�� �����ߴ��� Ȯ��
                if (session.ReceivedChunks.Count == session.TotalChunks)
                {
                    // ûũ���� ������� ������
                    var reconstructedBytes = new List<byte>();
                    for (int i = 0; i < session.TotalChunks; i++)
                    {
                        if (session.ReceivedChunks.ContainsKey(i))
                        {
                            reconstructedBytes.AddRange(session.ReceivedChunks[i]);
                        }
                        else
                        {
                            Debug.LogError($"[Chunked Patch] Missing chunk {i} in session {sessionId}");
                            return null;
                        }
                    }

                    // ���� ����
                    activeSessions.Remove(sessionId);

                    // ���� �޽��� ����
                    string reconstructedMessage = Encoding.UTF8.GetString(reconstructedBytes.ToArray());

                    Debug.Log($"[Chunked Patch] Message reconstructed: {reconstructedBytes.Count} bytes");

                    return reconstructedMessage;
                }

                return null; // ���� ��� ûũ�� �������� ����
            }
            catch (Exception e)
            {
                Debug.LogError($"[Chunked Patch] Error processing chunk: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// ����� ���ǵ��� ����
        /// </summary>
        public static void CleanupExpiredSessions()
        {
            var expiredSessions = new List<string>();
            DateTime cutoffTime = DateTime.Now.AddMinutes(-5); // 5�� Ÿ�Ӿƿ�

            foreach (var kvp in activeSessions)
            {
                if (kvp.Value.StartTime < cutoffTime)
                {
                    expiredSessions.Add(kvp.Key);
                }
            }

            foreach (string sessionId in expiredSessions)
            {
                activeSessions.Remove(sessionId);
                Debug.LogWarning($"[Chunked Patch] Cleaned up expired session: {sessionId}");
            }
        }

        /// <summary>
        /// ���� Ȱ�� ���� ���� ��� (������)
        /// </summary>
        public static void LogSessionStatus()
        {
            Debug.Log($"[Chunked Patch] Active sessions: {activeSessions.Count}");
            foreach (var kvp in activeSessions)
            {
                var session = kvp.Value;
                Debug.Log($"  Session {kvp.Key}: {session.ReceivedChunks.Count}/{session.TotalChunks} chunks");
            }
        }
    }

    /// <summary>
    /// ûũ ������ ����
    /// </summary>
    [System.Serializable]
    public class ChunkData
    {
        public bool __chunked;
        public string sessionId;
        public int chunkIndex;
        public int totalChunks;
        public string data;
        public bool isLastChunk;
    }

    /// <summary>
    /// ûũ ���� ����
    /// </summary>
    public class ChunkedReceiveSession
    {
        public string SessionId;
        public int TotalChunks;
        public Dictionary<int, byte[]> ReceivedChunks;
        public DateTime StartTime;
    }
}