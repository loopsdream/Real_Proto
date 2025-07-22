// ChunkedMessagePatch.cs - 기존 Unity MCP에 추가할 패치
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UnityMCP.Chunked
{
    /// <summary>
    /// 기존 Unity MCP 시스템에 청크 기능을 추가하는 패치
    /// </summary>
    public static class ChunkedMessagePatch
    {
        private const int MAX_CHUNK_SIZE = 1500; // 2KB보다 작게 설정
        private const string CHUNK_MARKER = "__CHUNKED_MESSAGE__";

        private static Dictionary<string, ChunkedReceiveSession> activeSessions =
            new Dictionary<string, ChunkedReceiveSession>();

        /// <summary>
        /// 큰 메시지를 청크로 분할 (기존 Unity MCP 호환)
        /// </summary>
        public static List<string> SplitLargeMessage(string originalMessage)
        {
            List<string> chunks = new List<string>();

            // 메시지가 작으면 그대로 반환
            if (originalMessage.Length <= MAX_CHUNK_SIZE)
            {
                chunks.Add(originalMessage);
                return chunks;
            }

            // 세션 ID 생성
            string sessionId = Guid.NewGuid().ToString("N")[..8]; // 8자리로 단축
            byte[] messageBytes = Encoding.UTF8.GetBytes(originalMessage);
            int totalChunks = Mathf.CeilToInt((float)messageBytes.Length / MAX_CHUNK_SIZE);

            Debug.Log($"[Chunked Patch] Splitting message: {messageBytes.Length} bytes into {totalChunks} chunks");

            // 각 청크 생성
            for (int i = 0; i < totalChunks; i++)
            {
                int startIndex = i * MAX_CHUNK_SIZE;
                int length = Mathf.Min(MAX_CHUNK_SIZE, messageBytes.Length - startIndex);

                byte[] chunkData = new byte[length];
                Array.Copy(messageBytes, startIndex, chunkData, 0, length);

                // 청크 메시지 구성 (JSON 형태로 유지)
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
        /// 청크 메시지인지 확인
        /// </summary>
        public static bool IsChunkedMessage(string message)
        {
            try
            {
                // 간단한 청크 식별자 체크
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
        /// 청크 메시지를 처리하고 완전한 메시지 반환 (있는 경우)
        /// </summary>
        public static string ProcessChunkMessage(string chunkMessage)
        {
            try
            {
                var chunkData = JsonUtility.FromJson<ChunkData>(chunkMessage);

                if (!chunkData.__chunked)
                {
                    return null; // 청크 메시지가 아님
                }

                string sessionId = chunkData.sessionId;

                // 세션이 없으면 생성
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

                // 청크 데이터 저장
                byte[] chunkBytes = Convert.FromBase64String(chunkData.data);
                session.ReceivedChunks[chunkData.chunkIndex] = chunkBytes;

                Debug.Log($"[Chunked Patch] Received chunk {chunkData.chunkIndex + 1}/{chunkData.totalChunks}");

                // 모든 청크가 도착했는지 확인
                if (session.ReceivedChunks.Count == session.TotalChunks)
                {
                    // 청크들을 순서대로 재조립
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

                    // 세션 정리
                    activeSessions.Remove(sessionId);

                    // 원본 메시지 복원
                    string reconstructedMessage = Encoding.UTF8.GetString(reconstructedBytes.ToArray());

                    Debug.Log($"[Chunked Patch] Message reconstructed: {reconstructedBytes.Count} bytes");

                    return reconstructedMessage;
                }

                return null; // 아직 모든 청크가 도착하지 않음
            }
            catch (Exception e)
            {
                Debug.LogError($"[Chunked Patch] Error processing chunk: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 만료된 세션들을 정리
        /// </summary>
        public static void CleanupExpiredSessions()
        {
            var expiredSessions = new List<string>();
            DateTime cutoffTime = DateTime.Now.AddMinutes(-5); // 5분 타임아웃

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
        /// 현재 활성 세션 정보 출력 (디버깅용)
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
    /// 청크 데이터 구조
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
    /// 청크 수신 세션
    /// </summary>
    public class ChunkedReceiveSession
    {
        public string SessionId;
        public int TotalChunks;
        public Dictionary<int, byte[]> ReceivedChunks;
        public DateTime StartTime;
    }
}