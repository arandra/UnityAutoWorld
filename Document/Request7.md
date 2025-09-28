# 이벤트 시스템 변경 계획

## 개요
- 초기 계획에 맞게 데이터에 따라 이벤트를 처리한다.
- 이벤트 와 액션을 매칭하는 데이터 EventAction을 사용한다.

## Task data 변경
- 기존의 result를 삭제한다.
- RiseEvent에 해당하는 이벤트를 발동한다.

## EventAction 구현 
- EventObject(예: 필드, 시민)가 직접 이벤트를 발행하고 EventParameter.Target은 부가 정보를 담는다.
- EventActions 데이터에는 `eventName`, `eventListener`, `actionName`, `actionImmediately`, 파라미터 목록이 정의된다.
- 각 매니저는 이벤트 목록을 전달받아 자신의 `eventName → actionName` 매핑을 만들고 필요한 이벤트만 EventManager에 등록한다.
- `actionImmediately = false`인 액션은 매니저가 틱 종료 시 누적 합산(예: 자원 합계) 후 한 번에 실행한다.

## PopulationManager 개선
- Resident/Worker/자원 처리 책임을 분리한다: `CitizenManager`, `ResourceManager`, 필요 시 `FieldManager`가 담당.
- 새 CitizenManager는 시민 추가·제거, EventObject 조회, 시민 상태(Tired 등) 관리 등을 담당한다. 이벤트(예: RiseEvent)는 Citizen과 같은 EventObject가 직접 발행한다.
- ResourceManager는 자원 소비/증가를 책임지고 CitizenManager 등에서 의존한다.
- FieldManager는 필드와 관련된 이벤트(예: OccupyingTaskComplete)를 처리해 TransfromField 등을 실행한다.
- 이벤트의 메시지나 표현 방식(텍스트/이미지 등)은 뷰(`GameUIController`)에서 결정한다. 이벤트 발행자는 순수 이벤트 내용만 발행한다.

## Task 데이터 변경 및 RiseEvent 처리
- Task의 `Result`/`Outcome` 대신 `RiseEvent` 문자열을 사용하여 작업 완료 시 이벤트를 발행한다.
- Example: ExplorationOffice에서 Occupying task 완료 → source=Field EventObject, target=변경될 필드 EventObject로 이벤트 발행.
- CitizenManager/ResourceManager 등은 해당 이벤트에 대응하는 액션을 실행한다. (예: OccupyingTaskComplete → FieldManager.TransformField)

## 처리 순서
1. Bootstrap에서 EventActions를 로드하고 각 매니저에 이벤트/액션 매핑 전달.
2. 매니저는 ActionName → 메서드 실행 딕셔너리를 만들어 이벤트에 등록.
3. CitizenManager는 시민 상태를 EventActions 기반으로 관리하고, `actionImmediately`가 false인 액션은 틱 종료 시 누적 처리.
4. Task 완료 시 RiseEvent 문자열을 발행하여 매니저들이 데이터를 기반으로 반응한다.
