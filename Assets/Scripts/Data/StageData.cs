using UnityEngine;

namespace CROxCRO.Data
{
    [CreateAssetMenu(fileName = "New Stage", menuName = "CROxCRO/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Stage Basic Info")]
        public string stageName;
        public int stageNumber;
        
        [Header("Grid Settings")]
        public int width = 8;
        public int height = 8;
        
        [Header("Game Settings")]
        public float timeLimit = 300f; // 5분 기본 제한시간
        public int maxMoves = -1; // -1이면 무제한 이동
        
        [Header("Block Pattern")]
        [Tooltip("0: Empty, 1-6: Block Colors")]
        public int[,] blockPattern;
        
        [Header("Rewards")]
        public int coinReward = 100;
        public int diamondReward = 0;
        
        [Header("Difficulty")]
        [Range(1, 5)]
        public int difficulty = 1;
        
        // 에디터에서 패턴을 쉽게 편집하기 위한 직렬화 가능한 배열
        [SerializeField] private int[] serializedPattern;
        
        private void OnValidate()
        {
            // 2D 배열을 1D 배열로 직렬화
            if (blockPattern != null)
            {
                serializedPattern = new int[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (x < blockPattern.GetLength(0) && y < blockPattern.GetLength(1))
                            serializedPattern[y * width + x] = blockPattern[x, y];
                    }
                }
            }
        }
        
        public void InitializePattern()
        {
            blockPattern = new int[width, height];
            
            if (serializedPattern != null && serializedPattern.Length == width * height)
            {
                // 1D 배열을 2D 배열로 역직렬화
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        blockPattern[x, y] = serializedPattern[y * width + x];
                    }
                }
            }
        }
        
        public int GetBlockAt(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                return blockPattern[x, y];
            return 0;
        }
        
        public void SetBlockAt(int x, int y, int blockType)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                blockPattern[x, y] = blockType;
                OnValidate(); // 직렬화된 배열 업데이트
            }
        }
    }
}