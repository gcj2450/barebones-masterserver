using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistinctColors
{
    // http://tools.medialab.sciences-po.fr/iwanthue/

    static string[] distinct = {
        "#01dfc6",
        "#f45800",
        "#3d7dd5",
        "#ffed31",
        "#d243a3",
        "#7db300",
        "#ff3b89",
        "#15902f",
        "#aa66a0",
        "#ffd339",
        "#57ceff",
        "#bf9600",
        "#b9ccff",
        "#eeff8f",
        "#008ea3",
        "#a6ffac",
        "#ffb1d6",
        "#a0734f",
        "#ffddf6",
        "#897978"
    };

    static public Color GetDistinctColor(int n)
    {
        Color result;

        ColorUtility.TryParseHtmlString(distinct[n % distinct.Length], out result);
        return result;
    }
}
