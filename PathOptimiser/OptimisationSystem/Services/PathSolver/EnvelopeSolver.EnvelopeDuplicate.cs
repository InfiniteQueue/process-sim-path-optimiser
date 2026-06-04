using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tecnomatix.Engineering;
using CompoundOp = Tecnomatix.Engineering.ITxRoboticOrderedCompoundOperation;
using LocOp = Tecnomatix.Engineering.ITxRoboticLocationOperation;

namespace PathOptimiser.OptimisationSystem.Services.PathSolver
{
    public partial class EnvelopeSolver
    {
        public class EnvelopeDuplicate : IDisposable
        {
            CollisionEnvelope env { get; }
            public CompoundOp OriginalOperation { get; }
            public CompoundOp EnvCopyOperation { get; }

            public Hashtable copyPasteHash;
            public Dictionary<ITxObject, ITxObject> DuplicateToOriginal;

            bool DebugDontDelete => false;

            public static int EnvelopeViaExtensionCount => 3;

            public EnvelopeDuplicate(CollisionEnvelope env, CompoundOp OriginalOperation)
            {
                this.OriginalOperation = OriginalOperation;
                this.env = env;

                this.EnvCopyOperation = GenerateDuplicateOperation();

                InitialiseOperation();

                Debug.WriteLine($"Opened duplicate for {OriginalOperation.Name}, {copyPasteHash.Keys.OfType<ITxLocationOperation>().Count()} items");
                Debug.WriteLine($"{EnvCopyOperation.Vias().First().Name} to {EnvCopyOperation.Vias().Last().Name}");
            }
            TxObjectList GetObjectsToCopyList(CompoundOp originalOperation)
            {
                var viaList = env.ExtendBy(EnvelopeViaExtensionCount, EnvelopeViaExtensionCount);
                var txObjList = new TxObjectList();

                foreach (var item in OriginalOperation.GetDirectDescendants(new TxNoTypeFilter()).OfType<ITxLocationOperation>()) {
                    if (viaList.Contains(item)) txObjList.Add(item);
                }

                var originalViaList = originalOperation.Vias().ToList();

                ViaIndexesNameExtension = $"_Vias {originalViaList.IndexOf(viaList.First())} to {originalViaList.IndexOf(viaList.Last())}";

                return txObjList;
            }


            CompoundOp GenerateDuplicateOperation()
            {
                TxOperationRoot opRoot = GetRoot();
                var newOperation = opRoot.Paste(OriginalOperation);
                newOperation.Name = "SubOperation " + newOperation.Name;

                foreach (var via in newOperation.Vias().ToArray()) via.Delete();

                var copyList = GetObjectsToCopyList(OriginalOperation);
                newOperation.Paste(copyList, out copyPasteHash);

                DuplicateToOriginal = new Dictionary<ITxObject, ITxObject>();
                foreach(var item in copyPasteHash.Keys) {
                    DuplicateToOriginal.Add(copyPasteHash[item] as ITxObject, item as ITxObject);
                }

                return newOperation;
            }


            public void Dispose()
            {
                var copiedVias = EnvCopyOperation.Vias();
                var keys = copyPasteHash.Keys.Cast<ITxObject>().ToArray();
                var values = copyPasteHash.Values.Cast<ITxObject>().ToArray();
                

                //Copy speeds and cnts back to original
                for (var i = 0; i < copyPasteHash.Count; i++) {
                    if (keys[i] is LocOp key == false) continue;
                    if (values[i] is LocOp value == false) continue;

                    if (value == copiedVias.First() || value == copiedVias.Last()) continue;
                    key.SetSpeed(value.GetSpeed());
                    key.SetCNT(value.GetCNT());
                }


                if (DebugDontDelete) {
                    Debug.WriteLine($"---{nameof(EnvelopeDuplicate)} DEBUG SUBOPERATION DELETE SKIP---");
                    return;
                }

                else {
                    try {
                        Debug.WriteLine($"Closed duplicate for {OriginalOperation.Name}, {EnvCopyOperation.Vias().First().Name} to {EnvCopyOperation.Vias().Last().Name}");
                        EnvCopyOperation.Delete();
                    }
                    catch { }
                }
            }

            #region Helper Methods
            void InitialiseOperation()
            {
                EnvCopyOperation.Vias().First().SetCNT(null);
                EnvCopyOperation.Vias().Last().SetCNT(null);
                CleanUpWelds();
                EnsureLocationsMatch();
            }

            private void EnsureLocationsMatch()
            {
                foreach (var obj in copyPasteHash.Keys) {
                    
                    if (obj is ITxLocatableObject locatable && copyPasteHash[obj] is ITxLocatableObject target) {
                        target.Locate(locatable.AbsoluteLocation);

                        if ((obj as ITxLocatableObject).AttachmentParent is ITxLocatableObject attachParent) {
                            if (target.AttachmentParent != attachParent) {
                                target.Detach();
                                target.AttachTo(attachParent);
                            }
                        }
                    }
                }
            }
            string _viaIndexesNameExtension;
            string ViaIndexesNameExtension
            {
                get => _viaIndexesNameExtension;
                set {
                    if (EnvCopyOperation == null) return;
                    var oldName = EnvCopyOperation.Name;
                    var oldViaIndexesString = _viaIndexesNameExtension;
                    EnvCopyOperation.Name = oldName.Substring(0, oldName.Length - (oldViaIndexesString?.Length ?? 0));
                    EnvCopyOperation.Name += value;

                    _viaIndexesNameExtension = value;
                }
            }

            void CleanUpWelds()
            {
                var keys = copyPasteHash.Keys.OfType<ITxObject>().ToArray();
                var values = copyPasteHash.Values.OfType<ITxObject>().ToArray();

                foreach (var weld in EnvCopyOperation.GetDirectDescendants(new TxTypeFilter(typeof(TxWeldLocationOperation))).OfType<TxWeldLocationOperation>()) {
                    //Wait commands are sometimes needed to prevent collisions while over devices are driven
                    //Otherwise they slow down simulation, so remove them
                    if (weld.Commands.Any(x => x.Name.ToLower().Contains("drive")) == false) { 
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
                OriginalToCopy( new ITxLocationOperation[] { OriginalVias }).FirstOrDefault();

            #endregion
        }

    }
}
