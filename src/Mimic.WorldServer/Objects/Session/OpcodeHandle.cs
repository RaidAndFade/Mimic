using System;
using System.Reflection;

namespace Mimic.WorldServer
{
    public class OpcodeHandle
    {
        private MethodInfo _func;
        private AuthStatus _authStatus;

        public OpcodeHandle(AuthStatus authStatus, Action<WorldPacket> func){
            _func = func.Method;
            _authStatus = authStatus;
        }


        public void handle(AuthStatus curStatus, WorldSession ws, WorldPacket com){
            if(curStatus < _authStatus){
                throw new Exception("Auth status not good enough");
            }
            _func.Invoke(ws,new object[]{com});
        }
    }
}
