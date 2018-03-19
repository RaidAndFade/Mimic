using Mimic.WorldServer;
using System.Diagnostics;

namespace Mimic.WorldServer
{
    public class OpcodeHandler
    {
        private WorldSession _session;

        public static int NUM_OPCODES = 0x51f;

        private static OpcodeHandle[] _handles;

        public OpcodeHandler(WorldSession ws){
            _session = ws;
            initOpcodes(ws);
        }

        public void handle(WorldPacket wp){
            WorldCommand com = wp.cmd;
            if(_handles[(int)com] == null){
                Debug.Write("Received opcode "+com+" but it is not implemented yet. help!");
                return;
            }
            _handles[(int)com].handle(_session._wh._status,_session,wp);
        }

        public static void initOpcodes(WorldSession ws){
            if(_handles != null) return;

            _handles = new OpcodeHandle[NUM_OPCODES];
/* 0x037 */ _handles[(int)WorldCommand.CMSG_CHAR_ENUM] = new OpcodeHandle(AuthStatus.AUTHED,ws.RecvCharEnumRequest);
/* 0x209 */ _handles[(int)WorldCommand.CMSG_READY_FOR_ACCOUNT_DATA_TIMES] = new OpcodeHandle(AuthStatus.AUTHED, ws.RecvDataTimesRequest); 
/* 0x38C */ _handles[(int)WorldCommand.CMSG_REALM_SPLIT] = new OpcodeHandle(AuthStatus.AUTHED, ws.RecvRealmSplitRequest);
        }

    }
}
