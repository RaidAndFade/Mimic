namespace Mimic.RealmServer
{
    public enum AuthCommand : byte
    {
        LogonChallenge = 0x00,
        LogonProof = 0x01,
        ReconnectChallenge = 0x02,
        ReconnectProof = 0x03,
        RealmList = 0x10,
        XferInitiate = 0x30,
        XferData = 0x31,

        XferAccept = 0x32,
        XferResume = 0x33,
        XferCancel = 0x34
    }
}