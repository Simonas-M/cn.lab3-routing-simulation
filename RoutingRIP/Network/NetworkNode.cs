﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace RoutingRIP
{
    public class NetworkNode
    {
        public string Name;
        public Guid Id;
        private bool _isOffline = false;
        private List<NetworkNodeConnection> _connectedNodes = new List<NetworkNodeConnection>();
        private List<NetworkNode> _failingNodes = new List<NetworkNode>();

        public List<NetworkNodeConnection> ConnectedNodes { get { return _connectedNodes; } }

        public NetworkNode(string name, bool IsOffline = false)
        {
            Name = name;
            Id = Guid.NewGuid();
            _isOffline = IsOffline;
        }

        public void ChangeMode(bool mode)
        {
            _isOffline = mode;
        }

        public bool Pong()
        {
            return !_isOffline;
        }

        public void ConnectNode(NetworkNode node)
        {
            if (!_connectedNodes.Any(x => x.HopCount == 1 && x.To == node))
                _connectedNodes.Add(new NetworkNodeConnection { HopCount = 1, To = node });
        }

        public void DisconnectNode(NetworkNode node)
        {
            _connectedNodes.RemoveAll(x => (x.HopCount == 1 && x.To == node) || x.Through == node);
        }

        public List<NetworkNodeConnection> GetConnectedNodes()
        {
            return _connectedNodes;
        }

        public void GotPinged(NetworkNode pinger, List<NetworkNodeConnection> pingerNodes)
        {
            if (_isOffline)
                return;

            Console.WriteLine(Name + " got pinged by " + pinger.Name);

            if (!_connectedNodes.Any(x => x.To == pinger))
                ConnectNode(pinger);

            _connectedNodes.RemoveAll(x => x.Through == pinger && !pingerNodes.Contains(x));

            foreach (var node in pingerNodes)
            {
                if (node.To == this || node.Through == this)
                    continue;
                if (_connectedNodes.Any(x => x.To == node.To && x.HopCount > node.HopCount + 1))
                {
                    var myNode = _connectedNodes.SingleOrDefault(x => x.To == node.To);
                    myNode.HopCount = node.HopCount + 1;
                    myNode.Through = pinger;
                }
                if (!_connectedNodes.Any(x => x.To == node.To))
                    _connectedNodes.Add(new NetworkNodeConnection { HopCount = node.HopCount + 1, To = node.To, Through = pinger });
            }
        }

        public void SendData(NetworkNode nodeTo, string data)
        {
            Console.WriteLine(Name + " received data");
            if(nodeTo == this)
                Console.WriteLine(Name + ": I received data which was sent to me - " + data);
            else
            {
                if (_connectedNodes.Any(x => x.To == nodeTo))
                    if(_connectedNodes.Single(x => x.To == nodeTo).HopCount != 1)
                        _connectedNodes.Single(x => x.To == nodeTo).Through.SendData(nodeTo, data);
                    else
                        _connectedNodes.Single(x => x.To == nodeTo).To.SendData(nodeTo, data);
                else
                    throw new InvalidOperationException("Currently there is no path between " + Name + " and " + nodeTo.Name);
            }            
        }

        public void Ping(NetworkNode neighborNode)
        {
            Console.WriteLine(Name + " is pinging " + neighborNode.Name);
            if (neighborNode.Pong())
            {
                if (_failingNodes.Contains(neighborNode))
                    _failingNodes.Remove(neighborNode);
                neighborNode.GotPinged(this, _connectedNodes);
            }
            else
            {
                _connectedNodes.RemoveAll(x => x.To == neighborNode || x.Through == neighborNode);
                if (!_failingNodes.Contains(neighborNode))
                    _failingNodes.Add(neighborNode);
            }
        }

        public void PingAll()
        {
            if (_isOffline)
                return;

            foreach (NetworkNodeConnection node in _connectedNodes.Where(x => x.HopCount == 1).ToList())
            {
                Ping(node.To);
            }
        }
    }
}
