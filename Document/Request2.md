공통 이벤트 처리

이벤트 처리 핵심 개념
- 구성요소
  1. event object type enum
    * Manager
    * Citizen
    * Field
  2. event type enum : 각 이벤트 마다 추가 될 예정.
  3. event manager : 이벤트를 처리하는 싱글턴 매니저.
- 이벤트 발생
  1. eventManager.Invoke(EventType eventType, EventObject source, EventParameter parameter)
    * 여기서 source는 이벤트를 발동 하는 주체.
  2. EventParameter
    * 필드
      = EventObject? target = null
      = EventObjectType targetTypes = EventObjectType.None
      = int intValue
      = string stringValue = string.empty
      = object? customObject = null
    * 여기서 target은 이벤트를 받는 인스턴스가 아니라, source가 event를 행할 때 바라보는 대상.
- 이벤트 등록
  1. eventManager.Register(EventType eventType, IEventListener listener)
  2. eventManager.RegisterAll(IEventListener listener)

