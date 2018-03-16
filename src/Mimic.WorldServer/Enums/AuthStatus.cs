namespace Mimic.WorldServer
{
    public enum AuthStatus : byte
    {
        UNAUTHED,
        AUTHED,
        INGAME,
        MAPCHANGE,
        RECENTLOGCHANGE
    }
}
