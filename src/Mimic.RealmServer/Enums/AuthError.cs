namespace Mimic.RealmServer
{
    public enum AuthError : byte
    {
        Success = 0x0,
        ProtocolError = 0x1, // CMaNGOS: UNKNOWN0
        Unimplemented = 0x2, // CMaNGOS: UNKNOWN2
    }
}