// PortraitOrientation.cs - ���� ���θ�� ����
using UnityEngine;

public class PortraitOrientation : MonoBehaviour
{
    void Start()
    {
        // ���θ��� ���� ����
        Screen.orientation = ScreenOrientation.Portrait;

        // �ڵ� ȸ�� ��Ȱ��ȭ
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        Debug.Log("Portrait orientation locked");
    }

    void Update()
    {
        // ������ ���θ�� ����
        if (Screen.orientation != ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}