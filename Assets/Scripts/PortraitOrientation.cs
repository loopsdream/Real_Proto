// PortraitOrientation.cs - 강제 세로모드 유지
using UnityEngine;

public class PortraitOrientation : MonoBehaviour
{
    void Start()
    {
        // 세로모드로 강제 설정
        Screen.orientation = ScreenOrientation.Portrait;

        // 자동 회전 비활성화
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        Debug.Log("Portrait orientation locked");
    }

    void Update()
    {
        // 강제로 세로모드 유지
        if (Screen.orientation != ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}