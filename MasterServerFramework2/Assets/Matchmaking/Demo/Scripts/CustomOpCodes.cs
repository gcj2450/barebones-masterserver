using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CustomOpCodes : short
{
    requestStartGame = 1,
    gameServerMatchDetails,
    gameServerMatchCompletion
}
