﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RPG
{
    public class WallTools : MonoBehaviour
    {
        [SerializeField] private Color wallColor;
        [SerializeField] private Color doorColor;
        [SerializeField] private Color invisibleColor;
        [SerializeField] private Color hiddenDoorColor;
        [SerializeField] private LineController controllerPrefab;
        [SerializeField] private Transform controllerParent;

        private WallType wallType;
        private Color color;
        public bool Enabled = true;

        public PointController HoverPoint;
        private List<LineController> controllers = new List<LineController>();

        private void Awake()
        {
            color = wallColor;
        }
        private void Update()
        {
            for (int i = controllers.Count - 1; i >= 0; i--)
            {
                if (controllers[i] == null) controllers.RemoveAt(i);
            }
            if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject() && Enabled)
            {
                LineController controller = Instantiate(controllerPrefab, Vector3.zero, Quaternion.identity, controllerParent);
                controller.Init(wallType, this, color);
                controllers.Add(controller);
            }
        }

        public void SendTop(LineController line)
        {
            foreach (var controller in controllers)
            {
                controller.SendTop(controller == line);
            }
        }

        public void EnableWalls(bool enabled)
        {
            Enabled = enabled;
            foreach (var controller in controllers)
            {
                controller.Hide(Enabled);
            }
        }
        public void ChangeWallType(WallType type)
        {
            wallType = type;

            switch (wallType)
            {
                case WallType.Wall:
                    color = wallColor;
                    break;
                case WallType.Door:
                    color = doorColor;
                    break;
                case WallType.Invisible:
                    color = invisibleColor;
                    break;
                case WallType.Hidden_Door:
                    color = hiddenDoorColor;
                    break;
            }
        }
        public void SaveWalls(Action<List<WallData>> callback)
        {
            List<WallData> data = new List<WallData>();

            foreach (var controller in controllers)
            {
                data.Add(controller.SaveData());
                if (data.Count == controllers.Count) callback(data);
            }
        }
        public void LoadWalls(List<WallData> walls)
        {
            if (walls == null) return;

            foreach (var wall in walls)
            {
                LineController controller = Instantiate(controllerPrefab, Vector3.zero, Quaternion.identity, controllerParent);
                switch (wall.model)
                {
                    case WallType.Wall:
                        controller.Load(wall.model, wall.open, this, wallColor);
                        break;
                    case WallType.Door:
                        controller.Load(wall.model, wall.open, this, doorColor);
                        break;
                    case WallType.Invisible:
                        controller.Load(wall.model, wall.open, this, invisibleColor);
                        break;
                    case WallType.Hidden_Door:
                        controller.Load(wall.model, wall.open, this, hiddenDoorColor);
                        break;
                }
                foreach (var point in wall.points)
                {
                    controller.AddPoint(point);
                }

                controllers.Add(controller);
            }

            EnableWalls(false);
        }
    }
}