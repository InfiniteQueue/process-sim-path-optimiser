using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tecnomatix.Engineering;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;

namespace PathOptimiser.OptimisationSystem.Services
{
    internal class PlayToVia
    {
        DuplicateOperation duplicateOperation;
        ITxCompoundOperation ParentOp => Via.Collection as ITxCompoundOperation;
        TxSimulationPlayer SimPlayer;

        public PlayToVia()
        {
        }

        public void Run(ITxRoboticLocationOperation via, TxSimulationPlayer simPlayer)
        {
            this.SimPlayer = simPlayer;
            this.Via = via;
            this.duplicateOperation = new DuplicateOperation(via, ParentOp as CompoundOp);
            Debug.WriteLine($"Running to via {Via.Name} before envelope duplication");

            TecnomatixStatics.SafePlaySim(simPlayer, duplicateOperation.EnvCopyOperation, false);

            duplicateOperation.Dispose();
        }

        public ITxRoboticLocationOperation Via;


        public class DuplicateOperation : IDisposable
        {

            public DuplicateOperation(LocOp FinalVia, CompoundOp OriginalOperation)
            {
                this.finalVia = FinalVia;
                this.OriginalOperation = OriginalOperation;
                DuplicateEnvelope();
                CleanUpWelds();
                EnsureLocationsMatch();
            }

            LocOp finalVia;
            public CompoundOp OriginalOperation;
            public CompoundOp EnvCopyOperation;
            public Hashtable copyPasteHash;
            public Dictionary<ITxObject, ITxObject> DuplicateToOriginal;
            bool DebugDontDelete => false;

            public void Dispose()
            {
                EnvCopyOperation.Delete();
            }

            #region Helper Methods

            private void EnsureLocationsMatch()
            {
                foreach (var obj in copyPasteHash.Keys) if (obj is ITxLocatableObject locatable && copyPasteHash[obj] is ITxLocatableObject target) target.Locate(locatable.AbsoluteLocation);
            }

            void DuplicateEnvelope()
            {
                TxOperationRoot opRoot = GetRoot();
                EnvCopyOperation = opRoot.Paste(OriginalOperation);
                EnvCopyOperation.Name = "Optimiser_" + EnvCopyOperation.Name;

                foreach (var via in EnvCopyOperation.Vias().ToArray()) via.Delete();
                EnvCopyOperation.Paste(GetObjectList(OriginalOperation), out copyPasteHash);

                DuplicateToOriginal = new Dictionary<ITxObject, ITxObject>();
                foreach (var item in copyPasteHash.Keys) DuplicateToOriginal.Add(copyPasteHash[item] as ITxObject, item as ITxObject);
            }

            TxObjectList GetObjectList(CompoundOp originalOperation)
            {
                var txObjList = new TxObjectList();

                foreach (var item in OriginalOperation.GetDirectDescendants(new TxNoTypeFilter()).OfType<ITxLocationOperation>()) {
                    txObjList.Add(item);
                    if (item == finalVia) break;
                }
                return txObjList;
            }

            void CleanUpWelds()
            {
                var keys = copyPasteHash.Keys.OfType<ITxObject>().ToArray();
                var values = copyPasteHash.Values.OfType<ITxObject>().ToArray();
                foreach (var weld in EnvCopyOperation.GetDirectDescendants(new TxTypeFilter(typeof(TxWeldLocationOperation))).OfType<TxWeldLocationOperation>()) {
                    try {
                        foreach (var item in weld.Commands.ToList()) {
                            if (item.Name.ToLower().Contains("wait")) { weld.Commands.Remove(item); }
                        }
                        var waitParam = new TxRoboticDoubleParam("SW_WAIT_TIME", 0);
                        weld.SetParameter(waitParam);
                    }
                    catch { }
                }
            }

            TxOperationRoot GetRoot()
            {
                var collection = OriginalOperation.Collection;
                while (collection is TxOperationRoot == false) collection = collection.Collection;
                return collection as TxOperationRoot;
            }

            public IEnumerable<ITxLocationOperation> OriginalToCopy(IEnumerable<ITxLocationOperation> OriginalVias) =>
                OriginalVias.Select(x => copyPasteHash.ContainsKey(x) ? (LocOp)copyPasteHash[x] : null).Where(x => x is ITxLocationOperation);

            public ITxLocationOperation OriginalToCopy(ITxLocationOperation OriginalVias) =>
                OriginalToCopy(new ITxLocationOperation[] { OriginalVias }).FirstOrDefault();
            #endregion
        }


        //private void SafePlaySim()
        //{
        //    if (SimPlayer == null) SimPlayer = new TxSimulationPlayer(false, true);

        //    //ActiveDocument.CurrentOperation = Data.Operation;
        //    SimPlayer.AskUserForReset(false);
        //    /*if (ActiveDocument.CurrentOperation != Data.Operation)*/
        //    SimPlayer.SetOperation(duplicateOperation.EnvCopyOperation);
        //    SimPlayer.Pause();
        //    SimPlayer.Stop();
        //    SimPlayer.AskUserForReset(false);
        //    SimPlayer.Rewind();

        //    var debugTime = DateTime.Now;
        //    SimPlayer.AskUserForReset(false);
        //    SimPlayer.PlayWithoutRefresh();

        //    var errors = SimPlayer.GetErrorsAndTraces().Where(x => x.Contains("[Error]"));
        //    if (errors.Count() > 0) {
        //        Debug.WriteLine(string.Join("\n", errors));
        //        PathSolver.IsCancelRequested = true;
        //    }
        //}

    }
}
