using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageShuffleSystem : MonoBehaviour
{
    [Header("Shuffle Animation Settings")]
    public float shuffleAnimationDuration = 1.0f;
    public float blockMoveSpeed = 5.0f;
    public AnimationCurve shuffleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shuffle Effects")]
    public ParticleSystem shuffleParticleEffect; // ���� ��ƼŬ ȿ����

    private GridManagerRefactored gridManager;
    private BlockFactory blockFactory;
    private MatchingSystem matchingSystem;

    void Awake()
    {
        gridManager = GetComponent<GridManagerRefactored>();
        blockFactory = GetComponent<BlockFactory>();
        matchingSystem = GetComponent<MatchingSystem>();
    }

    public IEnumerator ExecuteShuffle(GameObject[,] grid, int width, int height)
    {
        Debug.Log("Starting shuffle animation...");

        // 1. ���� ��ϵ��� ���� ����
        List<ShuffleBlockData> blockData = CollectBlockData(grid, width, height);

        if (blockData.Count == 0)
        {
            Debug.Log("No blocks to shuffle");
            yield break;
        }

        // 2. ��� Ÿ�� ����
        List<int> shuffledTypes = ShuffleBlockTypes(blockData);

        // 3. �ִϸ��̼ǰ� �Բ� ��� ���ġ
        yield return StartCoroutine(AnimateBlockShuffle(blockData, shuffledTypes, grid));

        Debug.Log("Shuffle completed!");
    }

    private List<ShuffleBlockData> CollectBlockData(GameObject[,] grid, int width, int height)
    {
        List<ShuffleBlockData> blockData = new List<ShuffleBlockData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject block = grid[x, y];
                if (block != null)
                {
                    Block blockComponent = block.GetComponent<Block>();
                    if (blockComponent != null && !blockComponent.isEmpty)
                    {
                        ShuffleBlockData data = new ShuffleBlockData
                        {
                            position = new Vector2Int(x, y),
                            blockType = blockFactory.GetBlockTypeFromTag(block.tag),
                            originalBlock = block
                        };
                        blockData.Add(data);
                    }
                }
            }
        }

        return blockData;
    }

    private List<int> ShuffleBlockTypes(List<ShuffleBlockData> blockData)
    {
        List<int> types = new List<int>();
        foreach (var data in blockData)
        {
            types.Add(data.blockType);
        }

        // Fisher-Yates ���� �˰���
        for (int i = 0; i < types.Count; i++)
        {
            int randomIndex = Random.Range(i, types.Count);
            int temp = types[i];
            types[i] = types[randomIndex];
            types[randomIndex] = temp;
        }

        return types;
    }

    private IEnumerator AnimateBlockShuffle(List<ShuffleBlockData> blockData, List<int> shuffledTypes, GameObject[,] grid)
    {
        // 1. ���� ��ϵ��� ���� �������� �ϴ� �ִϸ��̼�
        yield return StartCoroutine(AnimateBlocksUp(blockData));

        // 2. ��� Ÿ�� ���� (�߰��� ������ �ʴ� ���¿���)
        for (int i = 0; i < blockData.Count; i++)
        {
            Vector2Int pos = blockData[i].position;
            int newType = shuffledTypes[i];

            // ���� ��� ����
            if (grid[pos.x, pos.y] != null)
            {
                blockFactory.DestroyBlock(grid[pos.x, pos.y]);
            }

            // �� ��� ���� (���ʿ��� ����)
            grid[pos.x, pos.y] = blockFactory.CreateBlockFromType(newType, pos.x, pos.y);

            // ����� ���ʿ� ��ġ
            Vector3 startPos = grid[pos.x, pos.y].transform.position + Vector3.up * 5f;
            grid[pos.x, pos.y].transform.position = startPos;
        }

        // 3. �� ��ϵ��� �Ʒ��� ����߸��� �ִϸ��̼�
        yield return StartCoroutine(AnimateBlocksDown(blockData, grid));
    }

    private IEnumerator AnimateBlocksUp(List<ShuffleBlockData> blockData)
    {
        float elapsedTime = 0f;
        Vector3[] startPositions = new Vector3[blockData.Count];
        Vector3[] targetPositions = new Vector3[blockData.Count];

        for (int i = 0; i < blockData.Count; i++)
        {
            startPositions[i] = blockData[i].originalBlock.transform.position;
            targetPositions[i] = startPositions[i] + Vector3.up * 5f;
        }

        while (elapsedTime < shuffleAnimationDuration * 0.5f)
        {
            float progress = elapsedTime / (shuffleAnimationDuration * 0.5f);
            float curveValue = shuffleCurve.Evaluate(progress);

            for (int i = 0; i < blockData.Count; i++)
            {
                if (blockData[i].originalBlock != null)
                {
                    blockData[i].originalBlock.transform.position =
                        Vector3.Lerp(startPositions[i], targetPositions[i], curveValue);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator AnimateBlocksDown(List<ShuffleBlockData> blockData, GameObject[,] grid)
    {
        float elapsedTime = 0f;
        Vector3[] startPositions = new Vector3[blockData.Count];
        Vector3[] targetPositions = new Vector3[blockData.Count];

        for (int i = 0; i < blockData.Count; i++)
        {
            Vector2Int pos = blockData[i].position;
            GameObject newBlock = grid[pos.x, pos.y];
            if (newBlock != null)
            {
                startPositions[i] = newBlock.transform.position;
                targetPositions[i] = startPositions[i] - Vector3.up * 5f; // ���� ��ġ��
            }
        }

        while (elapsedTime < shuffleAnimationDuration * 0.5f)
        {
            float progress = elapsedTime / (shuffleAnimationDuration * 0.5f);
            float curveValue = shuffleCurve.Evaluate(progress);

            for (int i = 0; i < blockData.Count; i++)
            {
                Vector2Int pos = blockData[i].position;
                GameObject newBlock = grid[pos.x, pos.y];
                if (newBlock != null)
                {
                    newBlock.transform.position =
                        Vector3.Lerp(startPositions[i], targetPositions[i], curveValue);
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public bool ValidateShuffleResult(GameObject[,] grid)
    {
        return matchingSystem.HasAnyPossibleMatch(grid);
    }
}

[System.Serializable]
public struct ShuffleBlockData
{
    public Vector2Int position;
    public int blockType;
    public GameObject originalBlock;
}