using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tecnomatix.Engineering;

namespace PathOptimiser
{
    internal class DeviceResetter
    {
        static Dictionary<ITxDevice, TxPoseData> StoredPoses = new Dictionary<ITxDevice, TxPoseData>();
        public static void Store()
        {
            StoredPoses.Clear();
            foreach(ITxDevice device in TxApplication.ActiveDocument.PhysicalRoot.GetAllDescendants(new TxTypeFilter(typeof(ITxDevice)))) {
                var pose = device.CurrentPose;
                //StoredPoses.Add(robot, new TxPoseData() { JointValues = new System.Collections.ArrayList(robot.CurrentPose.JointValues) });
                StoredPoses.Add(device, device.CurrentPose);
            }
            Debug.WriteLine("Device positions stored");
        }

        public static void Reload()
        {
            foreach(var pair in StoredPoses) {
                if (pair.Key.CurrentPose.JointValues.ToArray().SequenceEqual(pair.Value.JointValues.ToArray()) == false) {
                pair.Key.CurrentPose = pair.Value;
                }
            }
            Debug.WriteLine("Device positions reloaded");
        }
    }
}
