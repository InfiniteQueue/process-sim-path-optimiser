using System;
using System.Diagnostics;
using System.Linq;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    public class Optimiser
    {
        ViaDataCollection collection;
        public Optimiser(ViaDataCollection collection, OptimisationModes optimisationMode)
        {
            this.collection = collection;
            this.OptimisationMode = optimisationMode;
        }

        public OptimisationModes OptimisationMode;

        public enum OptimisationModes
        {
            Speed,
            CNT,
            SpeedAndCNT
        }

        public void Run(TxSimulationPlayer simPlayer)
        {
            Debug.WriteLine($"{nameof(Optimiser)}: Running");

            SetAllToMinimum();

            foreach (var via in collection.OrderBy(x => x.Via.Index())) {
                LockInVia(via);
            }

            //if (collection.GetCollisionReport() != null)  {
            //    Debug.WriteLine($"{nameof(OperationOptimiser)}: Impossible Operation");
            //    return;
            //}

            //CollisionReport colReport = null;

            //do {
            //    colReport = collection.GetCollisionReport();

            //    if (colReport != null)
            //    {
            //        Console.WriteLine("Collision detected via " + colReport.Via.Name + " with clearance " + colReport.Clearance);
            //        DoOptimisationStep(colReport, out var impossible);

            //        if (impossible)
            //        {
            //            Console.WriteLine("Impossible to optimise further, stopping.");
            //            Debugger.Break();
            //            break;
            //        }
            //    }

            //} while (colReport != null);


            Debug.WriteLine($"{nameof(Optimiser)}: Finished");
        }

        void LockInVia(ViaData via)
        {
            CollisionReport BestReport = null;
            for (var currentReport = collection.GetCollisionReport(); 
                IterationValid(currentReport, via.Via); 
                currentReport= collection.GetCollisionReport()) {


                if (currentReport == null) currentReport = new CollisionReport() { Via = via.Via, Clearance = 999999, CNT = via.Via.GetCNT(), Speed = via.Via.GetSpeed(), OperationTime = TxApplication.ActiveDocument.SimulationPlayer.CurrentTime };
                if (currentReport.Via != via.Via) { currentReport.Via = via.Via; currentReport.CNT = via.Via.GetCNT(); currentReport.Speed = via.Via.GetSpeed(); }
                BestReport = currentReport;

                DoOptimisationStep(currentReport, out bool impossible);

                if (impossible) break;
            }
            BestReport = BestReport ?? GetDefaultReport(via.Via);
            BestReport?.SetToTheseValues();

            if (BestReport != null) Debug.WriteLine($"{via.Via.Name} locked in values {BestReport.Speed},{BestReport.CNT}");
        }

        private bool IterationValid(CollisionReport currentReport, ITxLocationOperation locOp)
        {
            if (currentReport == null) {
                Debug.WriteLine($"{nameof(IterationValid)}: No report found");
                return true;
            }
            if (currentReport.Via.Index() > locOp.Index()) return true;

            Debug.WriteLine($"Clearance: {currentReport.Clearance}");

            if (currentReport.Via.Index() < locOp.Index()) {
                Debug.WriteLine($"{nameof(IterationValid)}: Prev via broken {currentReport.Clearance}");
                return false;
            }

            else  {
                Debug.WriteLine($"{nameof(IterationValid)}: Collision Detected {currentReport.Clearance}");
                return false;
            }
        }

        public void SetAllToMinimum()
        {
            foreach (var item in collection) {
                if (OptimisationMode == OptimisationModes.CNT || OptimisationMode == OptimisationModes.SpeedAndCNT) item.Via.SetCNT(0);
                if (OptimisationMode == OptimisationModes.Speed || OptimisationMode == OptimisationModes.SpeedAndCNT) item.Via.SetSpeed(10);
            }
        }

        private CollisionReport GetDefaultReport(ITxLocationOperation Via)
        {
            return new CollisionReport() { Via = Via, Clearance = 999999, CNT = Via.GetCNT(), Speed = Via.GetSpeed(), OperationTime = TxApplication.ActiveDocument.SimulationPlayer.CurrentTime };
        }

        void DoOptimisationStep(CollisionReport colReport, out bool impossible)
        {
            impossible = false;
            if (OptimisationMode == OptimisationModes.Speed) SpeedStep(colReport, out impossible);
            else if (OptimisationMode == OptimisationModes.CNT) CNTStep(colReport, out impossible);
            else if (OptimisationMode == OptimisationModes.SpeedAndCNT) SpeedAndCNTStep(colReport, out impossible);
        }

        void CNTStep(CollisionReport colReport, out bool Impossible)
        {
            Impossible = colReport.Via.GetCNT() >= 100;
            colReport.Via.SetCNT(Math.Min(100, colReport.Via.GetCNT() + 10));
        }
        void SpeedStep(CollisionReport colReport, out bool Impossible)
        {
            Impossible = colReport.Via.GetSpeed() >= 100;
            colReport.Via.SetSpeed(Math.Min(100, colReport.Via.GetSpeed() + 10));
        }

        void SpeedAndCNTStep(CollisionReport colReport, out bool Impossible)
        {
            Impossible = false;
            double? CNTDelta = null;
            double? speedDelta = null;

            if (colReport?.CNT == 100 && colReport?.Speed == 100) {
                Impossible = true;
                return;
            }

            if (colReport.Speed != 100 && colReport.CNT != 100) {
                if (colReport.CNT < 100) {
                    colReport.Via.SetCNT(Math.Max(colReport.Via.GetCNT() + 10, 100));
                    var CNTReport = collection.GetCollisionReport() ?? GetDefaultReport(colReport.Via);
                    CNTDelta = (colReport.Clearance - CNTReport.Clearance) / Math.Max((colReport.OperationTime - CNTReport.OperationTime), 0.01);
                }

            if (colReport.Speed < 100) {
                colReport.Via.SetSpeed(colReport.Via.GetSpeed() + 10);
                var speedReport = collection.GetCollisionReport() ?? GetDefaultReport(colReport.Via);
                speedDelta = (colReport.Clearance - speedReport.Clearance) / Math.Max((colReport.OperationTime - speedReport.OperationTime), 0.01);
            }
        }

            colReport.Via.SetSpeed(colReport.Speed);
            colReport.Via.SetCNT(colReport.CNT);

            Debug.WriteLine($"CNT {CNTDelta} : Speed {speedDelta}");

            if ((colReport.Speed == 100 && colReport.CNT != 100  || CNTDelta < speedDelta)) {
                Debug.WriteLine($"CNT up to {Math.Min(colReport.CNT + 10, 100)}");
                colReport.Via.SetCNT(Math.Min(colReport.CNT + 10, 100));
            }

            else if ((colReport.Speed != 100 && colReport.CNT == 100 || speedDelta <= CNTDelta)) {
                Debug.WriteLine($"Speed up to {Math.Min(colReport.Speed + 10, 100)}");
                colReport.Via.SetSpeed(Math.Min(colReport.Speed + 10, 100));
            }
        }

        //void DoOptimisationStep(CollisionReport colReport, out bool impossible)
        //{
        //    impossible = false;
        //    if (OptimisationMode == OptimisationModes.Speed) SpeedStep(colReport, out impossible);
        //    else if (OptimisationMode == OptimisationModes.CNT) CNTStep(colReport, out impossible);
        //    else if (OptimisationMode == OptimisationModes.SpeedAndCNT) SpeedAndCNTStep(colReport, out impossible);
        //}

        //void CNTStep(CollisionReport colReport, out bool Impossible)
        //{
        //    Impossible = colReport.Via.GetCNT() < 10;
        //    colReport.Via.SetCNT(Math.Max(0, colReport.Via.GetCNT() - 10));
        //}
        //void SpeedStep(CollisionReport colReport, out bool Impossible)
        //{
        //    Impossible = colReport.Via.GetSpeed() < 10;
        //    colReport.Via.SetSpeed(Math.Max(0, colReport.Via.GetSpeed() - 10));
        //}
        //void SpeedAndCNTStep(CollisionReport colReport, out bool Impossible)
        //{
        //    Impossible = false;
        //    double? CNTDelta = null;
        //    double? speedDelta = null;

        //    if (colReport.CNT == 0 && colReport.Speed <= 1) {
        //        Impossible = true;
        //        return;
        //    }

        //    if (colReport.CNT != 0) {
        //        colReport.Via.SetCNT(colReport.Via.GetCNT() - 10);
        //        var CNTReport = collection.GetCollisionReport();
        //        CNTDelta = (CNTReport.Clearance - colReport.Clearance) / Math.Max( (CNTReport.OperationTime - colReport.OperationTime), 0.01);
        //    }

        //    if (colReport.Speed > 1) {
        //        colReport.Via.SetSpeed(colReport.Via.GetSpeed() - 10);
        //        var speedReport = collection.GetCollisionReport();
        //        speedDelta = (speedReport.Clearance - colReport.Clearance) / Math.Max((speedReport.OperationTime - colReport.OperationTime), 0.01);
        //    }


        //    colReport.Via.SetSpeed(colReport.Speed);
        //    colReport.Via.SetCNT(colReport.CNT);

        //    if (CNTDelta != null && (speedDelta == null || CNTDelta < speedDelta)) {
        //        colReport.Via.SetCNT(Math.Max(colReport.CNT - 10, 0));
        //    }

        //    if (speedDelta != null && (CNTDelta == null || speedDelta <= CNTDelta)) {
        //        colReport.Via.SetSpeed(Math.Max(1, colReport.Speed - 10));
        //    }
        //}
    }
}
