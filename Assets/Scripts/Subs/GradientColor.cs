using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GradientColor : MonoBehaviour
{
    public Material gradientMat;
    public Color leftColor;
    public Color rightColor;

    private void Start()
    {
        gradientMat.SetColor("_Color", leftColor);
        gradientMat.SetColor("_Color2", rightColor);
    }
}
