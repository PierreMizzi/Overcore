using UnityEngine;
using UnityEngine.Playables;

public class EnemySpawnerAsset : PlayableAsset
{
    public EnemySpawnConfig spawnConfig;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EnemySpawnerBehaviour>.Create(graph);

        EnemySpawnerBehaviour behaviour = playable.GetBehaviour();

        behaviour.spawnConfig = spawnConfig;

        return playable;
    }
}
