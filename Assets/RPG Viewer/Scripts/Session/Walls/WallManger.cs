using System.Collections.Generic;
using UnityEngine;

namespace RPG
{
    public class WallManger : MonoBehaviour
    {
        [SerializeField] private Wall wallPrefab;
        [SerializeField] private Transform wallParent;

        private Dictionary<string, Wall> walls = new Dictionary<string, Wall>();

        private void OnEnable()
        {
            // Add event listeners
            Events.OnStateChanged.AddListener(ReloadWalls);
            Events.OnSceneLoaded.AddListener(LoadWalls);
        }
        private void OnDisable()
        {
            // Remove event listeners
            Events.OnStateChanged.RemoveListener(ReloadWalls);
            Events.OnSceneLoaded.RemoveListener(LoadWalls);
        }

        private void ReloadWalls(SessionState oldState, SessionState newState)
        {
            // Check if we are the master client
            if (ConnectionManager.Info.isMaster)
            {
                // Return if scene was not changed
                if (oldState.scene == newState.scene) return;
                UnloadWalls();
            }
            else
            {
                // Unload tokens if syncing was disabled
                if (oldState.synced && !newState.synced)
                {
                    UnloadWalls();
                    return;
                }

                // Return if scene was not changed
                if (oldState.scene == newState.scene) return;
                UnloadWalls();
            }
        }
        private void UnloadWalls()
        {
            // Loop through each wall
            foreach (var item in walls)
            {
                // Continue if token is null
                if (item.Value == null) continue;
                Destroy(item.Value.gameObject);
            }

            // Clear lists
            walls.Clear();
        }
        private void LoadWalls(SceneData settings)
        {
            // Instantiate walls
            List<WallData> list = settings.walls;
            for (int i = 0; i < list.Count; i++)
            {
                CreateWall(list[i]);
            }
        }

        private void CreateWall(WallData data)
        {
            // Instantiate wall and load its data
            Wall wall = Instantiate(wallPrefab, wallParent);
            wall.LoadData(data);

            // Add wall to dictionary
            walls.Add(data.id, wall);
        }
    }
}
