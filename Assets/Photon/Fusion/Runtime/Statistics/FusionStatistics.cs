namespace Fusion.Statistics {
  using UnityEngine;

  // Minimal stub: this project doesn't use Fusion's in-game debug stats overlay, but
  // Fusion.Unity.cs (core runtime) still references the FusionStatistics type itself.
  // The original UI implementation shipped with this package had a broken/incomplete
  // internal dependency (FusionStatsGraphBase) after the Fusion 2.1.1 import, so the
  // full stats-overlay feature has been removed rather than partially patched.
  [RequireComponent(typeof(NetworkRunner))]
  [DisallowMultipleComponent]
  public class FusionStatistics : SimulationBehaviour, ISpawned {
    void ISpawned.Spawned() {
    }
  }
}
