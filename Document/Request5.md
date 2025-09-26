# Core 개선 계획

## 개요
- 코어만으로 게임 루프를 구동하기 위한 보강 항목을 정리한다.
- Unity 등 외부 플랫폼은 틱 호출과 이벤트 출력 같은 인터페이스만 사용하도록 한다.

## 규칙 추가
### InitConst의 workerTicks의 시간이 흐르면 worker가 한 명 추가된다.
- 조건 : 해당 worker가 살수 있는 residence 여유분이 있어야 한다.
- worker가 추가되거나 residence 파괴되어 여유가 없는 순간 workerTicks의 시간은 멈춘다.(인구 증가만 멈춤)
- workerTicks의 시간이 멈춘경우 이벤트 발생.

### 사용자의 입력으로 필드 변경을 진행할 수 있다.
- 가능하다면 필드 변경은 즉시 시작된다.
- BadLand -> Transforming -> [target field] 순서로 진행된다.
- 진행 중인 필드 변경은 멈출 수 없다.
- 필드 변경을 진행하려면 task가 없는 worker가 한 명 필요하다.
- 필드 변경은 worker의 특별 task로 취급한다.
- 필드 변경 시 해당 필드의 size만큼의 BadLand를 해당 필드로 변경한다.
- 필드의 위치는 자동으로 결정되며 상하좌우, 대각선을 포함한 모든 인접한 필드가 empty인 위치만 가능하다.

### 사용자의 입력으로 특정 직업의 종사자 수를 늘리거나 줄일 수 있다.
- 늘리는 경우 : worker가 있다면 그중 한 명이 해당 직업으로 변경된다. worker가 없다면 무시된다.
- 줄이는 경우 : 해당 직업의 종사자가 있다면 그 중 한 명이 worker가 된다. 
- 직업 변경은 시간 소요 없이 즉시 일어난다.

### 작업은 가능한 즉시 시작한다.
- 매 틱마다 task가 없는 사람은 종사 가능한 field의 slot을 차지하고 가능한 일을 한다.
- 가능한 일이 여러개가 있다면 한번씩 돌아가면서 한다.
- 가능한 field slot이나 task가 없다면 task를 진행하지 않는다.
- 작업 진행도는 slot이 소유하고, 해당 slot이 중단된 경우 다른 작업자가 이어 받아 완료 할 수 있다.

### 휴식은 가능한 즉시 시작한다.
- InitConst의 ticksForRest(InitConst) 만큼의 누적 task ticks를 수행한 경우 Resting task를 진행할 수 있다.
- Resting을 진행하면 누적 task tics를 0으로 한다.
- task를 진행 중이면 진행 중인 task를 완료하고 Resting task를 진행한다.
- 아무 task가 없어도 누적 task tics는 증가한다.
- 이제보니 이 누적 task tics의 이름은 awakenTicks가 좋겠다.

### Food를 소모하려 하지만 Food가 없는 경우 그 사람은 사망한다.
- 이 현상으로 인해 진행 중인 task가 중단될 수 있다.
- 사람이 마을로 소환된 후 매 foodConsumeTicks(InitConst)가 지날 때마다 Food를 하나 소모한다.

### 영토확장은 Explorer의 Occupying task에 의해 진행된다.
- 영토에 인접한 UnoccupiedField 중 TownHall과 가장 가까운 하나를 골라 BadLand로 변경한다.

### 시간이 흐르거나 사용자 입력에 의해 상태가 변하는 등의 경우 이벤트를 발생한다.
- 자동으로 어떤 일이 시작되는 경우 이벤트가 발생한다. 
- 사용자 입력으로 어떤 일을 시작한 경우 이벤트가 발생한다.
- 사용자 입력으로 어떤 일을 시도 했으나 실패하는 경우 이벤트가 발생한다.