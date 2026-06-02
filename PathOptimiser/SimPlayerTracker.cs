using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    /// <summary> Handles various events related to the sim player changing </summary>
    public class SimPlayerTracker
    {

        private TxSimulationPlayer _simPlayer;
        public TxSimulationPlayer SimPlayer
        {
            get => _simPlayer; set {
                if (_simPlayer == value) return;

                if (_simPlayer != null) _simPlayer.Ended -= _simPlayer_Ended;

                this.SimPlayerChanged?.Invoke(_simPlayer, value);

                _simPlayer = value;


                if (_simPlayer != null) {
                    _simPlayer.AskUserForReset(false);
                    _simPlayer.Ended += _simPlayer_Ended;
                }

            }
        }


        private void _simPlayer_Ended(object sender, TxSimulationPlayer_EndedEventArgs args)
        {
            //SimPlayer.Stop();
            //SimPlayer = null;
            //Debug.WriteLine($"{nameof(SimPlayerTracker)}: Player Disposed");
        }

        public event Action<TxSimulationPlayer, TxSimulationPlayer> SimPlayerChanged;
    }

}
