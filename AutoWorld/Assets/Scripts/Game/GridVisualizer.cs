
using System.Text;
using AutoWorld.Core;
using AutoWorld.Core.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace AutoWorld.Game
{
    /// <summary>
    /// FieldManager의 그리드 상태를 텍스트로 시각화하여 UI에 표시합니다.
    /// </summary>
    public class GridVisualizer : MonoBehaviour
    {
        [Tooltip("그리드를 표시할 UI Text 컴포넌트")]
        public Text gridText;

        private IGameSession session;
        private float timer;
        private const float UpdateInterval = 0.5f; // 0.5초마다 그리드를 업데이트합니다.

        private void Awake()
        {
            if (gridText == null)
            {
                gridText = GetComponentInChildren<Text>(true);
            }
        }

        public void SetSession(IGameSession gameSession)
        {
            session = gameSession;
        }

        private void Update()
        {
            if (session == null || gridText == null)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= UpdateInterval)
            {
                timer = 0f;
                UpdateGridVisual();
            }
        }

        private void UpdateGridVisual()
        {
            if (session.Fields == null)
            {
                return;
            }

            var fieldManager = session.Fields;
            var coordinates = fieldManager.Coordinates;
            if (coordinates.Count == 0)
            {
                gridText.text = "Grid not initialized.";
                return;
            }

            // FieldManager에서 그리드의 경계를 가져옵니다.
            // (FieldManager에 min/max 프로퍼티가 public으로 노출되어야 합니다.)
            // 우선은 모든 좌표를 순회하여 경계를 직접 계산합니다.
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var coord in coordinates.Keys)
            {
                if (coord.X < minX) minX = coord.X;
                if (coord.X > maxX) maxX = coord.X;
                if (coord.Y < minY) minY = coord.Y;
                if (coord.Y > maxY) maxY = coord.Y;
            }

            var sb = new StringBuilder();
            for (int y = maxY; y >= minY; y--)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var coord = new FieldCoordinate(x, y);
                    if (coordinates.TryGetValue(coord, out var fieldState))
                    {
                        sb.Append(GetAsciiCharForField(fieldState.Definition.Type));
                    }
                    else
                    {
                        sb.Append(' '); // 비어있는 공간
                    }
                }
                sb.AppendLine();
            }

            gridText.text = sb.ToString();
        }

        private char GetAsciiCharForField(FieldType type)
        {
            switch (type)
            {
                case FieldType.UnoccupiedField: return '.';
                case FieldType.BadLand: return '·'; //┼, ■, ·
                case FieldType.CropField: return 'C';
                case FieldType.LumberMill: return 'L';
                case FieldType.Quarry: return 'Q';
                case FieldType.Residence: return 'H';
                case FieldType.Residence2: return 'H';
                case FieldType.ExplorationOffice: return 'E';
                case FieldType.Smithy: return 'S';
                case FieldType.TownHall: return 'T';
                case FieldType.Transforming: return '?';
                default: return '#';
            }
        }
    }
}
