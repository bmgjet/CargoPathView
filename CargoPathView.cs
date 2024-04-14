/*▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
 ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
 ░             ░         ░  ░   ░      ░  ░*/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("CargoPathView", "bmgjet", "0.0.1")]
    class CargoPathView : RustPlugin
    {
        private int ViewDistance = 2000;
        private Coroutine _coroutine = null;
        private void OnServerInitialized(bool initial) { _coroutine = ServerMgr.Instance.StartCoroutine(Startup(initial)); }
        private void Unload() { if (_coroutine != null) { ServerMgr.Instance.StopCoroutine(_coroutine); } }

        private void GenHarborPath()
        {
            if (CargoShip.startHarborApproachNodes == null || CargoShip.startHarborApproachNodes.Count <= 0)
            {
                CargoShip.startHarborApproachNodes = new System.Collections.Generic.List<int?>();
                foreach (BasePath basePath in CargoShip.harborApproachPaths)
                {
                    float num = float.MaxValue;
                    int num2 = -1;
                    for (int i = 0; i < TerrainMeta.Path.OceanPatrolFar.Count; i++)
                    {
                        Vector3 vector = TerrainMeta.Path.OceanPatrolFar[i];
                        Vector3 position = basePath.nodes[0].Position;
                        float num3 = Vector3.Distance(vector, position);
                        float num4 = num3;
                        Vector3 vector2 = Vector3.up * 3f;
                        if (!GamePhysics.LineOfSightRadius(vector + vector2, position + vector2, 1084293377, 3f, null)) { num4 *= 20f; }
                        if (num4 < num)
                        {
                            num = num4;
                            num2 = i;
                        }
                    }
                    if (num2 == -1)
                    {
                        Debug.LogError("Cargo couldn't find harbor approach node. Are you sure ocean paths have been generated?");
                        break;
                    }
                    CargoShip.startHarborApproachNodes.Add(new int?(num2));
                }
            }
        }

        private IEnumerator Startup(bool initial)
        {
            yield return CoroutineEx.waitForSeconds(initial ? 10 : 3); //Add delay on startup more for first start
            //Variables
            int nodeindex = 0;
            int harbours = 0;
            Vector3 LastNode = Vector3.zero;
            Vector3 FirstNode = Vector3.zero;
            Vector3 Start = Vector3.zero;
            Vector3 End = Vector3.zero;
            Dictionary<Vector3, Vector3> Cached = new Dictionary<Vector3, Vector3>();
            Vector3[] Cargopath = TerrainMeta.Path.OceanPatrolFar.ToArray();
            //Reverse List Order
            Array.Reverse(Cargopath);
            //Create Harbor path if no cargoship has already created it.
            GenHarborPath();
            yield return CoroutineEx.waitForSeconds(0.0035f);
            //Loop
            while (_coroutine != null)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    //Get Each Admin Thats Awake
                    if (player != null && player.IsAdmin && !player.IsSleeping())
                    {
                        yield return CoroutineEx.waitForSeconds(0.0035f);
                        //Draw cargo path on map
                        nodeindex = 0;
                        LastNode = Vector3.zero;
                        foreach (Vector3 vector in Cargopath)
                        {
                            if (Vector3.Distance(vector, player.transform.position) < ViewDistance)
                            {
                                Vector3 pos = vector;
                                pos.y = 1;
                                player.SendConsoleCommand("ddraw.sphere", 7, Color.blue, pos, 25f);
                                if (LastNode != Vector3.zero) { player.SendConsoleCommand("ddraw.line", 7, Color.blue, pos, LastNode); player.SendConsoleCommand("ddraw.line", 7, Color.blue, pos, LastNode); }
                                player.SendConsoleCommand("ddraw.text", 7, Color.white, pos, "<size=30>" + nodeindex.ToString() + "</size>");
                                LastNode = pos;
                            }
                            else { LastNode = Vector3.zero; }
                            nodeindex++;
                        }
                        yield return CoroutineEx.waitForSeconds(0.0035f);
                        //Draw trigger points on map
                        foreach (var entrynode in CargoShip.startHarborApproachNodes)
                        {
                            if (entrynode != null)
                            {
                                Vector3 pos = TerrainMeta.Path.OceanPatrolFar[(int)entrynode];
                                pos.y = 1;
                                player.SendConsoleCommand("ddraw.text", 7, Color.red, pos, "<size=30>Trigger</size>");
                            }
                        }
                        //Draw hardbor path on map
                        harbours = 0;
                        foreach (var path in CargoShip.harborApproachPaths)
                        {
                            LastNode = Vector3.zero;
                            FirstNode = Vector3.zero;
                            nodeindex = 0;
                            yield return CoroutineEx.waitForSeconds(0.0035f);
                            foreach (var node in path.nodes)
                            {
                                Vector3 vector = node.transform.position;
                                if (Vector3.Distance(vector, player.transform.position) < ViewDistance)
                                {
                                    Vector3 pos = vector;
                                    pos.y = 1;
                                    if (LastNode != Vector3.zero) { player.SendConsoleCommand("ddraw.line", 7, Color.green, pos, LastNode); player.SendConsoleCommand("ddraw.line", 7, Color.green, pos, LastNode); }
                                    LastNode = pos;
                                    player.SendConsoleCommand("ddraw.sphere", 7, Color.green, pos, 25f);
                                    player.SendConsoleCommand("ddraw.text", 7, Color.white, pos, "<size=30>" + nodeindex.ToString() + "</size>");
                                    nodeindex++;
                                }
                            }
                            yield return CoroutineEx.waitForSeconds(0.0035f);
                            //Cache end lines to save recalculating it each time.
                            if (Cached.Count == 0)
                            {
                                End = Vector3.zero;
                                float olddist = 9999;
                                for (int i = 0; i < Cargopath.Length - 1; i++)
                                {
                                    float dist = Vector3.Distance(TerrainMeta.Path.OceanPatrolFar[i], LastNode);
                                    if (olddist >= dist)
                                    {
                                        olddist = dist;
                                        End = TerrainMeta.Path.OceanPatrolFar[i];
                                    }
                                }
                                End.y = 1;
                                Cached.Add(End, LastNode);
                                Start = TerrainMeta.Path.OceanPatrolFar[(int)CargoShip.startHarborApproachNodes[harbours++]];
                                Start.y = 1;
                                FirstNode = path.nodes[0].transform.position;
                                FirstNode.y = 1;
                                Cached.Add(Start, FirstNode);
                            }
                        }
                        //Draw end lines
                        if (Cached.Count != 0)
                        {
                            foreach (var line in Cached) { player.SendConsoleCommand("ddraw.line", 7, Color.green, line.Key, line.Value); }
                        }
                    }
                }
                yield return CoroutineEx.waitForSeconds(6);
            }
        }
    }
}