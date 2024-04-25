using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ClientState
{
    // -> ConnectingToMaster
    Initial,
    // if success -> ConnectedToMaster, if failure -> FailedToConnectToMaster
    ConnectingToMaster,
    // -> LoggingIn
    ConnectedToMaster,
    // Stop
    FailedToConnectToMaster,
    // if success -> LoggedIn, if failure -> FailedToLogIn
    LoggingIn,
    // -> BetweenGames
    LoggedIn,
    // Stop
    FailedToLogIn,
    // RequestingGame
    BetweenGames,
    // if success -> ConnectingToGameServer, if failure -> FailedToGetGame
    RequestedGame,
    // -> ConnectingToGameServer
    AssignedGame,
    // Stop
    FailedToGetGame,
    // if success -> ConnectedToGameSever, if failure -> FailedToConnectToGameServer
    ConnectingToGameServer,
    // if get GameStarted -> GameStarted
    ConnectedToGameServer,
    // Stop
    FailedToConnectToGameServer,
    // if get GameEnded -> GameEnded
    GameStarted,
    // -> DisconnectFromGameServer
    GameEnded,
    // -> BetweenGames
    DisconnectedFromGameServer,
    DisconnectedFromMaster,
    Stop
};

public class ClientController : MonoBehaviour {

    public GameObject ButtonUI;
    public Button[] Buttons;

    public int GamesPlayed { get; set; }
    public int GamesAborted { get; set; }

    private ClientState _clientState;
    public ClientState ClientState
    {
        get { return _clientState; }
        set
        {
            _clientState = value;
            ShowClientState();
        }
    }

    private string _status;
    public string Status
    {
        set
        {
            _status = value;
        }
        get { return _status; }
    }

    ColorBlock highlightedColorBlock;

    private void ShowClientState()
    {
        if ((int)this.ClientState < Buttons.Length &&
            Buttons[(int)this.ClientState] != null)
        {
            Buttons[(int)this.ClientState].Select();
            Buttons[(int)this.ClientState].colors = highlightedColorBlock;
        }
    }

    void Start()
    {
        UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks % 10000));
        GamesPlayed = 0;
        GamesAborted = 0;

        ButtonUI.gameObject.SetActive(true);

        highlightedColorBlock = ColorBlock.defaultColorBlock;
        highlightedColorBlock.highlightedColor = new Color(135 / 255f, 206 / 255f, 250 / 255f);

        ClientState = ClientState.Initial;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(2, 2, Screen.width, style.fontSize);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;
        style.normal.textColor = Color.black;

        string text = string.Format("[Client | played {0} | aborted {1} | {2}] {3}",
            GamesPlayed, GamesAborted, ClientState, Status);
        GUI.Label(rect, text, style);
    }


}
