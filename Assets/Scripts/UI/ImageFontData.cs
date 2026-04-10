using UnityEngine;

// 이미지 폰트 에셋 - ScriptableObject로 관리
[CreateAssetMenu(fileName = "ImageFontData", menuName = "CROxCRO/Image Font Data")]
public class ImageFontData : ScriptableObject
{
    [Header("Digit Sprites (0-9)")]
    public Sprite[] digitSprites = new Sprite[10]; // 인덱스 = 숫자값

    [Header("Special Character Sprites")]
    public Sprite slashSprite;   // "/" 분수 표시용
    public Sprite minusSprite;   // "-" 음수 표시용
    public Sprite colonSprite;   // ":" 타이머 표시용

    // 숫자(0-9)에 해당하는 스프라이트 반환
    public Sprite GetDigitSprite(int digit)
    {
        if (digit < 0 || digit > 9) return null;
        return digitSprites[digit];
    }

    // 문자에 해당하는 스프라이트 반환
    public Sprite GetCharSprite(char c)
    {
        if (c >= '0' && c <= '9') return GetDigitSprite(c - '0');
        if (c == '/') return slashSprite;
        if (c == '-') return minusSprite;
        if (c == ':') return colonSprite;
        return null;
    }
}