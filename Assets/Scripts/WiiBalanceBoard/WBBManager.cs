using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WiiBalanceBoard
{
    public class WBBManager : MonoBehaviour
    {
        // Properties
        public BalanceBoard Board
        {
            get { return _boardInstance; }
        }

        // Private
        private BalanceBoard _boardInstance;

        private void Awake()
        {
            _boardInstance = new BalanceBoard();
        }

        /// <summary>
        /// Callback to handle events received
        /// from a connected Wii balance board.
        /// The callback runs on the Unity main thread.
        /// </summary>
        public void OnBalanceBoardReceive(float[] wbbdata)
        {
            _boardInstance.SetBoard(wbbdata);
        }
    }
}


