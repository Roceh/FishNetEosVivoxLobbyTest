using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOSLobbyTest
{
    public class EOSConsts
    {
        // EOS allows you to group lobbies into buckets - we just have one
        public static string AllLobbiesBucketId = "ALL_LOBBIES";

        // key for lobby name attribute
        public static string AttributeKeyLobbyName = "LOBBY_NAME";
    }
}
