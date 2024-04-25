using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortAllocator
{
    private int _startingPort;
    private int _endingPort;

    public int StartingPort
    {
        get { return _startingPort; }
        private set { _startingPort = value; }
    }

    public int EndingPort
    {
        get { return _endingPort; }
        private set { _endingPort = value; }
    }

    private Queue<int> _freePorts;
    private int _lastPortTaken = -1;

    public PortAllocator(int startingPort, int endingPort)
    {
        StartingPort = startingPort;
        EndingPort = endingPort;
        _freePorts = new Queue<int>();
    }

    public int GetAvailablePort()
    {
        // Return a port from a list of available ports
        if (_freePorts.Count > 0)
        {
            return _freePorts.Dequeue();
        }

        if (_lastPortTaken < 0)
        {
            _lastPortTaken = StartingPort;
            return _lastPortTaken;
        }

        if (_lastPortTaken < EndingPort)
        {
            _lastPortTaken += 1;
            return _lastPortTaken;
        }

        return -1;
    }

    public void ReleasePort(int port)
    {
        _freePorts.Enqueue(port);
    }

}
