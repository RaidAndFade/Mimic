using Mimic.WorldServer;

namespace Mimic.WorldServer
{
    public class OpcodeHandler
    {
        private WorldSession _session;

        private static OpcodeHandle[] _handles;

        public OpcodeHandler(WorldSession ws){
            _session = ws;
            initOpcodes();
        }

        public void handleOpcode(){

        }

        public void initOpcodes(){

        }

    }
}
