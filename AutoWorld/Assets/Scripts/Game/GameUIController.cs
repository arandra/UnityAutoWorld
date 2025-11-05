using System;
using System.Collections.Generic;
using System.Text;
using AutoWorld.Core;
using AutoWorld.Core.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AutoWorld.Game
{
    public sealed class GameUIController : MonoBehaviour, IEventListener
    {
        private sealed class MenuNode
        {
            public MenuNode(string title)
            {
                staticTitle = title ?? string.Empty;
                Children = new List<MenuNode>();
            }

            private readonly string staticTitle;

            public Func<string> TitleProvider { get; set; }

            public string Title => TitleProvider != null ? TitleProvider() : staticTitle;

            public MenuNode Parent { get; private set; }

            public List<MenuNode> Children { get; }

            public Action OnSelected { get; set; }

            public void AddChild(MenuNode child)
            {
                if (child == null)
                {
                    return;
                }

                child.Parent = this;
                Children.Add(child);
            }
        }

        private const int MaxMessages = 100;
        private const float StatusRefreshInterval = 0.5f;
        private const int FieldTransformationFailureInsufficientResources = 1;
        private const int FieldTransformationFailurePlacement = 2;
        private const int JobChangeFailureMissingWorker = 1;
        private const int JobChangeFailureInsufficientResource = 2;
        private const int JobChangeFailureNoCitizen = 3;

        [Header("상태 표시")]
        [SerializeField] private Text statusLabel;

        [Header("출력 패널")]
        [SerializeField] private ScrollRect messageScroll;
        [SerializeField] private RectTransform messageContent;
        [SerializeField] private Text messageTemplate;

        [Header("입력 패널")]
        [SerializeField] private RectTransform menuContent;
        [SerializeField] private Button optionButtonTemplate;
        [SerializeField] private Text infoLabelTemplate;
        [SerializeField] private Text pathLabel;
        [SerializeField] private Button backButton;

        private readonly List<GameObject> messageItems = new List<GameObject>();
        private readonly List<GameObject> optionItems = new List<GameObject>();

        private IGameSession session;
        private IEventManager eventManager;
        private bool isRegistered;
        private MenuNode rootNode;
        private MenuNode currentNode;
        private float statusTimer;

        private void Awake()
        {
            EnsureTemplatesDisabled();
            EnsureEventSystem();
        }

        private void Start()
        {
            EnsureMainCamera();
            InitializeSession();
            InitializeMenuTree();
            HookupUI();
            RefreshMenuOptions();
            UpdateMenuPath();
            AddSystemMessage("UI가 준비되었습니다.");
            RefreshStatusImmediate();
        }

        private void Update()
        {
            if (statusLabel == null)
            {
                return;
            }

            statusTimer += Time.deltaTime;
            if (statusTimer < StatusRefreshInterval)
            {
                return;
            }

            statusTimer = 0f;
            UpdateStatusLabel();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
        }

        public void OnEvent(string eventName, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            var color = ResolveEventColor(eventName);
            var message = BuildEventMessage(eventName, source, parameter);
            AddMessage(message, color);
            RefreshStatusImmediate();
        }

        private void InitializeSession()
        {
            if (!CoreRuntime.HasSession)
            {
                AddMessage("게임 세션을 찾을 수 없습니다.", Color.yellow);
                return;
            }

            session = CoreRuntime.Session;
            eventManager = EventManager.Instance;
            RegisterEvents();
            RefreshStatusImmediate();
        }

        private void RegisterEvents()
        {
            if (session == null || eventManager == null || isRegistered)
            {
                return;
            }

            eventManager.RegisterAll(this);

            isRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (!isRegistered || eventManager == null)
            {
                return;
            }

            eventManager.UnregisterAll(this);
            eventManager.Unregister(this);

            isRegistered = false;
        }

        private void InitializeMenuTree()
        {
            rootNode = new MenuNode("메인 메뉴");

            var jobRoot = new MenuNode("직업 변경");
            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                if (job == JobType.Worker)
                {
                    continue;
                }

                var jobNode = new MenuNode(job.ToString())
                {
                    TitleProvider = () => GetJobLabel(job)
                };
                jobNode.AddChild(new MenuNode("증가") { OnSelected = () => RequestJobChange(job, true) });
                jobNode.AddChild(new MenuNode("감소") { OnSelected = () => RequestJobChange(job, false) });
                jobRoot.AddChild(jobNode);
            }

            rootNode.AddChild(jobRoot);

            var fieldRoot = new MenuNode("필드 변경");
            foreach (FieldType fieldType in Enum.GetValues(typeof(FieldType)))
            {
                if (!IsBuildableField(fieldType))
                {
                    continue;
                }

                var fieldNode = new MenuNode(fieldType.ToString())
                {
                    TitleProvider = () => GetFieldLabel(fieldType)
                };
                fieldNode.AddChild(new MenuNode("생성") { OnSelected = () => RequestFieldCreation(fieldType) });
                fieldNode.AddChild(new MenuNode("파괴") { OnSelected = () => RequestFieldRemoval(fieldType) });
                fieldRoot.AddChild(fieldNode);
            }

            rootNode.AddChild(fieldRoot);
            currentNode = rootNode;
        }

        private void HookupUI()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(NavigateBack);
            }

            if (messageTemplate != null)
            {
                messageTemplate.gameObject.SetActive(false);
            }

            if (optionButtonTemplate != null)
            {
                optionButtonTemplate.gameObject.SetActive(false);
            }

            if (infoLabelTemplate != null)
            {
                infoLabelTemplate.gameObject.SetActive(false);
            }
        }

        private void RefreshMenuOptions()
        {
            ClearOptionItems();

            if (currentNode == null)
            {
                return;
            }

            if (currentNode.Children.Count == 0)
            {
                CreateInfoLabel("선택 가능한 항목이 없습니다.");
                return;
            }

            foreach (var child in currentNode.Children)
            {
                CreateOptionButton(child);
            }

            UpdateBackButtonState();
        }

        private void HandleMenuSelection(MenuNode node)
        {
            if (node == null)
            {
                return;
            }

            if (node.Children.Count > 0)
            {
                currentNode = node;
                RefreshMenuOptions();
                UpdateMenuPath();
                return;
            }

            node.OnSelected?.Invoke();
        }

        private void NavigateBack()
        {
            if (currentNode == null || currentNode.Parent == null)
            {
                return;
            }

            currentNode = currentNode.Parent;
            RefreshMenuOptions();
            UpdateMenuPath();
        }

        private void UpdateMenuPath()
        {
            if (pathLabel == null)
            {
                return;
            }

            var segments = new Stack<string>();
            var cursor = currentNode;
            while (cursor != null)
            {
                segments.Push(cursor.Title);
                cursor = cursor.Parent;
            }

            pathLabel.text = string.Join(" / ", segments);
        }

        private void UpdateBackButtonState()
        {
            if (backButton == null)
            {
                return;
            }

            backButton.interactable = currentNode != null && currentNode.Parent != null;
        }

        private void RequestJobChange(JobType job, bool increase)
        {
            if (session == null)
            {
                AddMessage("게임 세션이 준비되지 않았습니다.", Color.yellow);
                return;
            }

            var succeeded = increase ? session.IncreaseJob(job) : session.DecreaseJob(job);
            var actionName = increase ? "증가" : "감소";
            if (succeeded)
            {
                AddMessage($"직업 {job} {actionName} 요청을 보냈습니다.", ResolveInfoColor());
            }
            else
            {
                AddMessage($"직업 {job} {actionName} 요청이 거절되었습니다.", Color.yellow);
            }

            RefreshStatusImmediate();
        }

        private void RequestFieldCreation(FieldType fieldType)
        {
            if (session == null)
            {
                AddMessage("게임 세션이 준비되지 않았습니다.", Color.yellow);
                return;
            }

            var succeeded = session.RequestFieldTransformation(fieldType);
            if (succeeded)
            {
                AddMessage($"필드 {fieldType} 생성 요청을 보냈습니다.", ResolveInfoColor());
            }
            else
            {
                AddMessage($"필드 {fieldType} 생성 요청이 실패했습니다.", Color.yellow);
            }

            RefreshStatusImmediate();
        }

        private void RequestFieldRemoval(FieldType fieldType)
        {
            AddMessage($"필드 {fieldType} 파괴는 아직 지원되지 않습니다.", Color.yellow);
        }

        private bool IsBuildableField(FieldType fieldType)
        {
            if (fieldType == FieldType.UnoccupiedField || fieldType == FieldType.BadLand || fieldType == FieldType.Transforming || fieldType == FieldType.TownHall)
            {
                return false;
            }

            if (session == null)
            {
                return true;
            }

            try
            {
                var definition = session.Fields.GetDefinition(fieldType);
                return definition.ConstructionTicks > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GetJobLabel(JobType job)
        {
            var count = session?.Citizens?.GetJobCount(job) ?? 0;
            return $"{job} ({count})";
        }

        private string GetFieldLabel(FieldType fieldType)
        {
            var count = session?.Fields?.GetFieldCount(fieldType) ?? 0;
            return $"{fieldType} ({count})";
        }

        private void AddSystemMessage(string message)
        {
            AddMessage(message, Color.white);
        }

        private void AddMessage(string message, Color color)
        {
            if (messageContent == null || messageTemplate == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            Debug.Log($"[GameUI] {message}");

            var stickToBottom = messageScroll == null || messageScroll.verticalNormalizedPosition <= 0.001f;

            var item = Instantiate(messageTemplate, messageContent);
            var cloneObject = item.gameObject;
            cloneObject.SetActive(true);
            item.text = message;
            item.color = color;
            item.font = messageTemplate.font;
            item.fontSize = messageTemplate.fontSize;
            item.fontStyle = messageTemplate.fontStyle;

            item.canvasRenderer.SetAlpha(1f);
            item.rectTransform.localScale = Vector3.one;
            item.rectTransform.SetAsLastSibling();
            item.SetAllDirty();

            messageItems.Add(cloneObject);
            if (messageItems.Count > MaxMessages)
            {
                var oldest = messageItems[0];
                messageItems.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent);
            Canvas.ForceUpdateCanvases();
            if (messageScroll != null)
            {
                messageScroll.StopMovement();
                if (stickToBottom)
                {
                    messageScroll.verticalNormalizedPosition = 0f;
                }
            }
        }

        private void ClearOptionItems()
        {
            foreach (var item in optionItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }

            optionItems.Clear();
        }

        private void CreateOptionButton(MenuNode node)
        {
            if (optionButtonTemplate == null || menuContent == null)
            {
                return;
            }

            var button = Instantiate(optionButtonTemplate, menuContent);
            button.gameObject.SetActive(true);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleMenuSelection(node));

            var label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = node.Title;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(240f, 44f);
            }

            optionItems.Add(button.gameObject);
        }

        private void CreateInfoLabel(string message)
        {
            if (infoLabelTemplate == null || menuContent == null)
            {
                return;
            }

            var label = Instantiate(infoLabelTemplate, menuContent);
            label.gameObject.SetActive(true);
            label.text = message;
            optionItems.Add(label.gameObject);
        }

        private string BuildEventMessage(string eventName, EventObject source, EventParameter parameter)
        {
            switch (eventName)
            {
                case GameEvents.PopulationGrowth:
                    return $"{FormatCitizenLabel(parameter)}이(가) 마을에 합류했습니다.";
                case GameEvents.PopulationGrowthPaused:
                    {
                        var (population, capacity) = ExtractPopulationStatus(parameter);
                        return $"인구 성장이 중단되었습니다 ({population}/{capacity}).";
                    }
                case GameEvents.PopulationGrowthResumed:
                    {
                        var (population, capacity) = ExtractPopulationStatus(parameter);
                        return $"인구 성장이 재개되었습니다 ({population}/{capacity}).";
                    }
                case GameEvents.CitizenFoodConsumed:
                    return $"{FormatCitizenLabel(parameter)}이(가) 식사를 마쳤습니다.";
                case GameEvents.CitizenFoodShortage:
                    return $"{FormatCitizenLabel(parameter)}이(가) 식량을 얻지 못했습니다.";
                case GameEvents.MissingMeal:
                    return $"{FormatCitizenLabel(parameter)}이(가) 굶주림으로 위험한 상태입니다.";
                case GameEvents.CitizenDied:
                    return $"{FormatCitizenLabel(parameter)}이(가) {DescribeCitizenDeath(parameter.StringValue)}";
                case GameEvents.SoldierLevelUpgraded:
                    return $"{FormatCitizenLabel(parameter)}이(가) 병사 레벨 {Math.Max(1, parameter.IntValue)}로 승급했습니다.";
                case GameEvents.TaskStarted:
                    return $"{FormatCitizenLabel(parameter)}이(가) '{parameter.StringValue}' 작업을 시작했습니다.";
                case GameEvents.TaskCompleted:
                    return $"{FormatCitizenLabel(parameter)}이(가) '{parameter.StringValue}' 작업을 완료했습니다.";
                case GameEvents.RestStarted:
                    return $"{FormatCitizenLabel(parameter)}이(가) {parameter.StringValue}에서 휴식을 시작했습니다.";
                case GameEvents.RestCompleted:
                    return $"{FormatCitizenLabel(parameter)}이(가) {parameter.StringValue}에서 휴식을 마쳤습니다.";
                case GameEvents.OverflowingTicksForRest:
                    return $"{FormatCitizenLabel(parameter)}이(가) 휴식이 필요합니다.";
                case GameEvents.FieldTransformationStarted:
                    return $"{parameter.StringValue} 변환을 시작했습니다.";
                case GameEvents.FieldTransformationCompleted:
                    return $"{parameter.StringValue} 변환이 완료되었습니다.";
                case GameEvents.FieldTransformationFailed:
                    return DescribeFieldTransformationFailure(parameter);
                case GameEvents.BuildingCompleted:
                    return $"{parameter.StringValue} 건설이 완료되었습니다.";
                case GameEvents.TerritoryExpansion:
                    return "영토가 확장되었습니다.";
                case GameEvents.TerritoryExpansionFailed:
                    return "확장할 수 있는 구역이 없습니다.";
                case GameEvents.JobAssignmentChanged:
                    return $"시민의 직업이 {parameter.StringValue}으로 변경되었습니다.";
                case GameEvents.JobChangeFailed:
                    return DescribeJobChangeFailure(parameter);
            }

            if (eventName.EndsWith("TaskComplete", StringComparison.Ordinal))
            {
                var prefix = eventName.Substring(0, eventName.Length - "TaskComplete".Length);
                var label = ToDisplayName(prefix);
                return $"[{label}] 작업이 완료되었습니다.";
            }

            var fallback = BuildFallbackDetails(parameter);
            var display = ToDisplayName(eventName);
            return fallback.Count == 0 ? $"[{display}]" : $"[{display}] {string.Join(" / ", fallback)}";
        }

        private Color ResolveEventColor(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return Color.white;
            }

            switch (eventName)
            {
                case GameEvents.CitizenDied:
                case GameEvents.MissingMeal:
                    return Color.red;
                case GameEvents.PopulationGrowthPaused:
                case GameEvents.FieldTransformationFailed:
                case GameEvents.JobChangeFailed:
                case GameEvents.TerritoryExpansionFailed:
                    return Color.yellow;
                case GameEvents.PopulationGrowth:
                case GameEvents.BuildingCompleted:
                case GameEvents.TaskCompleted:
                case GameEvents.FieldTransformationCompleted:
                case GameEvents.TerritoryExpansion:
                case GameEvents.SoldierLevelUpgraded:
                    return Color.green;
                case GameEvents.TaskStarted:
                case GameEvents.RestStarted:
                case GameEvents.RestCompleted:
                case GameEvents.FieldTransformationStarted:
                case GameEvents.PopulationGrowthResumed:
                case GameEvents.JobAssignmentChanged:
                case GameEvents.OverflowingTicksForRest:
                    return ResolveInfoColor();
            }

            if (eventName.EndsWith("TaskComplete", StringComparison.Ordinal))
            {
                return Color.green;
            }

            if (eventName.EndsWith("Failed", StringComparison.Ordinal))
            {
                return Color.yellow;
            }

            return Color.white;
        }

        private static (int Population, int Capacity) ExtractPopulationStatus(EventParameter parameter)
        {
            var population = parameter.IntValue;
            return TryParseInt(parameter.StringValue, out var capacity)
                ? (population, capacity)
                : (population, 0);
        }

        private static string FormatCitizenLabel(EventParameter parameter)
        {
            var id = GetCitizenId(parameter);
            return id > 0 ? $"시민 #{id}" : "시민";
        }

        private static int GetCitizenId(EventParameter parameter)
        {
            return parameter.CustomObject is int citizenId && citizenId > 0 ? citizenId : 0;
        }

        private string DescribeCitizenDeath(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "사망했습니다.";
            }

            return reason switch
            {
                "FoodShortage" => "식량 부족으로 사망했습니다.",
                _ => $"{ToDisplayName(reason)}로 사망했습니다."
            };
        }

        private string DescribeFieldTransformationFailure(EventParameter parameter)
        {
            var target = string.IsNullOrWhiteSpace(parameter.StringValue) ? "필드" : parameter.StringValue;
            return parameter.IntValue switch
            {
                FieldTransformationFailureInsufficientResources => $"{target} 변환에 필요한 자원이 부족합니다.",
                FieldTransformationFailurePlacement => $"{target}을(를) 배치할 수 있는 공간이 없습니다.",
                _ => $"{target} 변환에 실패했습니다."
            };
        }

        private string DescribeJobChangeFailure(EventParameter parameter)
        {
            var jobName = string.IsNullOrWhiteSpace(parameter.StringValue) ? "해당 직업" : parameter.StringValue;
            return parameter.IntValue switch
            {
                JobChangeFailureMissingWorker => $"일꾼이 부족해 {jobName}으로 전환할 수 없습니다.",
                JobChangeFailureInsufficientResource => $"자원이 부족해 {jobName}으로 전환할 수 없습니다.",
                JobChangeFailureNoCitizen => $"{jobName} 직종에 배정된 시민이 없습니다.",
                _ => $"{jobName} 직업 변경에 실패했습니다."
            };
        }

        private static List<string> BuildFallbackDetails(EventParameter parameter)
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(parameter.StringValue))
            {
                details.Add(parameter.StringValue);
            }

            if (parameter.IntValue != 0)
            {
                details.Add(parameter.IntValue.ToString());
            }

            if (parameter.Target.HasValue)
            {
                details.Add(parameter.Target.Value.ToString());
            }

            return details;
        }

        private static bool TryParseInt(string value, out int result)
        {
            return int.TryParse(value, out result);
        }

        private static string ToDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "이벤트";
            }

            var builder = new StringBuilder(value.Length + 8);
            builder.Append(value[0]);
            for (var i = 1; i < value.Length; i++)
            {
                var current = value[i];
                var previous = value[i - 1];
                if (char.IsUpper(current) && !char.IsWhiteSpace(previous) && !char.IsUpper(previous))
                {
                    builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }

        private Color ResolveInfoColor()
        {
            return new Color(0.4f, 0.65f, 1f);
        }

        private void RefreshStatusImmediate()
        {
            statusTimer = 0f;
            UpdateStatusLabel();
        }

        private void UpdateStatusLabel()
        {
            if (statusLabel == null)
            {
                return;
            }

            if (session == null)
            {
                statusLabel.text = "상태: 세션 없음";
                return;
            }

            var populationCount = session.Citizens?.Citizens?.Count ?? 0;
            var territorySize = CalculateTerritorySize();
            var foodAmount = session.Resources?.GetAmount(ResourceType.Food) ?? 0;
            var woodAmount = session.Resources?.GetAmount(ResourceType.Wood) ?? 0;
            var stoneAmount = session.Resources?.GetAmount(ResourceType.Stone) ?? 0;

            statusLabel.text = $"상태: 인구 {populationCount} / 영토 {territorySize} / 식량 {foodAmount} / 나무 {woodAmount} / 돌 {stoneAmount}";
        }

        private int CalculateTerritorySize()
        {
            if (session?.Fields == null)
            {
                return 0;
            }

            var fields = session.Fields.Fields;
            if (fields == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field?.Coordinates == null)
                {
                    continue;
                }

                total += field.Coordinates.Count;
            }

            return total;
        }

        private void EnsureTemplatesDisabled()
        {
            if (messageTemplate != null)
            {
                messageTemplate.gameObject.SetActive(false);
            }

            if (optionButtonTemplate != null)
            {
                optionButtonTemplate.gameObject.SetActive(false);
            }

            if (infoLabelTemplate != null)
            {
                infoLabelTemplate.gameObject.SetActive(false);
            }
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void EnsureMainCamera()
        {
            if (Camera.allCamerasCount > 0)
            {
                return;
            }

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.LookAt(Vector3.zero);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 60f;
        }
    }
}
