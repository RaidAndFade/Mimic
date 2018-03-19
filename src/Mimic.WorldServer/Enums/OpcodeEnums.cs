namespace Mimic.WorldServer
{
    public enum AuthStatus : byte
    {
        UNAUTHED, //not even authed yet
        AUTHED, //Authed, in charselect or something
        RECENTLOGCHANGE, //Authed, recently in or out of game
        MAPCHANGE, //Authed, ingame, changing zone
        INGAME //Authed, Ingame
    }

    public enum OpcodeHandleTime : byte
    {
        OnRecieve,
        OnSessionUpdate, //threadunsafe
        OnMapUpdate //threadsafe
    }
}
