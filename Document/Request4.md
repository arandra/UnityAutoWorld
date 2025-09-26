# 도메인 데이터 스펙 정리 (Request 4)

## 1. 자원(Resource)
| 이름  | 설명                     | 비고               |
|-------|--------------------------|--------------------|
| Food  | 생존 유지 용 식량        | `FoodConsumeTicks` 주기로 1 소비 |
| Wood  | 건설 및 제작 재료       | FieldTransform 비용에 사용 |
| Stone | 고급 건설 재료           | 고급 거주지, 대장간 건설에 사용 |
| Weapon| 병사 육성 재료           | Soldier 전직 비용 |
| Armor | 병사 육성 재료           | Soldier 전직 비용 |

## 2. 직업(Job)
| 이름       | 설명                        | 비고                           |
|------------|-----------------------------|--------------------------------|
| Worker     | 범용 작업자                 | 기본 생산/건설 담당           |
| Farmer     | 농장 전담                    | Food 생산                      |
| Miner      | 채석장 전담                  | Stone 생산                     |
| WoodCutter | 목재 가공 전담               | Wood 생산                      |
| Explorer   | 탐험 전담                    | BadLand 확보                   |
| Soldier    | 전투 전담                    | Weapon/Armor 소비, 레벨 업     |

### Soldier 레벨 규칙
- 전직 시 레벨 1에서 시작한다.
- `SoldierUpgradeTicks`마다 레벨이 1씩 증가한다.
- 최대 레벨은 `InitConst` 데이터에 추가 예정.

## 3. 필드(Field)
| 이름               | Empty | 설명                                |
|--------------------|-------|-------------------------------------|
| UnoccupiedField    | 예    | 미점유 지형                         |
| BadLand            | 예    | 불모지, 탐험 이후 확보 대상         |
| CropField          | 아니오| 식량 생산 필드                      |
| LumberMill         | 아니오| 목재 생산 필드                      |
| Quarry             | 아니오| 석재 생산 필드                      |
| Residence          | 아니오| 기본 거주지                         |
| ExplorationOffice  | 아니오| 탐험 거점                           |
| Smithy             | 아니오| 무기/갑옷 제작                      |

## 4. 필드 변환(FieldTransform)
| 대상 필드       | Size | Slot | CostTicks | 자원 비용                     | 선행 조건              |
|----------------|------|------|-----------|-------------------------------|------------------------|
| CropField      | 1    | 1    | 20        | 없음                          | 없음                   |
| LumberMill     | 1    | 1    | 40        | 없음                          | 없음                   |
| Quarry         | 1    | 1    | 40        | 없음                          | 없음                   |
| Residence      | 1    | 4    | 50        | Wood 4                        | LumberMill             |
| ExplorationOffice| 1  | 2    | 50        | Wood 5                        | LumberMill             |
| Residence2     | 2    | 8    | 100       | Wood 12, Stone 4              | Residence, Quarry      |
| Smithy         | 1    | 1    | 50        | Wood 4, Stone 4               | Quarry                 |

## 5. 작업(Task)
| 필드               | 이름           | 담당 직업 | Tick | 결과           |
|--------------------|----------------|-----------|------|----------------|
| CropField          | Harvesting     | Farmer    | 10   | Food           |
| LumberMill         | Cutting        | WoodCutter| 30   | Wood           |
| Quarry             | Quarring       | Miner     | 30   | Stone          |
| Residence          | Resting        | Any       | 30   | (무결과)       |
| ExplorationOffice  | Occupying      | Explorer  | 50   | BadLand 필드 확보 |
| Smithy             | MakingWeapon   | (미정)    | 60   | Weapon         |
| Smithy             | MakingArmor    | (미정)    | 60   | Armor          |

> 추후 Requirement/Result를 복수 자원으로 확장할 때 Task 스키마에 Kind/Value 분리 필드를 도입한다.

## 6. 틱 운영 규칙
- 기본 틱 간격은 100ms (`TickConfig.TickDurationMillis` 기본값).
- 런타임에서 `TickConfig.TickDurationMillis`와 `ITickScheduler.SetTickDuration(ms)`를 통해 속도를 조절한다.
- `FoodConsumeTicks` 주기로 모든 인구가 Food 1을 소비한다.
- Soldier는 `SoldierUpgradeTicks` 간격마다 레벨이 1씩 상승한다.

## 7. 초기화 데이터 (InitConst)
| 항목              | 값                                  |
|-------------------|-------------------------------------|
| WorkerTicks        | 60                                  |
| DestroyTicks       | 15                                  |
| FoodConsumeTicks   | 70                                  |
| SoldierUpgradeTicks| 120                                 |
| InitJobs           | Soldier, Explorer, Worker x4        |
| InitBadLandSize    | 12                                  |
| InitFields         | ExplorationOffice, Residence x2     |

## 8. 향후 과제
1. Soldier 최대 레벨을 InitConst에 추가하고 로직과 연동한다.
2. Smithy 작업 담당 직업 정의 및 Resting/일반 작업에 맞는 Job 매핑 규칙 설계.
3. Task 결과 `BadLand` 변환을 일반화할 Kind/Value 구조 설계.
4. TickScheduler 구현체에서 동적 속도 변경과 Food 소비 루틴을 연결한다.
