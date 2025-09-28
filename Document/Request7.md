# 이벤트 시스템 변경 계획

## 개요
- 초기 계획에 맞게 데이터에 따라 이벤트를 처리한다.
- 이벤트 와 액션을 매칭하는 데이터 EventAction을 사용한다.

## Task data 변경
- 기존의 result를 삭제한다.
- RiseEvent에 해당하는 이벤트를 발동한다.

## EventAction 구현 
- EventListener는 data에 따라 이벤트를 구독한다.
- EventListener는 이벤트에 따른 Action을 구현한다.

## PopulationManager 개선
- PopulationManager에서 RaiseManagerEvent를 호출시 메시지를 넣는데, 이벤트 결과를 메시지로 보여줄지 이미지로 보여줄 지 여부는 view인 GameUIController의 선택사항이다.
- PopulationManager는 너무 많은 책임이 있다. ResourceManager, FeildManager, CitizenManager로 분산하고 PopulationManager는 삭제한다.
                                                                                                

