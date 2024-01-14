using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using Mirror.RemoteCalls;

namespace BetterCoreManagement.Network
{
    public class CoreNetwork : NetworkBehaviour
    {
        //Properties
        [SyncVar]
        public string NetworkID;
        public PlatformUserId playerID;
        private NetworkedPlayer _player => Player.instance.networkedPlayer;

        private HashSet<NetworkConnection> _clients = new HashSet<NetworkConnection>();

        public string NetworkNetworkID
        {
            get => NetworkID;
            [param: In]
            set
            {
                if (SyncVarEqual(value, ref NetworkID))
                    return;
                string networkId = NetworkID;
                SetSyncVar(value, ref NetworkID, 1UL);
            }
        }
        
        #region Sync Methods

        protected override bool SerializeSyncVars(NetworkWriter writer, bool forceAll)
        {
            bool flag = base.SerializeSyncVars(writer, forceAll);
            if (forceAll)
            {
                writer.WriteString(NetworkID);
                return true;
            }
            writer.WriteULong(syncVarDirtyBits);
            if (((long) syncVarDirtyBits & 1L) != 0L)
            {
                writer.WriteString(NetworkID);
                flag = true;
            }
            return flag;
        }

        protected override void DeserializeSyncVars(NetworkReader reader, bool initialState)
        {
            base.DeserializeSyncVars(reader, initialState);
            if (initialState)
            {
                string networkId = NetworkID;
                NetworkNetworkID = reader.ReadString();
            }
            else
            {
                if (((long) reader.ReadULong() & 1L) == 0L)
                    return;
                string networkId = NetworkID;
                NetworkNetworkID = reader.ReadString();
            }
        }        

        #endregion

        #region Static Methods
        static CoreNetwork()
        {
            RemoteCallHelper.RegisterCommandDelegate(typeof(CoreNetwork), "RequestCoreCount",
                new CmdDelegate(InvokeUserCode_RequestCoreCount), true);
            RemoteCallHelper.RegisterRpcDelegate(typeof(CoreNetwork), "LoadCoreCountFromServer",
                new CmdDelegate(InvokeUserCode_LoadCoreCountFromServer));
        }

        private static void InvokeUserCode_RequestCoreCount(NetworkBehaviour obj, NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkServer.active)
                BetterCoreManagement.Log.LogError("Command RequestCoreCount called on client.");
            else
                ((CoreNetwork)obj).UserCode_RequestCoreCount(senderConnection);
        }
        
        protected static void InvokeUserCode_LoadCoreCountFromServer(
            NetworkBehaviour obj,
            NetworkReader reader,
            NetworkConnectionToClient senderConnection)
        {
            if (!NetworkClient.active)
                BetterCoreManagement.Log.LogError("TargetRPC LoadCoreCountFromServer called on server.");
            else
                ((CoreNetwork)obj).UserCode_LoadCoreCountFromServer(NetworkClient.connection,
                    reader.ReadList<int>(),reader.ReadList<int>());
        }

        #endregion

        #region Instantiated Methods

        private void Awake()
        {
            syncMode = SyncMode.Owner;
        }

        public override void OnStartClient()
        {
            if (connectionToClient == null)
            {
                return;
            }
            
            _clients.Add(connectionToClient);
        }

        public override void OnStartLocalPlayer()
        {
            BetterCoreManagement.Log.LogDebug($"OnStartLocalPlayer() for Network ID {netId}");
            RequestCoreCount();
            InvokeRepeating(nameof(InvokeRequestCoreCount),300,300);
        }
        
        public override void OnStartServer()
        {
            if (connectionToClient == null)
            {
                return;
            }
            BetterCoreManagement.Log.LogDebug($"OnStartServer() for Network ID {netId}");
                
            if (!hasAuthority)
            {
                NetworkNetworkID = (string)connectionToClient.authenticationData;
                playerID = Platform.mgr.ClientIdFromString(NetworkID);
                BetterCoreManagement.Log.LogDebug($"No Authority for Network ID {netId} / Player ID {playerID}");
            }
            else
            {
                NetworkNetworkID = "host";
                playerID = Platform.mgr.LocalPlayerId();
                BetterCoreManagement.Log.LogDebug($"Has Authority for Network ID {netId} / Player ID {playerID}");
            }
        }

        public override void OnStopClient()
        {
            if (connectionToClient == null)
            {
                return;
            }
            BetterCoreManagement.Log.LogDebug($"OnStopClient() for Network ID {netId}");
            CancelInvoke(nameof(RequestCoreCount));
            _clients.Remove(connectionToClient);
        }

        private void InvokeRequestCoreCount()
        {
            RequestCoreCount();
        }
        [Command]
        public void RequestCoreCount(NetworkConnectionToClient sender = null)
        {
            if (isServer)
                return;
            BetterCoreManagement.Log.LogDebug($"Requesting Core Count");
            PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
            SendCommandInternal(typeof(CoreNetwork), nameof(RequestCoreCount), writer, 0);
            NetworkWriterPool.Recycle(writer);
        }

        [TargetRpc]
        public void LoadCoreCountFromServer(
            NetworkConnection connection)
        {
            BetterCoreManagement.Log.LogDebug($"Core Count Requested by NetworkedPlayer");
            var writer = NetworkWriterPool.GetWriter();
            var exportKeyList = new List<int>();
            var exportValueList = new List<int>();
            foreach (var coreCount in BetterCoreManagement.VirtualCoreCounts)
            {
                exportKeyList.Add((int)coreCount.Key);
                exportValueList.Add(coreCount.Value);
            }
            writer.WriteList(exportKeyList);
            writer.WriteList(exportValueList);
            SendTargetRPCInternal(connection, typeof(CoreNetwork), nameof(LoadCoreCountFromServer), writer, 0);
            NetworkWriterPool.Recycle(writer);
        }

        private void UserCode_RequestCoreCount(NetworkConnectionToClient sender)
        {
            StartCoroutine(PackageCoreCount(sender));
        }

        private void UserCode_LoadCoreCountFromServer(NetworkConnection connection,
            List<int> keyList, List<int> valueList)
        {
            BetterCoreManagement.Log.LogDebug($"Core Count Received by Host/Server");
            for (int i = 0; i < keyList.Count; i++)
            {
                BetterCoreManagement.VirtualCoreCounts[(ResearchCoreDefinition.CoreType)keyList[i]] = valueList[i];
            }
        }

        private IEnumerator PackageCoreCount(NetworkConnectionToClient sender)
        {
            while (SaveState.isSaving)
                yield return null;

            LoadCoreCountFromServer(sender);
        }

        #endregion
    }
}